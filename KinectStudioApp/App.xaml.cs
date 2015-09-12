//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Microsoft.Kinect.Tools;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    public partial class App : ToolsUIApplication
    {
        protected override void OnExit(ExitEventArgs e)
        {
            DebugHelper.AssertUIThread();

            base.OnExit(e);

            if (this.kstudioService != null)
            {
                this.kstudioService.Shutdown();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public bool IsKinectForWindows
        {
            get
            {
                DebugHelper.AssertUIThread();
                return true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public bool IsKinectForXbox
        {
            get
            {
                DebugHelper.AssertUIThread();
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public double PhysicalMemoryMB
        {
            get
            {
                return (new Microsoft.VisualBasic.Devices.ComputerInfo()).AvailablePhysicalMemory / (1024.0 * 1024.0);
            }
        }

        public OpenTabItemData OpenReadOnlyFileTabItem
        {
            get
            {
                DebugHelper.AssertUIThread();

                IMostRecentlyUsedService mruService = RootServiceProvider.GetService(typeof(IMostRecentlyUsedService)) as IMostRecentlyUsedService;
                if (mruService != null)
                {
                    return mruService.OpenReadOnlyFileTabControls.FirstOrDefault();
                }

                return null;
            }
        }

        public OpenTabItemData OpenWritableFileTabItem
        {
            get
            {
                DebugHelper.AssertUIThread();

                IMostRecentlyUsedService mruService = RootServiceProvider.GetService(typeof(IMostRecentlyUsedService)) as IMostRecentlyUsedService;
                if (mruService != null)
                {
                    return mruService.OpenWritableFileTabControls.FirstOrDefault();
                }

                return null;
            }
        }

        public override string AppTitle { get { return Strings.AppTitle; } }
        public override string AppName { get { return "KinectStudioWindows"; } }
        public override string DefaultWindowStateStreamName { get { return "KinectStudio.DefaultWindowState.xml"; } }
        public override string AppVersion { get { return "2.0"; } }

        // If you want to add other extension assemblies to the discovery process, return them here.
        public override IEnumerable<string> ExtensionAssemblies { get { return Enumerable.Empty<string>(); } }

        private SplashScreen splash = null;

        public App()
        {
            this.splash = new SplashScreen(@"\Images\Splash.png");
            this.splash.Show(false);

            InitializeComponent();

            string tabsResourceKey = "WindowsFileTabs";

            FileTabDefinition[] fileTabs = this.FindResource(tabsResourceKey) as FileTabDefinition[];
            if (fileTabs != null)
            {
                this.FileTabDefinitions = new ObservableCollection<FileTabDefinition>(fileTabs);
            }

            Uri uri = new Uri("/" + typeof(IPluginService).Assembly.GetName().Name + ";component/Resources.xaml", UriKind.Relative);

            ResourceDictionary pluginResources = new ResourceDictionary()
                {
                    Source = uri,
                };
            this.Resources.MergedDictionaries.Add(pluginResources);

            CompositionTarget.Rendering += (source, e) =>
                {
                    if (KStudio.ProcessNotifications())
                    {
                        CommandManager.InvalidateRequerySuggested();
                    }
                };

            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewKeyUpEvent, new KeyEventHandler(this.TextBox_PreviewKeyUp));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(this.TextBox_GotFocus));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseDownEvent, new MouseButtonEventHandler(this.TextBox_PreviewMouseDown));

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        [STAThreadAttribute()]
        public static void Main()
        {
            var app = new App();
            app.Run();
        }

        protected override void OnInitialized()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.kstudioService == null);

            this.kstudioService = this.RootServiceProvider.GetService(typeof(IKStudioService)) as IKStudioService;

            if (this.kstudioService != null)
            {
                this.kstudioService.Initialize(this.RootServiceProvider);
            }
        }

        protected override ToolsUIWindow CreateToolsUIWindow()
        {
            DebugHelper.AssertUIThread();

            string[] args = null;

            if (this.firstWindow)
            {
                args = Environment.GetCommandLineArgs();
                this.firstWindow = false;
            }

            MainWindow mainWindow = new MainWindow(args);

            if (args != null)
            {
                if (this.splash != null)
                {
                    IPluginService pluginService = this.RootServiceProvider.GetService(typeof(IPluginService)) as IPluginService;
                    if (pluginService != null)
                    {
                        pluginService.Initialize();
                    }

                    mainWindow.Loaded += (s, ea) =>
                        {
                            if (this.splash != null)
                            {
                                this.splash.Close(TimeSpan.Zero);
                                this.splash = null;
                            }
                        };

                }
            }

            return mainWindow;
        }

        protected override void OnShutdown()
        {
            DebugHelper.AssertUIThread();

            if (this.kstudioService != null)
            {
                this.kstudioService.StopMonitor();
                this.kstudioService.CloseRecording(false);
                this.kstudioService.ClosePlayback();
            }

            base.OnShutdown();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            bool logged = false;
            e.Handled = true;

            try
            {
                ILoggingService loggingService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(ILoggingService)) as ILoggingService;
                if (loggingService != null)
                {
                    loggingService.LogException(e.Exception);
                    logged = true;
                }
            }
            finally
            {
                if (!logged)
                {
                    Debug.WriteLine("Exception: " + e.ToString());
                }
            }

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }

        private void NullableComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Combo boxes can be annoying with null selection when using a collection view for the ItemsSource (since
            // ICollectionView cannot be composited out of the box), so this is the smallest workaround to get the 
            // desired behavior.

            ComboBox comboBox = sender as ComboBox;
            if ((comboBox != null) && !(comboBox.Tag is bool))
            {
#if DEBUG
                comboBox.KeyDown += NullableComboBox_KeyDown;
#endif // DEBUG

                string path = comboBox.Tag as string;
                if (path != null)
                {
                    Binding selectedItemBinding = new Binding(path);
                    comboBox.SetBinding(ComboBox.SelectedItemProperty, selectedItemBinding);

                    DataTrigger selectedItemDataTrigger = new DataTrigger()
                        { 
                            Binding = selectedItemBinding,
                        };

                    Setter selectedIndexSetter = new Setter(ComboBox.SelectedIndexProperty, -1);
                    selectedItemDataTrigger.Setters.Add(selectedIndexSetter);
                }


                comboBox.Tag = true;
            }
        }

#if DEBUG
        private void NullableComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e != null)
            {
                if (e.Key == Key.Delete)
                {
                    ComboBox comboBox = sender as ComboBox;
                    if (comboBox != null)
                    {
                        KStudioEventStream eventStream = comboBox.SelectedItem as KStudioEventStream;
                        if (eventStream != null)
                        {
                            EventStreamState ess = eventStream.UserState as EventStreamState;
                            if (ess != null)
                            {
                                if (ess.SelectedFilePlaybackStream != null)
                                {
                                    EventStreamState ess2 = ess.SelectedFilePlaybackStream.UserState as EventStreamState;

                                    if (ess2 != null)
                                    {
                                        e.Handled = true;

                                        ess2.SelectedLivePlaybackStream = null;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
#endif // DEBUG

        private void MetadataButton_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (e != null)
            {
                e.Handled = true;
            }

            ButtonBase button = sender as ButtonBase;
            if ((button != null) && button.IsEnabled)
            {
                button.ContextMenu = null;

                IMetadataViewService metadataViewService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(IMetadataViewService)) as IMetadataViewService;

                if (metadataViewService != null)
                {
                    IEnumerable<MetadataView> metadataViews = metadataViewService.GetMetadataViews(Window.GetWindow(button));
                    if (metadataViews.Count() > 0)
                    {
                        button.Focus();

                        button.ContextMenu = new ContextMenu()
                            {
                                DataContext = button.CommandParameter,
                                ItemContainerStyle = Resources["MetadataViewSelectorMenuItemStyle"] as Style,
                                ItemsSource = metadataViews,
                                PlacementTarget = button,
                                IsOpen = true,
                            };
                    }
                }
            }
        }

        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter) || (e.Key == Key.Return))
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    if ((textBox != null) && !textBox.AcceptsReturn)
                    {
                        BindingExpression binding = textBox.GetBindingExpression(TextBox.TextProperty);
                        if (binding != null)
                        {
                            e.Handled = true;
                            binding.UpdateSource();
                        }

                        textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
                }
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.SelectAll();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 3)
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    textBox.SelectAll();
                }
            }
            else
            {
                DependencyObject obj = e.OriginalSource as UIElement;
                while ((obj != null) && !(obj is TextBox))
                {
                    obj = System.Windows.Media.VisualTreeHelper.GetParent(obj);
                }

                TextBox textBox = obj as TextBox;
                if (textBox != null)
                {
                    if (!textBox.IsKeyboardFocusWithin)
                    {
                        e.Handled = true;
                        textBox.Focus();
                    }
                }
            }
        }

        private bool firstWindow = true;
        private IKStudioService kstudioService = null;
    }
}