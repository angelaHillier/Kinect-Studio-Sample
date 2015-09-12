//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public interface IActivationSite
    {
        IActivationSite ParentSite { get; }

        // Bubbling activation is a request by a child site to obtain focus.  It is generally triggered by a call to View.Activate()
        void BubbleActivation(object child);

        // Tunneling activation is a mandate from a parent site to become active (take focus).  It can be called as a result of
        // the bubbling activation (caused by View.Activate), or it can happen when the user manipulates a tab control to bring
        // a new layout or view to the top.
        void TunnelActivation();

        // Notification of activation is a child that says "like it or not, I just got activated"
        void NotifyActivation(object child);
    }

    public static class ViewActivationHelpers
    {
        public static T FindParentSite<T>(this IActivationSite site) where T : class, IActivationSite
        {
            for (var p = site; p != null; p = p.ParentSite)
            {
                if (p is T)
                {
                    return (T)p;
                }
            }

            return null;
        }
    }

    public abstract class View : DependencyObject, INotifyPropertyChanged
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title", typeof(string), typeof(View));

        public static readonly DependencyProperty ViewObjectProperty = DependencyProperty.RegisterAttached(
            "ViewObject", typeof(View), typeof(View));

        public static readonly DependencyProperty IsInitiallyFocusedProperty = DependencyProperty.RegisterAttached(
            "IsInitiallyFocused", typeof(bool), typeof(View), new FrameworkPropertyMetadata(false, OnIsInitiallyFocusedChanged));

        public static readonly RoutedEvent InitiallyFocusedElementVisibleEvent = EventManager.RegisterRoutedEvent("InitiallyFocusedElementVisible", RoutingStrategy.Bubble, typeof(EventHandler<InitiallyFocusedElementVisibleEventArgs>), typeof(View));
        static int nextActivationIndex = 0;

        protected IServiceProvider ServiceProvider { get; private set; }
        public IViewCreationCommand ViewCreationCommand { get; internal set; }
        public int ActivationIndex { get; private set; }
        public bool IsViewContentLoaded { get; private set; }
        protected ViewManager ViewManager { get; private set; }

        private FrameworkElement viewContent;
        private FrameworkElement lastElementWithFocus;
        private bool askedToTakeFocus;
        private DispatcherFrame waitingForLoadedFrame;
        private IActivationSite site;
        private string documentFactoryName;

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public FrameworkElement ViewContent
        {
            get
            {
                if (this.viewContent == null)
                {
                    this.viewContent = CreateViewContent();
                    this.viewContent.GotKeyboardFocus += OnViewContentGotKeyboardFocus;
                    this.viewContent.IsKeyboardFocusWithinChanged += OnViewContentIsKeyboardFocusWithinChanged;
                    this.viewContent.Loaded += OnViewContentLoaded;
                    this.viewContent.AddHandler(InitiallyFocusedElementVisibleEvent, new EventHandler<InitiallyFocusedElementVisibleEventArgs>(OnInitiallyFocusedElementVisible));
                    SetViewObject(this.viewContent, this);
                    if (this.viewContent.IsLoaded)
                    {
                        this.IsViewContentLoaded = true;
                        Notify("IsViewContentLoaded");
                    }
                }

                return viewContent;
            }
        }

        protected bool HasViewContentBeenCreated { get { return this.viewContent != null; } }

        internal IActivationSite Site
        {
            get
            {
                return this.site;
            }
            set
            {
                if (this.site != value)
                {
                    this.site = value;
                    Notify("Site");
                }
            }
        }

        public string LocationDescription
        {
            get
            {
                LayoutInstance layoutInstance = null;
                string slotName = null;
                ToolsUIWindow ownerWindow = null;

                // NOTE:  This is gunky code, but is only used in the "Views" listbox in the 
                // (internal) options page...
                for (var parent = this.Site; parent != null; parent = parent.ParentSite)
                {
                    if (parent is LayoutInstance)
                    {
                        layoutInstance = (LayoutInstance)parent;
                    }
                    else if (parent is SlotContent)
                    {
                        slotName = ((SlotContent)parent).SlotName;
                    }
                    else if (parent is LayoutTabControl)
                    {
                        ownerWindow = ((LayoutTabControl)parent).FindParent<ToolsUIWindow>();
                    }
                }

                if (layoutInstance != null && slotName != null)
                {
                    return string.Format("Layout '{0}', Slot {1} ({2})", layoutInstance.LayoutDefinition.Header, slotName,
                        ownerWindow != null ? (ownerWindow.IsMainWindow ? "Main Window" : string.Format("Aux window {0}", ownerWindow.GetHashCode())) : "Unknown window");
                }

                return null;
            }
        }

        public string LayoutName
        {
            get
            {
                var layoutInstance = this.Site.FindParentSite<LayoutInstance>();

                if (layoutInstance != null)
                {
                    return layoutInstance.LayoutDefinition.Header;
                }

                return null;
            }
        }

        public string DocumentAffinity
        {
            get
            {
                return this.documentFactoryName;
            }
            set
            {
                if (this.documentFactoryName != null)
                {
                    Debug.Fail("Don't set the document affinity for a view more than once!");
                }

                this.documentFactoryName = value;
                OnSetDocumentAffinity(value);
            }
        }

        internal void Initialize(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.ViewManager = serviceProvider.GetService(typeof(ViewManager)) as ViewManager;

            if (this.ViewManager != null)
            {
                this.ViewManager.OnViewCreated(this);
            }

            this.OnInitialized();
        }

        protected abstract FrameworkElement CreateViewContent();

        protected virtual void OnInitialized()
        {
        }

        protected virtual void OnSetDocumentAffinity(string documentFactoryName)
        {
        }

        public virtual void LoadViewState(XElement state)
        {
        }

        public virtual XElement GetViewState()
        {
            return null;
        }

        // "Ephemeral" view state includes things not persisted in the window state between sessions.
        // Should be things like scroll positions, expansion states, selection, etc.
        // This is used when duplicating windows.
        public virtual void ReplicateEphemeralViewState(View viewToCopy)
        {
        }

        public void Close()
        {
            this.OnClosed();

            var handler = this.Closed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }

            if (this.viewContent != null)
            {
                this.viewContent.GotKeyboardFocus -= OnViewContentGotKeyboardFocus;
                this.viewContent.IsKeyboardFocusWithinChanged -= OnViewContentIsKeyboardFocusWithinChanged;
                this.viewContent.Loaded -= OnViewContentLoaded;
            }
        }

        protected virtual void OnClosed()
        {
        }

        void OnViewContentLoaded(object sender, RoutedEventArgs e)
        {
            if (this.waitingForLoadedFrame != null)
            {
                this.waitingForLoadedFrame.Continue = false;
            }

            this.IsViewContentLoaded = true;
            Notify("IsViewContentLoaded");
        }

        void OnViewContentGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (this.viewContent.IsKeyboardFocusWithin)
            {
                this.lastElementWithFocus = e.NewFocus as FrameworkElement;
            }
        }

        void OnViewContentIsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.viewContent.IsKeyboardFocusWithin)
            {
                this.HandleActivation();
            }
        }

        void OnInitiallyFocusedElementVisible(object sender, InitiallyFocusedElementVisibleEventArgs e)
        {
            if (this.askedToTakeFocus)
            {
                e.InitiallyFocusedElement.Focus();
                this.askedToTakeFocus = false;
            }
            else if (this.lastElementWithFocus == null)
            {
                this.lastElementWithFocus = e.InitiallyFocusedElement as FrameworkElement;
            }
        }

        public void Activate()
        {
            // To "Activate" a view means to make it take keyboard focus.  This may require flipping
            // to different tab(s) depending on how the view is sited, so it's largely up to the site.
            if (this.Site == null)
            {
                // We don't have a site.  Not a recommended scenario, but we try anyway.
                if (this.ViewContent != null)
                {
                    this.ViewContent.Focus();
                }

                return;
            }

            this.Site.BubbleActivation(this);

            if (this.viewContent == null || !this.viewContent.IsLoaded)
            {
                if (ToolsUIApplication.Instance == null || !ToolsUIApplication.Instance.ShuttingDown)
                {
                    var timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100) };

                    // Because we were just activated, the tab may be in the process of becoming active/loaded
                    // (i.e., if this tab was behind another one, it would have been unloaded).
                    // The timer is in case something goes sideways and the view content never gets loaded again.
                    EventHandler tickHandler = null;
                    tickHandler = (o, s) =>
                    {
                        if (waitingForLoadedFrame != null)
                        {
                            waitingForLoadedFrame.Continue = false;
                        }
                        timer.Stop();
                        timer.Tick -= tickHandler;
                    };
                    timer.Tick += tickHandler;
                    waitingForLoadedFrame = new DispatcherFrame();
                    timer.Start();
                    try
                    {
                        Dispatcher.PushFrame(waitingForLoadedFrame);
                    }
                    catch (Exception)
                    {
                        // there's no way to determine if a dispatcher is suspended...
                    }
                    waitingForLoadedFrame = null;
                }

                if (this.viewContent == null)
                {
                    // Something went south, we didn't get activated.  Bail.
                    return;
                }
            }

            var ownerWindow = this.viewContent.FindParent<Window>();

            if (ownerWindow == null || ownerWindow.IsActive)
            {
                RestoreLastElementFocus();
            }
            else
            {
                EventHandler handler = null;

                handler = (s, e) =>
                {
                    RestoreLastElementFocus();
                    ownerWindow.Activated -= handler;
                };
                ownerWindow.Activated += handler;
            }
        }

        void RestoreLastElementFocus()
        {
            // Restore the element that last had focus.  This is also virtual, so derivations can do
            // special casing as necessary.
            if (!RestoreFocus(this.lastElementWithFocus))
            {
                // We don't know who to give focus to.  Call our (virtual) TakeFocus method, which
                // gives derivations a chance to find/focus an appropriate element.
                if (!TakeFocus())
                {
                    // Derivation didn't know who to focus either.  In this case, we just indicate that we
                    // were asked to take focus.  Hopefully, as the view content is loaded/has templates
                    // applied, etc., an element marked IsInitiallyFocused will be loaded, and we'll
                    // focus it.
                    this.askedToTakeFocus = true;
                }
            }
        }

        protected virtual bool RestoreFocus(FrameworkElement lastFocusedElement)
        {
            // Try to set focus back to the same element that had focus last time (given).  Note that the
            // lastFocusedElement may be the initially focused element captured by the 
            // InitiallyFocusedElementVisible event.
            if (lastFocusedElement != null && lastFocusedElement.IsVisible && lastFocusedElement.Focusable)
            {
                lastFocusedElement.Focus();
                return true;
            }

            return false;
        }

        protected virtual bool TakeFocus()
        {
            // Override this method to set focus on a particular element when your view is activated for the
            // first time.  Note that overriding this is only necessary if the element you want to have initial
            // focus is dynamic -- otherwise, just set View.InitiallyFocusedElement to true on the element, and
            // we'll focus it for you, if/when it becomes visible.
            return false;
        }

        internal void HandleActivation()
        {
            this.ActivationIndex = ++nextActivationIndex;

            if (this.Site != null)
            {
                this.Site.NotifyActivation(this);
            }

            this.OnActivated();

            var handler = this.Activated;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnActivated()
        {
        }

        protected void Notify(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        public static View GetViewObject(DependencyObject obj)
        {
            return (View)obj.GetValue(ViewObjectProperty);
        }

        public static void SetViewObject(DependencyObject obj, View value)
        {
            obj.SetValue(ViewObjectProperty, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Closed;
        public event EventHandler Activated;

        public static bool GetIsInitiallyFocused(UIElement obj)
        {
            return (bool)obj.GetValue(IsInitiallyFocusedProperty);
        }

        public static void SetIsInitiallyFocused(UIElement obj, bool value)
        {
            obj.SetValue(IsInitiallyFocusedProperty, value);
        }

        static void OnIsInitiallyFocusedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = obj as UIElement;

            if (element != null && (bool)e.NewValue)
            {
                // When this property is set to true on an element, we simply raise an event that the
                // owning View object listens for, so it knows what element to give focus when the view
                // is activated for the first time.  If the element is not visible at this point, we
                // wait for it to become visible before raising the event.
                if (element.IsVisible)
                {
                    element.RaiseEvent(new InitiallyFocusedElementVisibleEventArgs(element));
                }
                else
                {
                    element.IsVisibleChanged += OnInitiallyFocusedElementVisibleChanged;
                }
            }
        }

        static void OnInitiallyFocusedElementVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as UIElement;

            if (element != null && element.IsVisible)
            {
                element.RaiseEvent(new InitiallyFocusedElementVisibleEventArgs(element));
                element.IsVisibleChanged -= OnInitiallyFocusedElementVisibleChanged;
            }
        }
    }

    public class InitiallyFocusedElementVisibleEventArgs : RoutedEventArgs
    {
        public UIElement InitiallyFocusedElement { get; private set; }

        public InitiallyFocusedElementVisibleEventArgs(UIElement initiallyFocusedElement)
            : base(View.InitiallyFocusedElementVisibleEvent)
        {
            this.InitiallyFocusedElement = initiallyFocusedElement;
        }
    }
}
