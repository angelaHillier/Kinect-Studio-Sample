//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ToolsUIApplication : Application, INotifyPropertyChanged
    {
        public const int WindowStateVersion = 4;

        public static ToolsUIApplication Instance { get; private set; }

        string sessionStateFileName;
        string windowStateFileName;
        SessionStateService sessionStateService;
        IDictionary<string, IViewCreationCommand> viewCreators;
        ToolsUIWindow mainWindow;
        List<ToolsUIWindow> allWindows;
        List<DocumentCategory> documentCategories;
        List<LayoutDefinition> defaultLayoutDefinitions;
        bool viewBindingUpdateRequested;
        bool isInLayoutEditMode;

        public ObservableCollection<LayoutDefinition> LayoutDefinitions { get; private set; }
        public ObservableCollection<FileTabDefinition> FileTabDefinitions { get; set; }
        public ObservableCollection<QATButtonDefinition> QATButtonDefinitions { get; set; }

        public string StateStorageDirectory { get; private set; }
        public RootServiceProvider RootServiceProvider { get; private set; }
        public ExtensionManager ExtensionManager { get; private set; }
        public ISessionStateService SessionStateService { get { return this.sessionStateService; } }
        public RecentDocumentService RecentDocumentService { get; private set; }
        public CollectionViewSource RecentDocumentsSource { get; private set; }
        public CollectionViewSource FoldersSource { get; private set; }
        public bool ShuttingDown { get; set; }
        public ToolsUIWindow MainToolsUIWindow { get { return this.mainWindow; } }
        public IEnumerable<ToolsUIWindow> ToolsUIWindows { get { return this.allWindows; } }
        public IEnumerable<DocumentCategory> DocumentCategories { get { return this.documentCategories; } }
        public int WindowCount { get { return this.allWindows.Count; } }

        public bool IsInLayoutEditMode
        {
            get
            {
                return this.isInLayoutEditMode;
            }
            set
            {
                if (value != this.isInLayoutEditMode)
                {
                    this.isInLayoutEditMode = value;
                    Notify("IsInLayoutEditMode");
                }
            }
        }

        // NOTE:  Making these abstract breaks instantiation (even for a code-behind derivation) in XAML.
        public virtual string AppName { get { throw new InvalidOperationException("Must override AppName property!"); } }
        public virtual string AppVersion { get { throw new InvalidOperationException("Must override AppVersion property!"); } }
        public virtual string AppTitle { get { return this.AppName; } }
        public virtual string DefaultWindowStateStreamName { get { return null; } }
        public virtual string DefaultThemesStreamName { get { return null; } }
        public virtual Assembly ResourceStreamAssembly { get { return this.GetType().Assembly; } }
        public virtual IEnumerable<string> ExtensionAssemblies { get { return Enumerable.Empty<string>(); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public ToolsUIApplication()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Can't create more than one ToolsUIApplication in a single process!");
            }

            Instance = this;

            this.LayoutDefinitions = new ObservableCollection<LayoutDefinition>();
            this.FileTabDefinitions = new ObservableCollection<FileTabDefinition>();
            this.QATButtonDefinitions = new ObservableCollection<QATButtonDefinition>();
            this.RecentDocumentsSource = new CollectionViewSource();
            this.FoldersSource = new CollectionViewSource();
            this.allWindows = new List<ToolsUIWindow>();

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            // WPF has a peculiar bug involving monitor resolution changes and windows with AllowsTransparency = true.
            // Under certain circumstances, after such a change, ToolsUIWindows can become visually unresponsive.  It
            // seems that any window resize event will get things back to normal, so we programmatically do that any
            // time the display settings change.
            foreach (var window in this.allWindows)
            {
                window.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // To keep the window where it is, we do different things based on maximized or not...
                    if (window.WindowState == WindowState.Maximized)
                    {
                        window.WindowState = WindowState.Normal;
                        window.WindowState = WindowState.Maximized;
                    }
                    else if (window.WindowState == WindowState.Normal)
                    {
                        window.Width = window.Width + 1;
                        window.Width = window.Width - 1;
                    }
                }));
            }
        }

        protected virtual ToolsUIWindow CreateToolsUIWindow()
        {
            throw new InvalidOperationException("Must override CreateToolsUIWindow!");
        }

        public ToolsUIWindow CreateWindow()
        {
            var window = CreateToolsUIWindow();

            if (this.mainWindow == null)
            {
                this.mainWindow = window;
            }

            this.allWindows.Add(window);
            window.Closed += OnWindowClosed;
            Notify("WindowCount");
            return window;
        }

        void OnWindowClosed(object sender, EventArgs e)
        {
            var window = sender as ToolsUIWindow;

            if (window == null)
            {
                return;
            }

            this.allWindows.Remove(window);

            if (this.allWindows.Count == 0)
            {
                // That was the last window. If we're not shutting down by now, do it.
                if (!this.ShuttingDown)
                {
                    this.ShuttingDown = true;
                    DoShutdown();
                }
            }

            if (window == this.mainWindow)
            {
                this.mainWindow = this.allWindows.OrderBy(w => -w.ActivationIndex).FirstOrDefault();
            }

            if (!this.ShuttingDown && (this.mainWindow != null))
            {
                RequestViewBindingUpdate();
                Notify("WindowCount");
            }
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            e.Cancel = !QueryShutdown();
        }

        protected virtual bool QueryShutdown()
        {
            return true;
        }

        protected virtual void OnShutdown()
        {
        }

        public void RequestViewBindingUpdate()
        {
            if (!this.viewBindingUpdateRequested)
            {
                this.viewBindingUpdateRequested = true;
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.viewBindingUpdateRequested = false;
                    foreach (var window in this.allWindows)
                    {
                        var layout = window.LayoutTabControl.SelectedContent as LayoutControl;

                        if (layout != null)
                        {
                            layout.LayoutInstance.ProcessViewBindingUpdateRequest();
                        }
                    }

                }), DispatcherPriority.Background);
            }
        }

        protected sealed override void OnStartup(StartupEventArgs e)
        {
            Initialize();

            CreateWindow();
            Debug.Assert(this.mainWindow != null, "Must have a main window at this point...");
            this.mainWindow.Show();
            base.OnStartup(e);
        }

        void Initialize()
        {
            ComputeStateStorageDirectory(this.AppName, this.AppVersion);

            this.ExtensionManager = new ExtensionManager(this.StateStorageDirectory);
            this.ExtensionManager.LoadState(this.ExtensionAssemblies);

            this.RootServiceProvider = new RootServiceProvider(this.ExtensionManager);
            this.RootServiceProvider.AddService(typeof(IUserNotificationService), new SimpleNotificationService(this.AppTitle));
            this.RootServiceProvider.AddService(typeof(ToolsUIApplication), this);
            this.RootServiceProvider.AddService(this.GetType(), this);
            this.RootServiceProvider.AddService(typeof(Dispatcher), Dispatcher.CurrentDispatcher);

            this.sessionStateService = new SessionStateService();
            this.RootServiceProvider.AddService(typeof(ISessionStateService), this.sessionStateService);

            this.windowStateFileName = Path.Combine(this.StateStorageDirectory, "WindowState.xml");
            this.sessionStateFileName = Path.Combine(this.StateStorageDirectory, "SessionState.xml");
            if (File.Exists(this.sessionStateFileName))
            {
                try
                {
                    XElement sessionState = XElement.Load(this.sessionStateFileName);
                    this.sessionStateService.SetFullSessionState(sessionState);
                }
                catch (Exception)
                {
                }
            }

            this.sessionStateService.StateSaveRequested += OnSessionStateSaveRequested;
            InitializeTheme();

            this.viewCreators = this.ExtensionManager.BuildViewCreatorDictionary();

            this.RecentDocumentService = this.RootServiceProvider.GetService(typeof(RecentDocumentService)) as RecentDocumentService;
            if (this.RecentDocumentService != null)
            {
                ((INotifyCollectionChanged)this.RecentDocumentService.DocumentIdentities).CollectionChanged += OnRecentDocumentIdentitiesCollectionChanged;
                OnRecentDocumentIdentitiesCollectionChanged(null, null);
            }

            this.FoldersSource.GroupDescriptions.Add(new PropertyGroupDescription { PropertyName = "Category" });

            if (this.RecentDocumentService != null)
            {
                NotifyCollectionChangedEventHandler handler = (s, e) =>
                {
                    this.FoldersSource.Source = new CategorizedFolder[] 
                    { 
                        new CategorizedFolder 
                        { 
                            Category = "Current Folder", 
                            DirectoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory()), 
                            ShortcutKey = "C" 
                        } 
                    }.Concat(this.RecentDocumentService.RecentFolders.Select((d, i) => new CategorizedFolder
                    {
                        Category = "Recent Folders",
                        DirectoryInfo = d,
                        ShortcutKey = string.Format("D{0}", i + 1)
                    }));
                };

                ((INotifyCollectionChanged)this.RecentDocumentService.RecentFolders).CollectionChanged += handler;
                handler(null, null);
            }

            this.documentCategories = new List<DocumentCategory>();
            foreach (var factoryName in this.ExtensionManager.DocumentFactoryNames)
            {
                var factory = this.ExtensionManager.LookupDocumentFactory(factoryName);

                if (factory != null)
                {
                    var category = new DocumentCategory { DisplayName = factory.DocumentKind, DocumentFactoryName = factoryName };
                    BindingOperations.SetBinding(category, DocumentCategory.ColorProperty, Theme.CreateBinding(factory.ColorThemePropertyName));
                    this.documentCategories.Add(category);
                }
            }

            this.documentCategories.Add(new DocumentCategory { Color = Colors.Transparent, DisplayName = "Any", DocumentFactoryName = null });

            OnInitialized();
        }

        void InitializeTheme()
        {
            var themesElement = this.sessionStateService.GetSessionState("Themes");
            bool activeThemeSet = false;

            InitializeThemeCreator();

            if (themesElement == null)
            {
                // No theme data stored in session state yet. Go get the default (if there is one)
                themesElement = ReadDefaultThemesStream();
            }

            if (themesElement != null)
            {
                var activeTheme = themesElement.Attribute("Active");

                foreach (var themeElement in themesElement.Elements("Theme"))
                {
                    var theme = ThemePersistence.LoadTheme(themeElement);

                    Theme.Instance.Themes.Add(theme);
                    if (activeTheme != null && activeTheme.Value == theme.ThemeName)
                    {
                        Theme.Instance.Theme = theme;
                        activeThemeSet = true;
                    }
                }
            }

            if (!activeThemeSet)
            {
                if (Theme.Instance.Themes.Count == 0)
                {
                    // Make sure at least one theme exists...
                    var defaultTheme = Theme.Instance.ThemeCreator();

                    defaultTheme.ThemeName = "Default Theme";
                    Theme.Instance.Themes.Add(defaultTheme);
                }

                // If no active one specified, pick the first one arbitrarily.
                Theme.Instance.Theme = Theme.Instance.Themes[0];
            }
        }

        XElement ReadDefaultThemesStream()
        {
            if (this.DefaultThemesStreamName == null)
            {
                return null;
            }

            try
            {
                using (var stream = this.ResourceStreamAssembly.GetManifestResourceStream(this.DefaultThemesStreamName))
                {
                    return XElement.Load(stream);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected virtual void InitializeThemeCreator()
        {
            Theme.Instance.ThemeCreator = () => new BaseTheme();
        }

        void OnSessionStateSaveRequested(object sender, EventArgs e)
        {
            this.sessionStateService.SetSessionState("Themes", new XElement("Themes",
                new XAttribute("Active", Theme.Instance.Theme.ThemeName),
                Theme.Instance.Themes.Select(t => ThemePersistence.SaveTheme(t))));
        }

        void OnRecentDocumentIdentitiesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.RecentDocumentsSource.Source = this.RecentDocumentService.DocumentIdentities.Select((d, i) => new Tuple<int, DocumentIdentity>(i + 1, d));
        }

        protected virtual void OnInitialized()
        {
        }

        void ComputeStateStorageDirectory(string appName, string version)
        {
            string installLocation = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);

            // If using a custom build of Kinect Studio, we expect all state files to be present in the install path
            // and that the user has read/write access to the path
            if (File.Exists(Path.Combine(installLocation, "KinectStudioWindowsState", "SessionState.xml")))
            {
                this.StateStorageDirectory = Path.Combine(installLocation, "KinectStudioWindowsState");
            }
            else // Used by SDK verson of K4W
            {
                string storageBaseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", appName, version);
                this.StateStorageDirectory = storageBaseDirectory;

                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                string installDirFile;

                if (installLocation.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase))
                {
                    // If we're running from *anywhere* under ProgramFilesX86, assume it's the SDK install location and use the
                    // default settings directory under %localappdata%.
                    this.StateStorageDirectory = storageBaseDirectory;
                }
                else
                {
                    // We're not running under program files and no files were previously found in the install path.  In that case,
                    // we may have write access to the "install" directory so try that first.  If it works, it's the
                    // most convenient place for our config files.
                    try
                    {
                        var storageDir = Path.Combine(installLocation, appName + "State");
                        var testFile = Path.Combine(storageDir, "writeaccesstestfile.dat");

                        if (!Directory.Exists(storageDir))
                        {
                            Directory.CreateDirectory(storageDir);
                        }

                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                        this.StateStorageDirectory = storageDir;
                    }
                    catch (Exception)
                    {
                    }

                    if (this.StateStorageDirectory == null)
                    {
                        // That didn't work, so we'll need to use a location under %localappdata%.  Make one based on the install location.
                        string mungedInstallLocation = installLocation.Replace(':', '_').Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
                        int maxInstallLocationLen = 200 - storageBaseDirectory.Length;      // Rationale behind 200:  MAX_PATH (260-ish) minus space for names like "WindowState.xml", etc.

                        if (mungedInstallLocation.Length > maxInstallLocationLen)
                        {
                            // Shorten the path, using the first and last "edges" of the munged location string
                            mungedInstallLocation = mungedInstallLocation.Substring(0, maxInstallLocationLen / 2) + mungedInstallLocation.Substring(mungedInstallLocation.Length - (maxInstallLocationLen / 2));
                        }

                        this.StateStorageDirectory = Path.Combine(storageBaseDirectory, mungedInstallLocation);
                    }
                }

                if (!Directory.Exists(this.StateStorageDirectory))
                {
                    Directory.CreateDirectory(this.StateStorageDirectory);
                }

                installDirFile = Path.Combine(this.StateStorageDirectory, "InstallDir.txt");

                if (!File.Exists(installDirFile))
                {
                    File.WriteAllText(installDirFile, installLocation);
                }
                else
                {
                    var location = File.ReadAllText(installDirFile);

                    if (!StringComparer.OrdinalIgnoreCase.Equals(location, installLocation))
                    {
                        try
                        {
                            foreach (var file in Directory.GetFiles(this.StateStorageDirectory))
                            {
                                File.Delete(file);
                            }

                            File.WriteAllText(installDirFile, installLocation);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        public void RevertToDefaultWindowState()
        {
            if (File.Exists(this.windowStateFileName))
            {
                try
                {
                    File.Delete(this.windowStateFileName);
                }
                catch (Exception)
                {
                }
            }

            foreach (var auxWindow in this.ToolsUIWindows.ToArray())
            {
                if (auxWindow != this.mainWindow)
                {
                    auxWindow.Close();
                }
            }

            this.LoadWindowState();
        }

        internal void LoadWindowState(ToolsUIWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            try
            {
                var defaultStateText = GetDefaultWindowStateResourceText();
                var stateDoc = XElement.Parse(defaultStateText);
                var layoutDefinitionsElement = stateDoc.Element("LayoutDefinitions");

                this.defaultLayoutDefinitions = layoutDefinitionsElement.Elements("LayoutDefinition")
                    .Select(e => LayoutDefinition.LoadFromState(e, this.viewCreators))
                    .ToList();
            }
            catch (Exception)
            {
                this.defaultLayoutDefinitions = new List<LayoutDefinition>();
            }

            LoadWindowState();
        }

        void LoadWindowState()
        {
            bool usingDefault = false;

            try
            {
                if (!File.Exists(this.windowStateFileName))
                {
                    usingDefault = true;
                    CreateDefaultWindowState();
                }

                TryLoadWindowState();
            }
            catch (Exception ex)
            {
                bool versionMismatch = ex is VersionMismatchException;

                // Don't let missing/corrupt window state data tip us over.  Try the default if we haven't already.
                if (!usingDefault)
                {
                    ex = null;

                    try
                    {
                        CreateDefaultWindowState();
                        TryLoadWindowState();
                    }
                    catch (Exception ex2)
                    {
                        // This will leave our window blank.  Bad, but the file tab will still work.  When we have a tools/options/reset window state option,
                        // that will be the solution (or at least the way we can report the problem in a natural way).  This would be a weird
                        // situation anyway...
                        ex = ex2;
                    }
                }

                if (ex != null || versionMismatch)
                {
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        var notificationService = this.RootServiceProvider.GetService(typeof(IUserNotificationService)) as IUserNotificationService;

                        if (ex != null)
                        {
                            var loggingService = this.RootServiceProvider.GetService(typeof(ILoggingService)) as ILoggingService;

                            if (loggingService != null)
                            {
                                loggingService.LogException(ex);
                            }

                            if (notificationService != null)
                            {
                                notificationService.ShowError(StringResources.ErrorLoadingWindowState, HResult.FromException(ex));
                            }
                        }
                        else
                        {
                            if (notificationService != null)
                            {
                                notificationService.ShowMessageBox(StringResources.WindowStateVersionMismatch);
                            }
                        }

                    }), DispatcherPriority.Background);
                }
            }
        }

        string GetDefaultWindowStateResourceText()
        {
            try
            {
                var stream = this.ResourceStreamAssembly.GetManifestResourceStream(this.DefaultWindowStateStreamName);

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        void CreateDefaultWindowState()
        {
            try
            {
                string text = GetDefaultWindowStateResourceText();

                if (text != null)
                {
                    File.WriteAllText(this.windowStateFileName, text);
                    return;
                }
            }
            catch (Exception)
            {
            }

            File.Delete(this.windowStateFileName);
        }

        public static bool TryLoadWindowRect(XElement windowState, out Rect windowRect, out bool isMaximized)
        {
            // The embedded (initial) window state data has no attributes, indicating we should use default window position.
            if (!windowState.HasAttributes)
            {
                windowRect = new Rect();
                isMaximized = false;
                return false;
            }

            // If the attributes are there, we assume they're correct (exceptions will be caught and we'll revert to embedded)
            windowRect = new Rect(
                double.Parse(windowState.Attribute("Left").Value, CultureInfo.InvariantCulture),
                double.Parse(windowState.Attribute("Top").Value, CultureInfo.InvariantCulture),
                double.Parse(windowState.Attribute("Width").Value, CultureInfo.InvariantCulture),
                double.Parse(windowState.Attribute("Height").Value, CultureInfo.InvariantCulture));
            isMaximized = bool.Parse(windowState.Attribute("IsMaximized").Value);

            // See if the center of this rect is in a monitor.  If not, fail.
            POINT p = new POINT() { X = (int)(windowRect.Left + (windowRect.Width / 2)), Y = (int)(windowRect.Top + (windowRect.Height / 2)) };
            IntPtr monitor = MonitorFromPoint(p, MonitorOptions.MONITOR_DEFAULTTONULL);

            if (monitor == IntPtr.Zero)
            {
                return false;
            }

            return (windowRect.Width > 0) && (windowRect.Height > 0);
        }

        void TryLoadWindowState()
        {
            if (!File.Exists(this.windowStateFileName))
            {
                // Load nothing.  
                return;
            }

            XElement windowStateRoot = XElement.Load(this.windowStateFileName);
            XAttribute versionAttr = windowStateRoot.Attribute("Version");
            XElement mainWindowState = windowStateRoot.Element("MainWindow");
            XElement layoutState = windowStateRoot.Element("LayoutDefinitions");
            int version;

            if (versionAttr == null || !int.TryParse(versionAttr.Value, out version) || version != WindowStateVersion)
            {
                throw new VersionMismatchException();
            }

            var bumpedDocumentFactories = new HashSet<string>();
            var defaultLayoutVersions = windowStateRoot.Element("DefaultLayoutVersions");

            if (defaultLayoutVersions != null)
            {
                foreach (var layoutVersion in defaultLayoutVersions.Elements("DefaultLayoutVersion"))
                {
                    var factory = this.ExtensionManager.LookupDocumentFactory(layoutVersion.Attribute("DocumentFactoryName").Value);

                    if (int.Parse(layoutVersion.Attribute("Version").Value) < factory.DefaultLayoutVersion)
                    {
                        bumpedDocumentFactories.Add(factory.Name);
                    }
                }
            }

            // Don't call clear here, instead remove one at a time.  Reason:  Calling Clear()
            // sends a "Reset" event, and the layout handling code in ToolsUIWindow gets mad at that.
            // Usually doesn't matter (at startup), but this also gets called as part of "Revert
            // to Default Window State".
            while (this.LayoutDefinitions.Count > 0)
            {
                this.LayoutDefinitions.RemoveAt(0);
            }

            var usedIds = new HashSet<Guid>();
            var loadedLayoutDefs = new Dictionary<string, List<LayoutDefinition>>();

            foreach (var layoutDefElement in layoutState.Elements("LayoutDefinition"))
            {
                var layoutDef = LayoutDefinition.LoadFromState(layoutDefElement, this.viewCreators);

                if (layoutDef.Id.Equals(Guid.Empty))
                {
                    // Could be from a state format before layouts had ID's.  Match the name and document affinity to the
                    // default set, and if found, assume its ID.  Failing that, give it a fresh one.
                    var defaultLayoutDef = this.defaultLayoutDefinitions.FirstOrDefault(ld => ld.Header == layoutDef.Header && ld.DocumentFactoryName == layoutDef.DocumentFactoryName);

                    if (defaultLayoutDef != null && !usedIds.Contains(defaultLayoutDef.Id))
                    {
                        layoutDef.Id = defaultLayoutDef.Id;
                    }
                    else
                    {
                        layoutDef.Id = Guid.NewGuid();
                    }
                }

                // We only do this to guard against users having same-named layouts (affinitized with the same document factory).  We
                // trust that Guid.NewGuid actually creates unique values...
                usedIds.Add(layoutDef.Id);

                if (layoutDef.DocumentFactoryName == null)
                {
                    // This is a "global" (any-document) layout, so we can add it now.
                    this.LayoutDefinitions.Add(layoutDef);
                }
                else
                {
                    List<LayoutDefinition> list;

                    // Stage this for validation against layout version
                    if (!loadedLayoutDefs.TryGetValue(layoutDef.DocumentFactoryName, out list))
                    {
                        list = new List<LayoutDefinition>();
                        loadedLayoutDefs.Add(layoutDef.DocumentFactoryName, list);
                    }

                    list.Add(layoutDef);
                }
            }

            foreach (var kvp in loadedLayoutDefs)
            {
                if (bumpedDocumentFactories.Contains(kvp.Key))
                {
                    var oldSet = kvp.Value.ToDictionary(ld => ld.Id);

                    // These need to be updated/upgraded. We do that giving preference to the defaults by loading them first,
                    // but keep their names in case the user has changed them.  Then add the remaining loaded layouts.
                    foreach (var layout in this.defaultLayoutDefinitions.Where(ld => ld.DocumentFactoryName == kvp.Key))
                    {
                        LayoutDefinition existingLayout;

                        if (oldSet.TryGetValue(layout.Id, out existingLayout))
                        {
                            layout.Header = existingLayout.Header;
                            oldSet.Remove(layout.Id);
                        }

                        // Mark this layout as having been reverted to default.  This prevents the per-instance state data
                        // from being loaded, which would likely fail due to structure mismatch with the definition.
                        layout.RevertedToDefault = true;
                        this.LayoutDefinitions.Add(layout);
                    }

                    // Get the rest -- don't just iterate over the dictionary, use the list instead to 
                    // preserve order (as much as possible).  Note that these are not reverted to default, as
                    // they do not have one.
                    foreach (var remainingLayout in kvp.Value.Where(ld => oldSet.ContainsKey(ld.Id)))
                    {
                        this.LayoutDefinitions.Add(remainingLayout);
                    }
                }
                else
                {
                    // These are good to go
                    foreach (var layout in kvp.Value)
                    {
                        this.LayoutDefinitions.Add(layout);
                    }
                }
            }

            if (mainWindowState != null)
            {
                this.mainWindow.LoadWindowState(mainWindowState);
            }

            var windowElements = windowStateRoot.Elements("Window");

            if (windowElements.Any())
            {
                if (this.mainWindow.IsLoaded)
                {
                    RecreateAuxiliaryWindows(windowElements);
                }
                else
                {
                    RoutedEventHandler handler = null;

                    handler = (s, e) =>
                    {
                        RecreateAuxiliaryWindows(windowElements);
                        this.mainWindow.Loaded -= handler;
                        this.mainWindow.Activate();
                    };
                    this.mainWindow.Loaded += handler;
                }
            }
        }

        void RecreateAuxiliaryWindows(IEnumerable<XElement> windowElements)
        {
            foreach (var auxWindowState in windowElements)
            {
                var auxWindow = this.CreateWindow();
                auxWindow.LoadWindowState(auxWindowState);
                auxWindow.Show();
            }
        }

        bool SaveSessionState()
        {
            // This broadcasts a "hey, time to make sure your session state is pushed into the state service" event
            XElement root = this.sessionStateService.GetFullSessionState();

            if (root != null)
            {
                try
                {
                    root.Save(this.sessionStateFileName);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        bool SaveGlobalWindowState()
        {
            var savedWindow = this.mainWindow;

            if (savedWindow == null)
            {
                return false;
            }

            var windowRect = savedWindow.NormalWindowRect;
            var windowStateRoot = new XElement("WindowState",
                new XAttribute("Version", WindowStateVersion),
                new XElement("DefaultLayoutVersions",
                    this.ExtensionManager.DocumentFactoryNames.Select(n =>
                        new XElement("DefaultLayoutVersion",
                            new XAttribute("DocumentFactoryName", n),
                            new XAttribute("Version", this.ExtensionManager.LookupDocumentFactory(n).DefaultLayoutVersion)))),
                new XElement("LayoutDefinitions", this.LayoutDefinitions.Select(p => p.SaveState())),
                savedWindow.SaveWindowState("MainWindow"));

            windowStateRoot.Add(this.allWindows.Where(w => w != savedWindow).Select(w => w.SaveWindowState("Window")));

            try
            {
                windowStateRoot.Save(this.windowStateFileName);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public void SaveState()
        {
            this.SaveSessionState();
            this.SaveGlobalWindowState();
            this.ExtensionManager.SaveState();
        }

        void DoShutdown()
        {
            SaveState();
            OnShutdown();
            Shutdown();
        }

        public bool RequestShutdown()
        {
            if (this.ShuttingDown)
            {
                Debug.Fail("Re-entrancy in RequestShutdown.");
                return false;
            }

            this.ShuttingDown = true;

            if (QueryShutdown())
            {
                DoShutdown();
                return true;
            }
            else
            {
                this.ShuttingDown = false;
                return false;
            }
        }

        void Notify(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        class VersionMismatchException : Exception
        {
        }

        enum MonitorOptions : uint
        {
            MONITOR_DEFAULTTONULL = 0x00000000,
            MONITOR_DEFAULTTOPRIMARY = 0x00000001,
            MONITOR_DEFAULTTONEAREST = 0x00000002
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr MonitorFromPoint(POINT pt, MonitorOptions dwFlags);
    }

    public class CategorizedFolder
    {
        public DirectoryInfo DirectoryInfo { get; set; }
        public string Category { get; set; }
        public string ShortcutKey { get; set; }
    }

    public class DocumentCategory : DependencyObject
    {
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(Color), typeof(DocumentCategory));

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public string DisplayName { get; set; }
        public string DocumentFactoryName { get; set; }
    }
}
