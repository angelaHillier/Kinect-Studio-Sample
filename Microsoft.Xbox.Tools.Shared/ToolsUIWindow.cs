//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ToolsUIWindow : Window
    {
        public static readonly DependencyProperty IsFileTabOpenProperty = DependencyProperty.Register(
            "IsFileTabOpen", typeof(bool), typeof(ToolsUIWindow), new FrameworkPropertyMetadata(OnIsFileTabOpenChanged));

        public static readonly DependencyProperty IsDocumentDropdownOpenProperty = DependencyProperty.Register(
            "IsDocumentDropdownOpen", typeof(bool), typeof(ToolsUIWindow), new FrameworkPropertyMetadata(OnIsDocumentDropdownOpenChanged));

        public static readonly DependencyProperty ServiceProviderProperty = DependencyProperty.Register(
            "ServiceProvider", typeof(IServiceProvider), typeof(ToolsUIWindow));

        public static readonly DependencyProperty CurrentLayoutDefinitionForEditProperty = DependencyProperty.Register(
            "CurrentLayoutDefinitionForEdit", typeof(LayoutDefinition), typeof(ToolsUIWindow));

        public static RoutedCommand CloseWindowCommand = new RoutedCommand("CloseWindow", typeof(ToolsUIWindow));
        public static RoutedCommand ExitCommand = new RoutedCommand("Exit", typeof(ToolsUIWindow));
        public static RoutedCommand MinimizeCommand = new RoutedCommand("Minimize", typeof(ToolsUIWindow));
        public static RoutedCommand MaximizeCommand = new RoutedCommand("Maximize", typeof(ToolsUIWindow));
        public static RoutedCommand RestoreCommand = new RoutedCommand("Restore", typeof(ToolsUIWindow));
        public static RoutedCommand ToggleFileTabCommand = new RoutedCommand("ToggleFileTab", typeof(ToolsUIWindow));
        public static RoutedCommand OpenDocumentDropdownCommand = new RoutedCommand("OpenDocumentDropdown", typeof(ToolsUIWindow));
        public static RoutedCommand ActivateDocumentCommand = new RoutedCommand("ActivateDocument", typeof(ToolsUIWindow));
        public static RoutedCommand CloseDocumentCommand = new RoutedCommand("CloseDocument", typeof(ToolsUIWindow));
        public static RoutedCommand EditLayoutsCommand = new RoutedCommand("EditLayouts", typeof(ToolsUIWindow));
        public static RoutedCommand RevertToDefaultWindowStateCommand = new RoutedCommand("RevertToDefaultWindowState", typeof(ToolsUIWindow));

        static int nextActivationIndex;

        FrameworkElement mover;
        FrameworkElement sysMenuIcon;
        Point moveStartPoint;
        Size moveStartSize;
        Button newWindowButton;
        QATItemsControl qatItemsControl;
        FileTabControl fileTabControl;
        DependencyObject focusedElement;
        Action<DragDeltaEventArgs> currentSizeAction;
        Thumb currentSizer;
        Dictionary<string, Action<DragDeltaEventArgs>> sizerTable;
        bool sizeFixed;
        bool inDragMove;
        HwndSource hwndSource;
        XElement deferredWindowState;
        WindowDocumentTracker documentTracker;
        DispatcherTimer shortcutDisplayTimer;
        ShortcutManager shortcutManager;
        string currentDocumentFactoryName;
        ToolsUIWindow duplicatingWindow;

        public bool IsFileTabOpen
        {
            get { return (bool)GetValue(IsFileTabOpenProperty); }
            set { SetValue(IsFileTabOpenProperty, value); }
        }

        public IServiceProvider ServiceProvider
        {
            get { return (IServiceProvider)GetValue(ServiceProviderProperty); }
            set { SetValue(ServiceProviderProperty, value); }
        }

        public bool IsDocumentDropdownOpen
        {
            get { return (bool)GetValue(IsDocumentDropdownOpenProperty); }
            set { SetValue(IsDocumentDropdownOpenProperty, value); }
        }

        public LayoutDefinition CurrentLayoutDefinitionForEdit
        {
            get { return (LayoutDefinition)GetValue(CurrentLayoutDefinitionForEditProperty); }
            set { SetValue(CurrentLayoutDefinitionForEditProperty, value); }
        }
        
        public ToolsUIApplication Application { get; private set; }
        public LayoutTabControl LayoutTabControl { get; private set; }
        public ObservableCollection<LayoutInstance> Layouts { get; private set; }
        public bool IsMainWindow { get { return this == this.Application.MainToolsUIWindow; } }
        public IActiveDocumentTracker DocumentTracker { get { return this.documentTracker; } }
        public Rect NormalWindowRect { get; private set; }
        public int ActivationIndex { get; private set; }
        public Dictionary<object, object> StateTable { get; private set; }

        public ToolsUIWindow()
        {
            this.StateTable = new Dictionary<object, object>();

            this.Application = ToolsUIApplication.Instance;
            if (this.Application == null)
            {
                throw new InvalidOperationException("ToolsUIWindow must be used in conjunction with ToolsUIApplication!");
            }

            // NOTE:  Can't use !IsMainWindow here, because it will never return true (the application hasn't had
            // a chance to set it yet, as we're the constructor!)
            if (this.Application.MainToolsUIWindow != null)
            {
                this.Style = this.Application.MainToolsUIWindow.Style;
                this.ServiceProvider = CreateServiceProvider();
            }

            this.Layouts = new ObservableCollection<LayoutInstance>();

            AddHandler(Thumb.DragStartedEvent, (DragStartedEventHandler)OnDragStarted);
            AddHandler(Thumb.DragDeltaEvent, (DragDeltaEventHandler)OnDragDelta);
            AddHandler(Thumb.DragCompletedEvent, (DragCompletedEventHandler)OnDragCompleted);

            this.sizerTable = new Dictionary<string, Action<DragDeltaEventArgs>>();

            CommandBindings.Add(new CommandBinding(CloseWindowCommand, OnCloseWindowExecuted));
            CommandBindings.Add(new CommandBinding(ExitCommand, OnExitExecuted));
            CommandBindings.Add(new CommandBinding(MinimizeCommand, OnMinimizeExecuted, OnMinimizeCanExecute));
            CommandBindings.Add(new CommandBinding(MaximizeCommand, OnMaximizeExecuted, OnMaximizeCanExecute));
            CommandBindings.Add(new CommandBinding(RestoreCommand, OnRestoreExecuted, OnRestoreCanExecute));
            CommandBindings.Add(new CommandBinding(ToggleFileTabCommand, OnToggleFileTabExecuted));
            CommandBindings.Add(new CommandBinding(OpenDocumentDropdownCommand, OnOpenDocumentDropdownExecuted));
            CommandBindings.Add(new CommandBinding(ActivateDocumentCommand, OnActivateDocumentExecuted));
            CommandBindings.Add(new CommandBinding(EditLayoutsCommand, OnEditLayoutsExecuted));
            CommandBindings.Add(new CommandBinding(RevertToDefaultWindowStateCommand, OnRevertToDefaultWindowStateExecuted));

            this.SourceInitialized += OnSourceInitialized;
            this.MinWidth = 200;
            this.MinHeight = 200;

            this.shortcutDisplayTimer = new DispatcherTimer();
            this.shortcutDisplayTimer.Tick += OnShortcutTimerTick;
            this.shortcutDisplayTimer.Interval = TimeSpan.FromMilliseconds(400);

            this.shortcutManager = new ShortcutManager();

            this.shortcutManager.UIModeChanged += OnUIModeChanged;
            this.shortcutManager.EmptyModePushed += OnEmptyModePushed;

            ShortcutManager.SetInstance(this, this.shortcutManager);
        }

        bool leaveShortcutModeOnAltUp;
        bool showAdornmentsOnAltUp;
        bool altKeyIsDown;
        bool inShortcutMode;

        void OnShortcutTimerTick(object sender, EventArgs e)
        {
            if (this.inShortcutMode)
            {
                this.shortcutManager.AreShortcutAdornmentsVisible = true;
            }

            this.shortcutDisplayTimer.Stop();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (IsAltKeyEvent(e))
            {
                if (!this.altKeyIsDown)
                {
                    if (!this.inShortcutMode)
                    {
                        this.inShortcutMode = true;
                        this.leaveShortcutModeOnAltUp = false;
                        this.showAdornmentsOnAltUp = true;
                        this.shortcutDisplayTimer.Start();
                    }
                    else
                    {
                        this.leaveShortcutModeOnAltUp = true;
                        this.showAdornmentsOnAltUp = false;
                    }
                }

                this.altKeyIsDown = true;
                e.Handled = true;
                return;
            }

            this.altKeyIsDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

            if (this.inShortcutMode || this.altKeyIsDown)
            {
                var key = e.Key == Key.System ? e.SystemKey : e.Key;

                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && this.shortcutManager.ProcessShortcutKey(key))
                {
                    e.Handled = true;
                }
                else if (!this.shortcutManager.AreShortcutAdornmentsVisible)
                {
                    LeaveShortcutMode();
                }
                else
                {
                    this.leaveShortcutModeOnAltUp = false;
                    this.showAdornmentsOnAltUp = false;
                    this.shortcutDisplayTimer.Stop();
                }
            }

            base.OnPreviewKeyDown(e);
        }

        bool IsAltKeyEvent(KeyEventArgs e)
        {
            return (e.Key == Key.LeftAlt || e.Key == Key.RightAlt || (e.Key == Key.System && (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt)));
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (IsAltKeyEvent(e))
            {
                Debug.Assert(!(this.leaveShortcutModeOnAltUp && this.showAdornmentsOnAltUp));

                if (this.leaveShortcutModeOnAltUp)
                {
                    LeaveShortcutMode();
                }

                if (this.showAdornmentsOnAltUp)
                {
                    this.shortcutManager.AreShortcutAdornmentsVisible = true;
                }

                this.shortcutDisplayTimer.Stop();

                this.altKeyIsDown = false;
            }

            base.OnPreviewKeyUp(e);
        }

        public void LeaveShortcutMode()
        {
            this.shortcutManager.AreShortcutAdornmentsVisible = false;
            this.showAdornmentsOnAltUp = false;
            this.inShortcutMode = false;
            this.shortcutDisplayTimer.Stop();
            this.shortcutManager.UIMode = this.IsFileTabOpen ? "File" : string.Empty;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (this.inShortcutMode)
            {
                // Special case -- if the document dropdown is open, let the click do its normal thing.
                // If we exit shortcut mode, it dismisses the dropdown before the click can happen.
                // If the user clicks on an item, it will exit shortcut mode as part of the document switch.
                // If the user clicks outside the combo, the close event will update the UI mode.
                if (!this.IsDocumentDropdownOpen)
                {
                    LeaveShortcutMode();
                }
            }
            base.OnPreviewMouseDown(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            this.ActivationIndex = ++nextActivationIndex;
            base.OnActivated(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            LeaveShortcutMode();
            base.OnDeactivated(e);
        }

        protected void ActivateFileTabPage(string pageName)
        {
            var pageTabDef = ToolsUIApplication.Instance.FileTabDefinitions.FirstOrDefault(d => d.Name == pageName);

            if (pageTabDef != null)
            {
                this.IsFileTabOpen = true;
                this.fileTabControl.SelectedItem = pageTabDef;
            }
        }

        void OnUIModeChanged(object sender, EventArgs e)
        {
            var mode = this.shortcutManager.UIMode;

            if (string.IsNullOrEmpty(mode) || !mode.StartsWith("File"))
            {
                this.IsFileTabOpen = false;
            }

            if (string.IsNullOrEmpty(mode) || !mode.StartsWith("DocSelect"))
            {
                this.IsDocumentDropdownOpen = false;
            }
        }

        void OnEmptyModePushed(object sender, EventArgs e)
        {
            LeaveShortcutMode();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (this.inShortcutMode)
                {
                    if (!this.shortcutManager.PopUISubMode())
                    {
                        LeaveShortcutMode();
                    }
                }
                else
                {
                    this.IsFileTabOpen = false;
                }

                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        void RecomputeLayoutShortcutKeys()
        {
            var pages = new Dictionary<Key, LayoutDefinition>();

            // The fixed keys are currently "F" (for FILE) and "D" (for the documents dropdown)
            pages[Key.F] = null;
            pages[Key.D] = null;

            foreach (var layoutDef in this.Application.LayoutDefinitions)
            {
                string header = string.IsNullOrEmpty(layoutDef.Header) ? null : layoutDef.Header.ToUpperInvariant();
                Key defaultKey = Key.None;

                for (int i = 0; i < header.Length; i++)
                {
                    string defaultKeyText = header == null ? string.Format(CultureInfo.InvariantCulture, "D{0}", i) : header.Substring(i, 1);

                    if (!Enum.TryParse<Key>(defaultKeyText, out defaultKey))
                    {
                        defaultKey = Key.None;
                    }

                    if (defaultKey != Key.None && !pages.ContainsKey(defaultKey))
                    {
                        break;
                    }
                }

                pages[defaultKey] = layoutDef;
            }

            foreach (var kvp in pages)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.ShortcutKey = kvp.Key == Key.None ? null : kvp.Key.ToString();
                }
            }
        }

        void RecreateLayoutCollectionFromScratch()
        {
            if (this.Layouts.Count > 0)
            {
                Debug.Fail("Resetting the layouts collection when non-empty -- that's painful!  Avoid it!");
                foreach (var layout in this.Layouts)
                {
                    layout.LayoutDefinition.HeaderChanged -= OnLayoutDefinitionHeaderChanged;
                }

                this.Layouts.Clear();
            }

            foreach (var layoutDef in this.Application.LayoutDefinitions)
            {
                this.Layouts.Add(new LayoutInstance(this.ServiceProvider, this.LayoutTabControl, layoutDef, false));
                layoutDef.HeaderChanged += OnLayoutDefinitionHeaderChanged;
            }

            RecomputeLayoutShortcutKeys();
            UpdateLayoutVisibility(true);
        }

        void UpdateLayoutVisibility(bool forceSwitchToMatchingKind)
        {
            var doc = this.documentTracker.ActiveDocument;
            string factoryName = null;

            if (doc != null)
            {
                factoryName = doc.DocumentFactoryName;
            }

            bool switchToMatchingKind = forceSwitchToMatchingKind || !StringComparer.OrdinalIgnoreCase.Equals(factoryName, this.currentDocumentFactoryName);
            LayoutInstance switchTo = null;

            this.currentDocumentFactoryName = factoryName;

            foreach (var layout in this.Layouts)
            {
                if (layout.LayoutDefinition.DocumentFactoryName == null)
                {
                    layout.IsVisible = true;
                    if (switchToMatchingKind && ((switchTo == null) || (switchTo.ActivationIndex < layout.ActivationIndex)))
                    {
                        switchTo = layout;
                    }
                }
                else
                {
                    layout.IsVisible = StringComparer.OrdinalIgnoreCase.Equals(layout.LayoutDefinition.DocumentFactoryName, factoryName);
                    if (switchToMatchingKind && layout.IsVisible && ((switchTo == null) || (switchTo.ActivationIndex < layout.ActivationIndex) || (switchTo.LayoutDefinition.DocumentFactoryName == null)))
                    {
                        switchTo = layout;
                    }
                }
            }

            if (switchTo != null)
            {
                this.LayoutTabControl.SelectedItem = switchTo;
            }

            if (forceSwitchToMatchingKind && this.IsLoaded)
            {
                var activeLayout = this.LayoutTabControl.SelectedItem as IActivationSite;

                if (activeLayout != null)
                {
                    activeLayout.TunnelActivation();
                }
            }
        }

        IServiceProvider CreateServiceProvider()
        {
            var container = new ServiceContainer(this.Application.RootServiceProvider);

            container.AddService(typeof(ToolsUIWindow), this);
            this.documentTracker = new WindowDocumentTracker(this, this.Application.RootServiceProvider);
            container.AddService(typeof(IActiveDocumentTracker), this.documentTracker);
            return container;
        }

        protected void InitializeWindow()
        {
            var style = this.FindResource(typeof(ToolsUIWindow));
            if (style != null)
            {
                this.Style = (Style)style;
            }

            if (this.ServiceProvider == null)
            {
                this.ServiceProvider = CreateServiceProvider();
                this.Application.LoadWindowState(this);
            }

            this.documentTracker.ActiveDocumentChanged += OnActiveDocumentChanged;

            var mb = new MultiBinding
            {
                Converter = new DocumentNameAndWindowCountToTitleConverter
                {
                    SingleWindowFormatString = StringResources.SingleWindowTitleDocFmt,
                    MultipleWindowMainFormatString = StringResources.MainWindowTitleDocFmt,
                    MultipleWindowAuxFormatString = StringResources.AuxWindowTitleDocFmt
                }
            };
            mb.Bindings.Add(new Binding { Source = this.DocumentTracker, Path = new PropertyPath("ActiveDocument.DisplayName") });
            mb.Bindings.Add(new Binding { Source = ToolsUIApplication.Instance, Path = new PropertyPath("WindowCount") });
            mb.Bindings.Add(new Binding { Source = this, Path = new PropertyPath("IsMainWindow") });
            mb.FallbackValue = ToolsUIApplication.Instance.AppTitle;
            this.SetBinding(TitleProperty, mb);

            OnWindowInitialized(this.ServiceProvider as ServiceContainer);
        }

        protected virtual void OnWindowInitialized(ServiceContainer serviceContainer)
        {
        }

        protected virtual void OnNewWindowCreated(ToolsUIWindow newWindow)
        {
        }

        ToolsUIWindow DuplicateWindow()
        {
            var window = ToolsUIApplication.Instance.CreateWindow();
            var thisState = this.SaveWindowState("Duplicate");

            window.duplicatingWindow = this;
            window.LoadWindowState(thisState);
            OnNewWindowCreated(window);

            window.Loaded += OnDuplicateWindowLoaded;
            return window;
        }

        void OnDuplicateWindowLoaded(object sender, RoutedEventArgs e)
        {
            var window = sender as ToolsUIWindow;

            if (window != null)
            {
                window.LayoutTabControl.SelectedIndex = this.LayoutTabControl.SelectedIndex;
                window.Loaded -= OnDuplicateWindowLoaded;
            }
        }

        void OnNewWindowButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var newWindow = DuplicateWindow();

            newWindow.Left = this.Left;
            newWindow.Top = this.Top;
            newWindow.Show();
            newWindow.BeginDragMove(e.MouseDevice);
            e.Handled = true;
        }

        void OnActiveDocumentChanged(object sender, EventArgs e)
        {
            UpdateLayoutVisibility(true);
        }

        void OnLayoutDefinitionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var layoutDef = (LayoutDefinition)e.NewItems[0];

                this.Layouts.Insert(e.NewStartingIndex, new LayoutInstance(this.ServiceProvider, this.LayoutTabControl, layoutDef, false));
                layoutDef.HeaderChanged += OnLayoutDefinitionHeaderChanged;
                RecomputeLayoutShortcutKeys();
                UpdateLayoutVisibility(false);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var layoutDef = (LayoutDefinition)e.OldItems[0];
                var layout = this.Layouts.FirstOrDefault(l => l.LayoutDefinition == layoutDef);

                this.Layouts.Remove(layout);
                layout.CloseAllViews();
                layoutDef.HeaderChanged -= OnLayoutDefinitionHeaderChanged;
                RecomputeLayoutShortcutKeys();
                UpdateLayoutVisibility(false);
            }
            else
            {
                RecreateLayoutCollectionFromScratch();
            }
        }

        void OnLayoutDefinitionHeaderChanged(object sender,  EventArgs e)
        {
            RecomputeLayoutShortcutKeys();
        }

        void ReplicateLayoutStates()
        {
            if (this.duplicatingWindow != null)
            {
                for (int i = 0; i < this.LayoutTabControl.Items.Count; i++)
                {
                    var ourLayout = this.LayoutTabControl.Items[i] as LayoutInstance;
                    var toCopyLayout = this.duplicatingWindow.LayoutTabControl.Items[i] as LayoutInstance;

                    ourLayout.ReplicateLayoutState(toCopyLayout);
                }

                this.duplicatingWindow = null;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.mover = this.GetTemplateChild("PART_Mover") as FrameworkElement;
            this.fileTabControl = this.GetTemplateChild("PART_FileTabControl") as FileTabControl;

            if (this.mover != null)
            {
                this.mover.MouseDown += OnMoverMouseDown;
            }

            this.sysMenuIcon = this.GetTemplateChild("PART_SysMenuIcon") as FrameworkElement;

            if (this.sysMenuIcon != null)
            {
                this.sysMenuIcon.MouseDown += OnIconMouseDown;
            }

            AssignSizer("PART_TopLeftSizer", SizeTopLeft);
            AssignSizer("PART_TopRightSizer", SizeTopRight);
            AssignSizer("PART_TopSizer", SizeTop);
            AssignSizer("PART_LeftSizer", SizeLeft);
            AssignSizer("PART_RightSizer", SizeRight);
            AssignSizer("PART_BottomLeftSizer", SizeBottomLeft);
            AssignSizer("PART_BottomRightSizer", SizeBottomRight);
            AssignSizer("PART_BottomSizer", SizeBottom);

            this.qatItemsControl = this.GetTemplateChild("PART_QATItemsControl") as QATItemsControl;

            if (this.qatItemsControl != null)
            {
                this.qatItemsControl.ItemContainerGenerator.StatusChanged += (o, e) => { EnableNewWindowButtonDrag(); };
                this.CommandBindings.Add(new CommandBinding(ViewLayoutEditor.CreateNewWindowCommand, (s, e) => { }));
                EnableNewWindowButtonDrag();
            }

            this.LayoutTabControl = this.GetTemplateChild("PART_LayoutTabControl") as LayoutTabControl;
            this.LayoutTabControl.IsVisibleChanged += OnLayoutTabControlIsVisibleChanged;

            this.Application.LayoutDefinitions.CollectionChanged += OnLayoutDefinitionsCollectionChanged;
            RecreateLayoutCollectionFromScratch();

            LoadDeferredWindowState();
            ReplicateLayoutStates();
        }

        void EnableNewWindowButtonDrag()
        {
            if (this.qatItemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                if (this.newWindowButton != null)
                {
                    this.newWindowButton.PreviewMouseLeftButtonDown -= OnNewWindowButtonPreviewMouseLeftButtonDown;
                }

                var def = this.qatItemsControl.Items.OfType<QATButtonDefinition>().FirstOrDefault(d => d.Command == ViewLayoutEditor.CreateNewWindowCommand);

                if (def != null)
                {
                    this.newWindowButton = this.qatItemsControl.ItemContainerGenerator.ContainerFromItem(def) as Button;

                    if (this.newWindowButton != null)
                    {
                        this.newWindowButton.PreviewMouseLeftButtonDown += OnNewWindowButtonPreviewMouseLeftButtonDown;
                    }
                }
            }
        }

        void OnSourceInitialized(object sender, EventArgs e)
        {
            this.hwndSource = HwndSource.FromVisual(this) as HwndSource;
            this.hwndSource.AddHook(WindowProc);
            this.LocationChanged += OnWindowLocationChanged;
            this.SizeChanged += OnWindowSizeChanged;
        }

        void UpdateNormalWindowRect()
        {
            // Need to dispatch this to later -- location change event fires before window state is updated from normal to maximized
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.NormalWindowRect = new Rect(this.Left, this.Top, this.ActualWidth, this.ActualHeight);
                }
            }), null);
        }

        public XElement SaveWindowState(string elementName)
        {
            return new XElement(elementName,
                    new XAttribute("Width", this.NormalWindowRect.Width),
                    new XAttribute("Height", this.NormalWindowRect.Height),
                    new XAttribute("Left", this.NormalWindowRect.X),
                    new XAttribute("Top", this.NormalWindowRect.Y),
                    new XAttribute("IsMaximized", (this.WindowState == WindowState.Maximized)),
                    this.LayoutTabControl.SaveLayoutStates());
        }

        public void LoadWindowState(XElement windowState)
        {
            Rect windowRect;
            bool isMaximized;

            if (ToolsUIApplication.TryLoadWindowRect(windowState, out windowRect, out isMaximized))
            {
                this.NormalWindowRect = windowRect;
                this.Width = windowRect.Width;
                this.Height = windowRect.Height;
                this.Left = windowRect.X;
                this.Top = windowRect.Y;

                if (isMaximized)
                {
                    if (!this.IsLoaded)
                    {
                        RoutedEventHandler handler = null;

                        // Can't set maximized here; need to do it after the window is loaded so that it maximizes on the correct monitor
                        handler = (s, e) => { this.WindowState = WindowState.Maximized; this.Loaded -= handler; };
                        this.Loaded += handler;
                    }
                    else
                    {
                        this.WindowState = WindowState.Maximized;
                    }
                }
            }

            this.deferredWindowState = windowState;

            if (this.LayoutTabControl != null)
            {
                // Template has been applied, so we don't need to defer this
                LoadDeferredWindowState();
            }
        }

        void LoadDeferredWindowState()
        {
            if (this.deferredWindowState != null)
            {
                this.LayoutTabControl.LoadLayoutStates(this.deferredWindowState.Elements("Layout"));
                this.deferredWindowState = null;
                UpdateLayoutVisibility(true);
            }
        }

        void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateNormalWindowRect();
        }

        void OnWindowLocationChanged(object sender, EventArgs e)
        {
            UpdateNormalWindowRect();
        }

        protected override void OnClosed(EventArgs e)
        {
            foreach (var layout in this.Layouts)
            {
                layout.CloseAllViews();
            }

            if (this.hwndSource != null && !this.hwndSource.IsDisposed)
            {
                this.hwndSource.RemoveHook(WindowProc);
                this.hwndSource.Dispose();
                this.hwndSource = null;
            }

            base.OnClosed(e);
        }

        void OnCloseWindowExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        void OnExitExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.IsFileTabOpen = false;
            this.Application.RequestShutdown();
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:/* WM_GETMINMAXINFO */
                    WmGetMinMaxInfo(hwnd, lParam, this.inDragMove);
                    handled = true;
                    break;
            }

            return (System.IntPtr)0;
        }

        void AssignSizer(string name, Action<DragDeltaEventArgs> action)
        {
            var thumb = this.GetTemplateChild(name) as Thumb;

            if (thumb != null)
            {
                this.sizerTable[name] = action;
            }
        }

        void OnIconMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    this.Close();
                }
                else
                {
                    var pos = this.sysMenuIcon.PointToScreen(new Point(0, this.sysMenuIcon.ActualHeight));
                    ShowSystemMenu((int)pos.X, (int)pos.Y);
                }
            }
        }

        void OnMoverMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    if (this.WindowState == WindowState.Maximized)
                    {
                        this.WindowState = WindowState.Normal;
                    }
                    else
                    {
                        this.WindowState = WindowState.Maximized;
                    }
                }
                else
                {
                    BeginDragMove(e.MouseDevice);
                }
            }
            else
            {
                var pos = this.PointToScreen(e.GetPosition(this));
                ShowSystemMenu((int)pos.X, (int)pos.Y);
            }
        }

        void BeginDragMove(MouseDevice device)
        {
            if (device == null)
            {
                device = Mouse.PrimaryDevice;
            }

            if (device.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (this.WindowState == WindowState.Maximized)
            {
                device.Capture(this.mover);
                this.mover.MouseMove += OnMoverMouseMove;
                this.mover.LostMouseCapture += OnMoverLostCapture;
                this.mover.MouseLeftButtonUp += OnMoverMouseLeftButtonUp;
                this.moveStartPoint = device.GetPosition(this);
            }
            else
            {
                this.inDragMove = true;
                this.DragMove();
                this.inDragMove = false;
            }
        }

        void OnMoverMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.MouseDevice.GetPosition(this);
            var delta = pos - this.moveStartPoint;

            if (Math.Abs(delta.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(delta.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var percent = pos.X / this.ActualWidth;
                Point screenPos = this.PointToScreenIndependent(pos);
                var left = screenPos.X - (RestoreBounds.Width * percent);
                var top = screenPos.Y - pos.Y;

                this.WindowState = WindowState.Normal;
                this.Left = left;
                this.Top = top;

                e.MouseDevice.Capture(null);
                this.inDragMove = true;
                DragMove();
                this.inDragMove = false;
            }
        }

        void OnMoverLostCapture(object sender, MouseEventArgs e)
        {
            this.mover.MouseMove -= OnMoverMouseMove;
            this.mover.LostMouseCapture -= OnMoverLostCapture;
            this.mover.MouseLeftButtonUp -= OnMoverMouseLeftButtonUp;
        }

        void OnMoverMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(null);
        }

        void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            var thumb = e.OriginalSource as Thumb;

            if (thumb != null && thumb.Name != null && this.sizerTable.ContainsKey(thumb.Name))
            {
                if (!this.sizeFixed)
                {
                    this.Width = this.ActualWidth;
                    this.Height = this.ActualHeight;
                    this.sizeFixed = true;
                }

                this.currentSizer = thumb;
                this.currentSizeAction = this.sizerTable[thumb.Name];
                this.moveStartPoint = new Point(this.Left, this.Top);
                this.moveStartSize = new Size(this.ActualWidth, this.ActualHeight);

                this.focusedElement = Keyboard.FocusedElement as DependencyObject;
                if (this.focusedElement != null)
                {
                    Keyboard.AddPreviewKeyDownHandler(this.focusedElement, new KeyEventHandler(OnPreviewKeyDown));
                }
            }
            else
            {
                this.currentSizeAction = null;
            }
        }

        void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && this.currentSizer != null)
            {
                this.currentSizer.CancelDrag();
            }
        }

        void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (this.currentSizeAction != null)
            {
                this.currentSizeAction(e);
            }
        }

        void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (e.Canceled)
            {
                this.Left = this.moveStartPoint.X;
                this.Top = this.moveStartPoint.Y;
                this.Width = this.moveStartSize.Width;
                this.Height = this.moveStartSize.Height;
            }

            this.currentSizer = null;
            this.currentSizeAction = null;
        }

        void SizeLeft(DragDeltaEventArgs e)
        {
            var delta = Math.Min(this.Width - this.MinWidth, e.HorizontalChange);
            this.Width = this.Width - delta;
            this.Left += delta;
        }

        void SizeTop(DragDeltaEventArgs e)
        {
            var delta = Math.Min(this.Height - this.MinHeight, e.VerticalChange);
            this.Height = this.Height - delta;
            this.Top += delta;
        }

        void SizeRight(DragDeltaEventArgs e)
        {
            this.Width = Math.Max(this.MinWidth, this.Width + e.HorizontalChange);
        }

        void SizeBottom(DragDeltaEventArgs e)
        {
            this.Height = Math.Max(this.MinHeight, this.Height + e.VerticalChange);
        }

        void SizeTopLeft(DragDeltaEventArgs e)
        {
            SizeTop(e);
            SizeLeft(e);
        }

        void SizeTopRight(DragDeltaEventArgs e)
        {
            SizeTop(e);
            SizeRight(e);
        }

        void SizeBottomLeft(DragDeltaEventArgs e)
        {
            SizeBottom(e);
            SizeLeft(e);
        }

        void SizeBottomRight(DragDeltaEventArgs e)
        {
            SizeBottom(e);
            SizeRight(e);
        }

        void OnMinimizeCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.WindowState != WindowState.Minimized;
            e.Handled = true;
        }

        void OnMinimizeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            e.Handled = true;
        }

        void OnMaximizeCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (this.WindowState != WindowState.Maximized);
            e.Handled = true;
        }

        void OnMaximizeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
            e.Handled = true;
        }

        void OnRestoreCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (this.WindowState != WindowState.Normal);
            e.Handled = true;
        }

        void OnRestoreExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            e.Handled = true;
        }

        void OnToggleFileTabExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.IsFileTabOpen = !this.IsFileTabOpen;
            this.shortcutManager.AreShortcutAdornmentsVisible = this.inShortcutMode;
        }

        void OnOpenDocumentDropdownExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.IsDocumentDropdownOpen = true;
            this.shortcutManager.AreShortcutAdornmentsVisible = this.inShortcutMode;
        }

        void OnActivateDocumentExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var d = e.Parameter as WindowDocumentTracker.DocumentWrapper;

            if (d != null)
            {
                this.documentTracker.SelectedDocument = d;
                this.LeaveShortcutMode();
            }
        }

        void OnEditLayoutsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.IsFileTabOpen = true;

            var fileTabDef = ToolsUIApplication.Instance.FileTabDefinitions.FirstOrDefault(d => d.Name == "PART_ViewLayoutEditor");

            if (fileTabDef != null)
            {
                var instance = this.LayoutTabControl.SelectedItem as LayoutInstance;

                if (instance != null)
                {
                    this.CurrentLayoutDefinitionForEdit = instance.LayoutDefinition;
                }
                
                this.fileTabControl.SelectedItem = fileTabDef;
            }
        }

        void OnRevertToDefaultWindowStateExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var n = this.ServiceProvider.GetService(typeof(IUserNotificationService)) as IUserNotificationService;

            if (n != null)
            {
                var res = n.ShowMessageBox(StringResources.ConfirmRevertToDefaultWindowState, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                if (res == MessageBoxResult.Yes)
                {
                    ToolsUIApplication.Instance.RevertToDefaultWindowState();
                }
            }
        }

        void ShowSystemMenu(int x, int y)
        {
            GetSystemMenu(this.hwndSource.Handle, true);
            IntPtr hSysMenu = GetSystemMenu(this.hwndSource.Handle, false);
            var selected = TrackPopupMenu(hSysMenu, TPM_RIGHTBUTTON | TPM_NONOTIFY | TPM_RETURNCMD, x, y, 0, this.hwndSource.Handle, IntPtr.Zero);
            PostMessage(this.hwndSource.Handle, 0, IntPtr.Zero, IntPtr.Zero);

            if (selected != 0)
            {
                PostMessage(this.hwndSource.Handle, WM_SYSCOMMAND, new IntPtr(selected), IntPtr.Zero);
            }
        }

        void OnLayoutTabControlIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!this.IsFileTabOpen && this.LayoutTabControl is IActivationSite)
            {
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    ((IActivationSite)this.LayoutTabControl).TunnelActivation();
                }));
            }
        }

        static void OnIsFileTabOpenChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ToolsUIWindow window = obj as ToolsUIWindow;

            if (window != null)
            {
                if (window.IsFileTabOpen)
                {
                    window.shortcutManager.UIMode = "File";
                }
                else
                {
                    window.shortcutManager.UIMode = string.Empty;
                }
            }
        }

        static void OnIsDocumentDropdownOpenChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ToolsUIWindow window = obj as ToolsUIWindow;

            if (window != null)
            {
                if (window.IsDocumentDropdownOpen)
                {
                    window.shortcutManager.UIMode = "DocSelect";
                }
                else
                {
                    window.shortcutManager.UIMode = string.Empty;
                }

                if (window.LayoutTabControl != null && !window.IsDocumentDropdownOpen)
                {
                    var activeLayout = window.LayoutTabControl.SelectedItem as IActivationSite;

                    if (activeLayout != null)
                    {
                        activeLayout.TunnelActivation();
                    }
                }
            }
        }

        class WindowDocumentTracker : ServiceBase, IActiveDocumentTracker, INotifyPropertyChanged
        {
            DocumentManager serviceFieldDocumentManager;
            Document activeDocument;
            IActiveDocumentTracker mainWindowDocumentTracker;
            bool isTrackingMainWindow;
            ToolsUIWindow thisWindow;
            ToolsUIWindow mainWindow;
            INotifyCollectionChanged documentListAsINCC;
            ActiveDocumentCookie activeDocumentCookie;
            ObservableCollection<DocumentWrapper> documentList;
            DocumentWrapper selectedDocument;

            public WindowDocumentTracker(ToolsUIWindow window, IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
                this.thisWindow = window;
                this.mainWindow = ToolsUIApplication.Instance.MainToolsUIWindow ?? window;      // If MainToolsUIWindow is null, the window is (about to be) the main window
                this.documentList = new ObservableCollection<DocumentWrapper>();

                if (this.thisWindow != this.mainWindow)
                {
                    // This is not the main window.  All secondary windows are born tracking the main window's
                    // active document.
                    this.mainWindowDocumentTracker = this.mainWindow.ServiceProvider.GetService(typeof(IActiveDocumentTracker)) as IActiveDocumentTracker;

                    if (this.mainWindowDocumentTracker != null)
                    {
                        this.mainWindowDocumentTracker.ActiveDocumentChanged += OnMainWindowActiveDocumentChanged;
                        this.activeDocumentCookie = new ActiveDocumentCookie(this.thisWindow);
                        this.activeDocument = this.mainWindowDocumentTracker.ActiveDocument;
                        this.activeDocumentCookie.WrappedDocument = this.mainWindowDocumentTracker.ActiveDocument;
                        this.isTrackingMainWindow = true;
                        this.mainWindow.Closed += OnMainWindowClosed;
                    }
                }

                this.documentListAsINCC = this.DocumentManager.Documents as INotifyCollectionChanged;

                if (this.documentListAsINCC != null)
                {
                    this.documentListAsINCC.CollectionChanged += OnDocumentListChanged;
                }

                InitializeDocumentList();
                this.selectedDocument = this.activeDocumentCookie ?? this.documentList.FirstOrDefault(d => d.WrappedDocument == this.activeDocument);
            }

            public Document ActiveDocument
            {
                get
                {
                    return this.activeDocument;
                }
                private set
                {
                    if (this.activeDocument != value)
                    {
                        if (this.activeDocument != null)
                        {
                            this.activeDocument.Closed -= OnActiveDocumentClosed;
                        }

                        this.activeDocument = value;

                        if (this.activeDocument != null)
                        {
                            this.activeDocument.Closed += OnActiveDocumentClosed;

                            var wrapper = FindWrapperForDocument(this.activeDocument);

                            if (wrapper != null)
                            {
                                this.documentList.Remove(wrapper);
                                this.documentList.Insert(this.activeDocumentCookie != null ? 1 : 0, wrapper);
                                AssignShortcuts();
                            }
                        }

                        var handler = this.ActiveDocumentChanged;

                        if (handler != null)
                        {
                            handler(this, EventArgs.Empty);
                        }

                        Notify("ActiveDocument");
                        Notify("Documents");
                    }
                }
            }

            public bool IsTrackingMainWindow
            {
                get
                {
                    return this.isTrackingMainWindow;
                }
                set
                {
                    if (this.isTrackingMainWindow != value)
                    {
                        this.isTrackingMainWindow = value;
                        Notify("IsTrackingMainWindow");
                    }
                }
            }

            public DocumentManager DocumentManager { get { return EnsureService(ref serviceFieldDocumentManager); } }

            public IEnumerable<object> DocumentPickList
            {
                get
                {
                    return documentList;
                }
            }

            public IEnumerable<Document> Documents
            {
                get
                {
                    return this.documentList.Where(w => !(w is ActiveDocumentCookie)).Select(w => w.WrappedDocument);
                }
            }

            public DocumentWrapper SelectedDocument
            {
                get
                {
                    return this.selectedDocument;
                }
                set
                {
                    if (this.selectedDocument != value)
                    {
                        // A trick here:  value is either a document wrapper, the ActiveDocumentCookie, or null.  
                        // If it's a document wrapper, then activate the document it wraps
                        // If it's the ActiveDocumentCookie, then activate null (makes us track the main window's active document)
                        // These two cases are handled by the DocumentWrapper's virtual DocumentToActivate property.
                        //
                        // If null, we want to do *nothing* -- it only happens when the underlying document
                        // list changes beneath us and the currently selected document is no longer present, 
                        // which happens when a document is closed.  Since we handle the document closed event
                        // differently (downstream), we let that handler select a different document.
                        if (value != null)
                        {
                            ActivateDocument(value.DocumentToActivate);
                        }

                        this.thisWindow.LeaveShortcutMode();
                    }
                }
            }

            void InitializeDocumentList()
            {
                if (this.activeDocumentCookie != null)
                {
                    this.documentList.Add(this.activeDocumentCookie);
                }

                foreach (var d in this.DocumentManager.Documents)
                {
                    this.documentList.Add(new DocumentWrapper(this.thisWindow) { WrappedDocument = d });
                }

                AssignShortcuts();
            }

            DocumentWrapper FindWrapperForDocument(Document doc)
            {
                return this.documentList.FirstOrDefault(w => (!(w is ActiveDocumentCookie)) && (w.WrappedDocument == doc));
            }

            void AssignShortcuts()
            {
                for (int i = 0; i < this.documentList.Count; i++)
                {
                    this.documentList[i].Shortcut = (i < 10) ? string.Format(CultureInfo.InvariantCulture, "D{0}:DocSelect", i < 9 ? i + 1 : 0) : null;
                }
            }

            void OnMainWindowClosed(object sender, EventArgs e)
            {
                this.thisWindow.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // We need to re-establish the main window.  It might even be us now!
                    this.mainWindowDocumentTracker.ActiveDocumentChanged -= OnMainWindowActiveDocumentChanged;
                    this.mainWindow.Closed -= OnMainWindowClosed;

                    this.mainWindow = ToolsUIApplication.Instance.MainToolsUIWindow;

                    if (this.thisWindow != this.mainWindow)
                    {
                        this.mainWindowDocumentTracker = this.mainWindow.ServiceProvider.GetService(typeof(IActiveDocumentTracker)) as IActiveDocumentTracker;
                    }
                    else
                    {
                        this.mainWindowDocumentTracker = null;
                    }

                    if (this.mainWindowDocumentTracker != null)
                    {
                        this.mainWindowDocumentTracker.ActiveDocumentChanged += OnMainWindowActiveDocumentChanged;
                        this.activeDocumentCookie.WrappedDocument = this.mainWindowDocumentTracker.ActiveDocument;
                        this.IsTrackingMainWindow = true;
                        this.mainWindow.Closed += OnMainWindowClosed;
                    }
                    else
                    {
                        this.documentList.Remove(this.activeDocumentCookie);
                        this.activeDocumentCookie = null;
                        this.IsTrackingMainWindow = false;
                        AssignShortcuts();
                    }

                }), DispatcherPriority.Background);
            }

            void OnDocumentListChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    // Document manager only adds documents to the end, so we do the same.
                    // The document will probably be activated shortly, at which time we'll
                    // bubble it to the top of our list.
                    Debug.Assert(e.NewItems != null && e.NewItems.Count == 1, "The document manager must only do Add and Remove on the documents list!");
                    this.documentList.Add(new DocumentWrapper(this.thisWindow) { WrappedDocument = e.NewItems[0] as Document });
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    Debug.Assert(e.OldItems != null && e.OldItems.Count == 1, "The document manager must only do Add and Remove on the documents list!");
                    var wrapper = FindWrapperForDocument((Document)e.OldItems[0]);
                    if (wrapper != null)
                    {
                        this.documentList.Remove(wrapper);
                    }
                }
                else
                {
                    // Our activation order is lost in this case, but this case should never happen...
                    Debug.Fail("The document manager must only do Add and Remove on the documents list!");
                    this.documentList.Clear();
                    InitializeDocumentList();
                }

                AssignShortcuts();
            }

            void OnActiveDocumentClosed(object sender, EventArgs e)
            {
                if (this.thisWindow != this.mainWindow)
                {
                    // Regardless of what we're tracking now, start tracking the main window's active document.
                    // That may be what we're doing already, but it doesn't hurt to be repetitive in this case.
                    ActivateDocument(null);
                }
                else
                {
                    // Activate the next most-recently-active document.
                    var wrapper = this.documentList.FirstOrDefault(d => !(d is ActiveDocumentCookie));
                    ActivateDocument(wrapper == null ? null : wrapper.WrappedDocument);
                }
            }

            public event EventHandler ActiveDocumentChanged;

            public void ActivateDocument(Document document)
            {
                if (this.mainWindowDocumentTracker != null)
                {
                    // This is a secondary window.  If we're setting the active document to a specific document,
                    // that means we no longer track the main window's active document.  Conversely, if we're setting
                    // it to null, it means we revert to tracking the main window's active document.
                    if (document == null)
                    {
                        this.ActiveDocument = this.mainWindowDocumentTracker.ActiveDocument;
                        this.selectedDocument = this.activeDocumentCookie;
                        this.IsTrackingMainWindow = true;
                    }
                    else
                    {
                        this.ActiveDocument = document;
                        this.selectedDocument = FindWrapperForDocument(document);
                        this.IsTrackingMainWindow = false;
                    }
                }
                else
                {
                    this.ActiveDocument = document;
                    this.selectedDocument = FindWrapperForDocument(document);
                }

                // Note that we don't use the SelectedDocument property setter -- it is called by the XAML code
                // in response to the combo box selection, and results in calls to this method.
                Notify("SelectedDocument");
            }

            void OnMainWindowActiveDocumentChanged(object sender, EventArgs e)
            {
                if (this.isTrackingMainWindow)
                {
                    this.ActiveDocument = this.mainWindowDocumentTracker.ActiveDocument;
                }

                this.activeDocumentCookie.WrappedDocument = this.mainWindowDocumentTracker.ActiveDocument;
            }

            void Notify(string property)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(property));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public class DocumentWrapper : INotifyPropertyChanged
            {
                protected Document wrappedDocument;
                string shortcut;

                public DocumentWrapper(ToolsUIWindow window)
                {
                    this.Window = window;
                }

                public ToolsUIWindow Window { get; private set; }
                public DocumentCategory Category { get { return this.wrappedDocument == null ? null : this.wrappedDocument.Category; } }
                public bool IsModified { get { return this.wrappedDocument == null ? false : this.wrappedDocument.IsModified; } }
                public virtual string DisplayName { get { return this.wrappedDocument == null ? null : this.wrappedDocument.DisplayName; } }
                public virtual Document DocumentToActivate { get { return this.wrappedDocument; } }

                public string Shortcut
                {
                    get
                    {
                        return this.shortcut;
                    }
                    set
                    {
                        this.shortcut = value;
                        Notify("Shortcut");
                    }
                }

                public Document WrappedDocument
                {
                    get
                    {
                        return this.wrappedDocument;
                    }
                    set
                    {
                        if (this.wrappedDocument != null)
                        {
                            this.wrappedDocument.PropertyChanged -= OnActiveDocumentPropertyChanged;
                        }

                        this.wrappedDocument = value;

                        if (this.wrappedDocument != null)
                        {
                            this.wrappedDocument.PropertyChanged += OnActiveDocumentPropertyChanged;
                        }

                        Notify("Category");
                        Notify("IsModified");
                        Notify("DisplayName");
                    }
                }

                void OnActiveDocumentPropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    Notify(e.PropertyName);
                }

                protected void Notify(string property)
                {
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(property));
                    }
                }

                public override string ToString()
                {
                    return this.wrappedDocument == null ? "" : this.wrappedDocument.DisplayName;
                }

                public event PropertyChangedEventHandler PropertyChanged;
            }

            class ActiveDocumentCookie : DocumentWrapper
            {
                public ActiveDocumentCookie(ToolsUIWindow window) : base(window) { }

                public override string DisplayName { get { return this.wrappedDocument == null ? "Active" : string.Format("Active ({0})", this.wrappedDocument.DisplayName); } }
                public override string ToString()
                {
                    return "(Active)";
                }

                public override Document DocumentToActivate { get { return null; } }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;

            public MONITORINFO() { this.cbSize = Marshal.SizeOf(this); }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }

        static void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam, bool inDragMove)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            IntPtr monitor;

            if (inDragMove)
            {
                POINT pt;
                GetCursorPos(out pt);

                // Because the window may not have moved to this monitor yet, use the mouse position
                // instead of the hwnd to find the monitor (rubber-band drag mode causes this)
                monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            }
            else
            {
                monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            }

            if (monitor != System.IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        const int MONITOR_DEFAULTTONULL = 0x00000000;
        const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;
        const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        const int WM_SYSCOMMAND = 0x112;
        const int WM_NCLBUTTONDOWN = 0x00A1;
        const int WM_NCRBUTTONDOWN = 0x00A4;
        const UInt32 WS_SYSMENU = 0x80000;
        const int TPM_RIGHTBUTTON = 0x0002;
        const int TPM_NONOTIFY = 0x0080;
        const int TPM_RETURNCMD = 0x0100;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int reserved, IntPtr hwnd, IntPtr rec);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
}
