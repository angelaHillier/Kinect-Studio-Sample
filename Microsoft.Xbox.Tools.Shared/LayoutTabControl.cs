//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class LayoutTabControl : TabControl, IActivationSite
    {
        public static readonly DependencyProperty IsFileButtonEnabledProperty = DependencyProperty.Register(
            "IsFileButtonEnabled", typeof(bool), typeof(LayoutTabControl), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty IsFileButtonVisibleProperty = DependencyProperty.Register(
            "IsFileButtonVisible", typeof(bool), typeof(LayoutTabControl), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty IsInLayoutEditModeProperty = DependencyProperty.Register(
            "IsInLayoutEditMode", typeof(bool), typeof(LayoutTabControl));

        public static readonly DependencyProperty ActiveContentProperty = DependencyProperty.Register(
            "ActiveContent", typeof(object), typeof(LayoutTabControl), new FrameworkPropertyMetadata(OnActiveContentChanged));

        public static readonly DependencyProperty ServiceProviderProperty = DependencyProperty.Register(
            "ServiceProvider", typeof(IServiceProvider), typeof(LayoutTabControl), new FrameworkPropertyMetadata(OnServiceProviderChanged));

        public static readonly RoutedEvent ActiveContentChangedEvent = EventManager.RegisterRoutedEvent("ActiveContentChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(LayoutTabControl));
        public static readonly RoutedCommand TabShortcutCommand = new RoutedCommand("TabShortcut", typeof(LayoutTabControl));

        ContentPresenter contentPresenter;
        LayoutInstance lastActiveLayout;
        ToolsUIWindow window;
        ComboBox documentCombo;
        IActiveDocumentTracker activeDocumentTracker;

        public LayoutTabControl()
        {
            this.CommandBindings.Add(new CommandBinding(TabShortcutCommand, OnTabShortcutExecuted));
        }

        public object ActiveContent
        {
            get { return (object)GetValue(ActiveContentProperty); }
            set { SetValue(ActiveContentProperty, value); }
        }

        public bool IsFileButtonEnabled
        {
            get { return (bool)GetValue(IsFileButtonEnabledProperty); }
            set { SetValue(IsFileButtonEnabledProperty, value); }
        }

        public bool IsFileButtonVisible
        {
            get { return (bool)GetValue(IsFileButtonVisibleProperty); }
            set { SetValue(IsFileButtonVisibleProperty, value); }
        }

        public bool IsInLayoutEditMode
        {
            get { return (bool)GetValue(IsInLayoutEditModeProperty); }
            set { SetValue(IsInLayoutEditModeProperty, value); }
        }

        public IServiceProvider ServiceProvider
        {
            get { return (IServiceProvider)GetValue(ServiceProviderProperty); }
            set { SetValue(ServiceProviderProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.contentPresenter = GetTemplateChild("PART_SelectedContentHost") as ContentPresenter;
            if (this.contentPresenter != null)
            {
                this.SetBinding(ActiveContentProperty, new Binding { Source = this.contentPresenter, Path = new PropertyPath(ContentPresenter.ContentProperty) });
            }

            this.documentCombo = GetTemplateChild("PART_DocumentCombo") as ComboBox;

            if (this.documentCombo != null && this.ServiceProvider != null)
            {
                this.activeDocumentTracker = this.ServiceProvider.GetService(typeof(IActiveDocumentTracker)) as IActiveDocumentTracker;

                if (this.activeDocumentTracker != null)
                {
                    this.activeDocumentTracker.ActiveDocumentChanged += OnActiveDocumentChanged;
                    this.Unloaded += OnUnloaded;
                }
            }
        }

        void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.activeDocumentTracker.ActiveDocumentChanged -= OnActiveDocumentChanged;
            this.Unloaded -= OnUnloaded;
        }

        void OnActiveDocumentChanged(object sender,  EventArgs e)
        {
            if (!this.activeDocumentTracker.Documents.Any())
            {
                BindingOperations.ClearBinding(this.documentCombo, MinWidthProperty);
            }
            else
            {
                this.documentCombo.SetBinding(MinWidthProperty, new Binding { Source = this.documentCombo, Path = new PropertyPath(ActualWidthProperty) });
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            var child = this.SelectedItem as IActivationSite;

            if (child != null && this.IsLoaded)
            {
                child.TunnelActivation();
            }

            var layoutDefinition = this.SelectedItem as LayoutDefinition;

            if (layoutDefinition != null)
            {
                layoutDefinition.OnActivated();
            }
            else
            {
                var layoutInstance = this.SelectedItem as LayoutInstance;

                if (layoutInstance != null)
                {
                    layoutInstance.LayoutDefinition.OnActivated();
                }
            }

            if (!this.IsInLayoutEditMode)
            {
                ToolsUIApplication.Instance.RequestViewBindingUpdate();
            }
        }

        void OnTabShortcutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var data = e.Parameter as LayoutInstance;

            if (data != null)
            {
                this.SelectedItem = data;

                var shortcutManager = ShortcutManager.GetInstance(this);

                if (shortcutManager != null)
                {
                    shortcutManager.PushUISubMode("ViewSelect");
                    shortcutManager.AreShortcutAdornmentsVisible = true;
                }

                e.Handled = true;
            }
        }

        void OnTabShortcutCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is LayoutDefinition;
            e.Handled = e.CanExecute;
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (this.SelectedIndex == -1 && this.Items.Count > 0)
            {
                this.SelectedIndex = 0;
            }
        }

        public IEnumerable<XElement> SaveLayoutStates()
        {
            for (int i = 0; i < this.Items.Count; i++)
            {
                var tabItem = this.ItemContainerGenerator.ContainerFromIndex(i) as TabItem;

                if (tabItem != null && tabItem.Content is LayoutControl)
                {
                    var layout = (LayoutControl)tabItem.Content;

                    var element = new XElement("Layout", new XAttribute("Name", layout.LayoutInstance.LayoutDefinition.Header));

                    if (i == this.SelectedIndex)
                    {
                        element.Add(new XAttribute("IsSelected", "true"));
                    }

                    layout.LayoutInstance.WriteState(element);
                    yield return element;
                }
            }
        }

        public void LoadLayoutStates(IEnumerable<XElement> elements)
        {
            var pages = elements.ToArray();
            int index = 0;

            foreach (var data in this.Items.OfType<LayoutInstance>())
            {
                XElement pageElement = pages[index++];

                data.ReadState(pageElement);
            }
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            var tabItem = element as TabItem;
            var data = item as LayoutInstance;

            if (tabItem == null)
            {
                base.PrepareContainerForItemOverride(element, item);
                return;
            }

            if (data != null)
            {
                var headerBinding = new Binding { Source = data.LayoutDefinition, Path = new PropertyPath(LayoutDefinition.HeaderProperty), Mode = BindingMode.TwoWay };
                tabItem.SetBinding(TabItem.HeaderProperty, headerBinding);
                tabItem.SetBinding(System.Windows.Automation.AutomationProperties.AutomationIdProperty, headerBinding);
                tabItem.Content = data.LayoutControl;
            }
            else if (item is LayoutDefinition)
            {
                var tabData = item as LayoutDefinition;
                var headerBinding = new Binding { Source = tabData, Path = new PropertyPath(LayoutDefinition.HeaderProperty), Mode = BindingMode.TwoWay };

                tabItem.SetBinding(TabItem.HeaderProperty, headerBinding);
                tabItem.SetBinding(System.Windows.Automation.AutomationProperties.AutomationIdProperty, headerBinding);

                // WPF's bindings are strange, so if our ServiceProvider is bound, it shows as null here
                var sp = this.ServiceProvider ?? this.FindParent<ToolsUIWindow>().ServiceProvider;
                var layoutInstance = new LayoutInstance(sp, this, tabData, true);

                tabItem.Content = layoutInstance.LayoutControl;
            }
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            var tabItem = element as TabItem;
            var data = item as LayoutDefinition;

            if (tabItem != null && data != null && tabItem.Content is LayoutInstance)
            {
                var layout = (LayoutInstance)tabItem.Content;

                layout.CloseAllViews();
            }

            base.ClearContainerForItemOverride(element, item);
        }

        public void EnsureVisibleLayoutSelected()
        {
            // First, check to see if the selected item is SUPPOSED to be visible (at the point this is
            // called, it's possible the binding hasn't propagated yet)
            if ((this.SelectedItem is LayoutInstance && ((LayoutInstance)this.SelectedItem).IsVisible) ||
                (this.SelectedItem is LayoutDefinition) && ViewLayoutEditor.GetIsLayoutVisible((LayoutDefinition)this.SelectedItem))
            {
                // Do nothing
                return;
            }

            foreach (var item in this.Items)
            {
                if ((item is LayoutInstance && ((LayoutInstance)item).IsVisible) ||
                    (item is LayoutDefinition) && ViewLayoutEditor.GetIsLayoutVisible((LayoutDefinition)item))
                {
                    this.SelectedItem = item;
                    break;
                }
            }
        }

        IActivationSite IActivationSite.ParentSite { get { return null; } }

        void IActivationSite.BubbleActivation(object child)
        {
            var layoutInstance = child as LayoutInstance;

            if (layoutInstance != null && this.Items.Contains(layoutInstance))
            {
                this.lastActiveLayout = layoutInstance;
                this.SelectedIndex = this.Items.IndexOf(layoutInstance);
            }
        }

        void IActivationSite.TunnelActivation()
        {
            // Nobody calls us to tunnel. Perhaps on window activation?
            if ((this.lastActiveLayout is IActivationSite) && this.Items.Contains(this.lastActiveLayout))
            {
                ((IActivationSite)this.lastActiveLayout).TunnelActivation();
            }
        }

        void IActivationSite.NotifyActivation(object child)
        {
            var layoutInstance = child as LayoutInstance;

            if (layoutInstance != null)
            {
                this.lastActiveLayout = layoutInstance;
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new LayoutTabControlAutomationPeer(this);
        }

        static void OnActiveContentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            LayoutTabControl ctrl = obj as LayoutTabControl;

            if (ctrl != null)
            {
                ctrl.RaiseEvent(new RoutedEventArgs(ActiveContentChangedEvent));

                if (ctrl.SelectedIndex >= 0)
                {
                    var tabItem = ctrl.ItemContainerGenerator.ContainerFromIndex(ctrl.SelectedIndex) as TabItem;

                    if (tabItem != null && tabItem.Content is IActivationSite)
                    {
                        ((IActivationSite)tabItem.Content).TunnelActivation();
                    }
                }
            }
        }

        static void OnServiceProviderChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            LayoutTabControl control = obj as LayoutTabControl;

            if (control != null)
            {
                if (control.ServiceProvider != null)
                {
                    control.window = control.ServiceProvider.GetService(typeof(ToolsUIWindow)) as ToolsUIWindow;
                }
                else
                {
                    control.window = null;
                }
            }
        }
    }

    public class LayoutTabControlAutomationPeer : TabControlAutomationPeer
    {
        LayoutTabControl owner;
        static string[] additionalUIAutomationChildrenNames = { "PART_FileTab", "PART_DocumentCloseButton", "PART_DocumentCombo" };

        public LayoutTabControlAutomationPeer(LayoutTabControl owner)
            : base(owner)
        {
            this.owner = owner;
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            var list = base.GetChildrenCore();

            foreach (var name in additionalUIAutomationChildrenNames)
            {
                var element = owner.Template.FindName(name, owner) as UIElement;

                if (element != null)
                {
                    list.Add(UIElementAutomationPeer.CreatePeerForElement(element));
                }
            }

            return list;
        }
    }
}
