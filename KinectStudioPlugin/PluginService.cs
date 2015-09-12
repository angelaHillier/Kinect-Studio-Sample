//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Windows;
    using System.Windows.Threading;
    using System.Xml.Linq;
    using nui = Microsoft.Xbox.Input.Nui;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using Microsoft.Xbox.Tools.Shared;
    using Microsoft.Kinect.Tools;
    using Microsoft.Win32;
    using KStudioBridge;
    using KinectStudioUtility;

    public class PluginService: IPluginService, IDisposable
    {
        public PluginService(IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            this.serviceProvider = serviceProvider;
            this.pluginMetadataStates = new List<PluginMetadataState>();

            this.readOnlyFileMetadata = new Dictionary<FileMetadataDataTemplateKey, DataTemplate>();
            this.writableFileMetadata = new Dictionary<FileMetadataDataTemplateKey, DataTemplate>();
            this.readOnlyStreamMetadata = new Dictionary<StreamMetadataDataTemplateKey, DataTemplate>();
            this.writableStreamMetadata = new Dictionary<StreamMetadataDataTemplateKey, DataTemplate>();

            SystemEvents.DisplaySettingsChanging += SystemEvents_DisplaySettingsChanging;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        ~PluginService()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "plugins"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        public void Initialize()
        {
            DebugHelper.AssertUIThread();

            if (this.serviceProvider != null)
            {
                this.notificationService = this.serviceProvider.GetService(typeof(IUserNotificationService)) as IUserNotificationService;
                this.loggingService = this.serviceProvider.GetService(typeof(ILoggingService)) as ILoggingService;
                this.sessionStateService = this.serviceProvider.GetService(typeof(ISessionStateService)) as ISessionStateService;
            }

            XElement pluginStatesElement = null;

            if (this.sessionStateService != null)
            {
                pluginStatesElement = this.sessionStateService.GetSessionState("PluginStates");

                this.sessionStateService.StateSaveRequested += this.SessionStateService_StateSaveRequested;
            }

            if (this.plugins == null)
            {
                Assembly pluginAssembly = Assembly.GetExecutingAssembly();

                HashSet<string> paths = new HashSet<string>();
                paths.Add(Path.GetDirectoryName(pluginAssembly.Location).ToUpperInvariant());
                paths.Add(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location.ToUpperInvariant()));

                List<IPlugin> plugins = new List<IPlugin>();

                foreach (string path in paths)
                {
                    plugins.AddRange(this.LoadPlugins(path, pluginAssembly));
                }

                Dictionary<Guid, XElement> pluginStateElements = new Dictionary<Guid, XElement>();
                if (pluginStatesElement != null)
                {
                    foreach (XElement element in pluginStatesElement.Elements("plugin"))
                    {
                        Guid pluginId = XmlExtensions.GetAttribute(element, "id", Guid.Empty);
                        pluginStateElements[pluginId] = element;
                    }
                }

                foreach (IPlugin plugin in plugins)
                {
                    XElement pluginElement;

                    if (pluginStateElements.TryGetValue(plugin.Id, out pluginElement))
                    {
                        XElement stateElement = pluginElement.Element("state");
                        if (stateElement != null)
                        {
                            plugin.ReadFrom(stateElement);
                        }
                    }
                }
                
                foreach (PluginMetadataState pluginMetadataState in this.pluginMetadataStates)
                {
                    XElement pluginElement;
                    if (pluginStateElements.TryGetValue(pluginMetadataState.Plugin.Id, out pluginElement))
                    {
                        pluginMetadataState.IsEnabled = XmlExtensions.GetAttribute(pluginElement, "metadataEnabled", pluginMetadataState.IsEnabled);
                        pluginMetadataState.Order = XmlExtensions.GetAttribute(pluginElement, "metadataOrder", pluginMetadataState.Order);
                    }
                }

                this.pluginMetadataStates.Sort();

                this.plugins = plugins.AsReadOnly();

                LoadMetadataDataTemplates();
            }

            this.InitializeNuiViz(EventType.Monitor, this.monitorData);
            this.InitializeNuiViz(EventType.Inspection, this.inspectionData);

            this.depthIrEngine = new DepthIrEngine();
        }

        public bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId)
        {
            DebugHelper.AssertUIThread();

            bool result = false;

            if (this.plugins != null)
            {
                result = this.plugins.OfType<IEventHandlerPlugin>().Any(ehp => ehp.IsInterestedInEventsFrom(eventType, dataTypeId, semanticId));
            }

            return result;
        }

        public void ClearEvents(EventType eventType)
        {
            DebugHelper.AssertUIThread();

            if (this.plugins != null)
            {
                foreach (IEventHandlerPlugin eventHandlerPlugin in this.plugins.OfType<IEventHandlerPlugin>())
                {
                    eventHandlerPlugin.ClearEvents(eventType);
                }
            }
        }

        public void HandleEvent(EventType eventType, KStudioEvent eventObj)
        {
            DebugHelper.AssertUIThread();

            EventTypePluginData data = null;

            switch (eventType)
            {
                case EventType.Monitor:
                    data = this.monitorData;
                    break;

                case EventType.Inspection:
                    data = this.inspectionData;
                    break;
            }

            if ((eventObj != null) && (data != null))
            {
                if ((eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.Calibration) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.CalibrationMonitor))
                {
                    if (data.registration != null)
                    {
                        data.registration.Dispose();
                        data.registration = null;
                    }

                    uint bufferSize;
                    IntPtr buffer;

                    eventObj.AccessUnderlyingEventDataBuffer(out bufferSize, out buffer);
                    data.registration = new nui.Registration(buffer, bufferSize);
                }
                else if (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.SystemInfo)
                {
                    if (data.registration != null)
                    {
                        data.registration.Dispose();
                        data.registration = null;
                    }

                    uint bufferSize;
                    IntPtr buffer;

                    eventObj.AccessUnderlyingEventDataBuffer(out bufferSize, out buffer);
                    if (this.depthIrEngine != null)
                    {
                        data.registration = this.depthIrEngine.GetCalibrationFromSysInfo(buffer, bufferSize);
                    }
                }

                if (this.plugins != null)
                {
                    foreach (IEventHandlerPlugin eventHandlerPlugin in this.plugins.OfType<IEventHandlerPlugin>())
                    {
                        eventHandlerPlugin.HandleEvent(eventType, eventObj);
                    }
                }
            }
        }

        public DataTemplate GetReadOnlyFileMetadataDataTemplate(Type valueType, string keyName)
        {
            DebugHelper.AssertUIThread();

            return GetFileMetadataDataTemplate(this.readOnlyFileMetadata, valueType, keyName);
        }

        public DataTemplate GetWritableFileMetadataDataTemplate(Type valueType, string keyName)
        {
            DebugHelper.AssertUIThread();

            return GetFileMetadataDataTemplate(this.writableFileMetadata, valueType, keyName);
        }

        public DataTemplate GetReadOnlyStreamMetadataDataTemplate(Type valueType, string keyName, Guid dataTypeId, Guid semanticId)
        {
            DebugHelper.AssertUIThread();

            return GetStreamMetadataDataTemplate(this.readOnlyStreamMetadata, valueType, keyName, dataTypeId, semanticId);
        }

        public DataTemplate GetWritableStreamMetadataDataTemplate(Type valueType, string keyName, Guid dataTypeId, Guid semanticId)
        {
            DebugHelper.AssertUIThread();

            return GetStreamMetadataDataTemplate(this.writableStreamMetadata, valueType, keyName, dataTypeId, semanticId);
        }

        public bool ShowMetadataPlugins(Window owner)
        {
            DebugHelper.AssertUIThread();

            bool result = false;

            if ((this.plugins != null) && (this.pluginMetadataStates != null))
            {
                ObservableCollection<PluginMetadataState> pluginMetadataStatesCopy = new ObservableCollection<PluginMetadataState>();

                lock (this.plugins)
                {
                    foreach (PluginMetadataState pluginMetadataState in this.pluginMetadataStates)
                    {
                        pluginMetadataStatesCopy.Add(new PluginMetadataState(pluginMetadataState));
                    }
                }

                MetadataPluginsDialog metadataDialog = new MetadataPluginsDialog()
                    {
                        DataContext = pluginMetadataStatesCopy,
                        Owner = owner,
                    };

                if (metadataDialog.ShowDialog() == true)
                {
                    lock (this.plugins)
                    {
                        this.pluginMetadataStates.Clear();

                        int order = 0;

                        foreach (PluginMetadataState pluginMetadataState in pluginMetadataStatesCopy)
                        {
                            pluginMetadataState.Order = order;
                            ++order;

                            this.pluginMetadataStates.Add(pluginMetadataState);
                        }

                        this.LoadMetadataDataTemplates();

                        result = true;
                    }
                }
            }

            return result;
        }

        public void Update2DPropertyView(EventType eventType, double x, double y, uint width, uint height)
        {
            foreach (IPlugin plugin in this.plugins)
            {
                I2DVisualPlugin visualPlugin = plugin as I2DVisualPlugin;
                if (visualPlugin != null)
                {
                    visualPlugin.UpdatePropertyView(eventType, x, y, width, height);
                }
            }
        }

        public void Clear2DPropertyView()
        {
            foreach (IPlugin plugin in this.plugins)
            {
                I2DVisualPlugin visualPlugin = plugin as I2DVisualPlugin;
                if (visualPlugin != null)
                {
                    visualPlugin.ClearPropertyView();
                }
            }
        }

        private void SessionStateService_StateSaveRequested(object sender, EventArgs e)
        {
            if (this.sessionStateService != null)
            {
                XElement pluginStatesElement = this.sessionStateService.GetSessionState("PluginStates");

                if (pluginStatesElement == null)
                {
                    pluginStatesElement = new XElement("plugins");
                    this.sessionStateService.SetSessionState("PluginStates", pluginStatesElement);
                }

                Dictionary<Guid, XElement> pluginStateElements = new Dictionary<Guid, XElement>();
                if (pluginStatesElement != null)
                {
                    foreach (XElement pluginElement in pluginStatesElement.Elements("plugin"))
                    {
                        Guid pluginId = XmlExtensions.GetAttribute(pluginElement, "id", Guid.Empty);
                        pluginStateElements[pluginId] = pluginElement;
                    }
                }

                foreach (IPlugin plugin in this.plugins)
                {
                    Guid pluginId = plugin.Id;
                    XElement pluginElement;

                    if (!pluginStateElements.TryGetValue(pluginId, out pluginElement))
                    {
                        pluginElement = new XElement("plugin");
                        pluginElement.SetAttributeValue("id", pluginId.ToString());

                        pluginStatesElement.Add(pluginElement);
                        pluginStateElements[pluginId] = pluginStatesElement;
                    }

                    XElement stateElement = pluginElement.Element("state");
                    if (stateElement == null)
                    {
                        stateElement = new XElement("state");
                        pluginElement.Add(stateElement);
                    }

                    plugin.WriteTo(stateElement);
                }

                foreach (PluginMetadataState pluginMetadataState in this.pluginMetadataStates)
                {
                    Guid pluginId = pluginMetadataState.Plugin.Id;
                    XElement pluginElement;

                    if (!pluginStateElements.TryGetValue(pluginId, out pluginElement))
                    {
                        pluginElement = new XElement("state");
                        pluginElement.SetAttributeValue("id", pluginId.ToString());

                        pluginStatesElement.Add(pluginElement);
                    }

                    pluginElement.SetAttributeValue("metadataEnabled", pluginMetadataState.IsEnabled.ToString(CultureInfo.InvariantCulture));
                    pluginElement.SetAttributeValue("metadataOrder", pluginMetadataState.Order.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "context"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Viz")]
        private void InitializeNuiViz(EventType eventType, EventTypePluginData data)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(data != null);

            if ((this.plugins != null) && (data.context == null))
            {
                bool okay = false;

                viz.Context context = new viz.Context();

                if (context.HasInitialized())
                {
                    data.imageContext = new viz.D3DImageContext();
                    data.context = context;

                    foreach (IImageVisualPlugin imagePlugin in this.plugins.OfType<IImageVisualPlugin>())
                    {
                        imagePlugin.InitializeRender(eventType, context);
                    }

                    okay = true;
                }
                else
                {
                    context.Dispose();
                }

                if (!okay)
                {
                    if (this.loggingService != null)
                    {
                        this.loggingService.LogLine(Strings.Error_LimitedGraphics);
                    }

                    if (this.notificationService != null)
                    {
                        Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                            {
                                this.notificationService.ShowMessageBox(Strings.Error_LimitedGraphics);
                            }));
                    }
                }
            }
        }

        private void UninitializeNuiViz(EventType eventType, EventTypePluginData data)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(data != null);

            PluginService.DisposeData(data, false);

            if (this.plugins != null)
            {
                foreach (IImageVisualPlugin imagePlugin in this.plugins.OfType<IImageVisualPlugin>())
                {
                    imagePlugin.UninitializeRender(eventType);
                }
            }
        }

        private DataTemplate GetFileMetadataDataTemplate(IDictionary<FileMetadataDataTemplateKey, DataTemplate> source, Type valueType, string keyName)
        {
            DebugHelper.AssertUIThread();

            DataTemplate result = null;

            if (this.plugins != null)
            {
                Debug.Assert(source != null);
                Debug.Assert(valueType != null);
                Debug.Assert(!String.IsNullOrWhiteSpace(keyName));

                FileMetadataDataTemplateKey key;

                lock (this.plugins)
                {
                    key = new FileMetadataDataTemplateKey(valueType, keyName);

                    if (!source.TryGetValue(key, out result))
                    {
                        key = new FileMetadataDataTemplateKey(valueType);

                        if (!source.TryGetValue(key, out result))
                        {
                            result = null;
                        }
                    }
                }
            }

            return result;
        }

        private DataTemplate GetStreamMetadataDataTemplate(IDictionary<StreamMetadataDataTemplateKey, DataTemplate> source, Type valueType, string keyName, Guid dataTypeId, Guid semanticId)
        {
            DebugHelper.AssertUIThread();

            DataTemplate result = null;

            if (this.plugins != null)
            {
                Debug.Assert(source != null);
                Debug.Assert(valueType != null);
                Debug.Assert(!String.IsNullOrWhiteSpace(keyName));
                Debug.Assert(dataTypeId != Guid.Empty);
                Debug.Assert(semanticId != Guid.Empty);

                StreamMetadataDataTemplateKey key;

                lock (this.plugins)
                {
                    key = new StreamMetadataDataTemplateKey(valueType, keyName, dataTypeId, semanticId);

                    if (!source.TryGetValue(key, out result))
                    {
                        key = new StreamMetadataDataTemplateKey(valueType, keyName, dataTypeId);

                        if (!source.TryGetValue(key, out result))
                        {
                            key = new StreamMetadataDataTemplateKey(valueType, keyName);

                            if (!source.TryGetValue(key, out result))
                            {
                                key = new StreamMetadataDataTemplateKey(valueType);

                                if (!source.TryGetValue(key, out result))
                                {
                                    result = null;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void LoadMetadataDataTemplates()
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(this.plugins != null);

            lock (this.plugins)
            {
                this.readOnlyFileMetadata.Clear();
                this.writableFileMetadata.Clear();
                this.readOnlyStreamMetadata.Clear();
                this.writableStreamMetadata.Clear();

                // these defaults can be overridden
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableByteMetadataValueDataTemplate", typeof(Byte));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableInt16MetadataValueDataTemplate", typeof(Int16));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableUInt16MetadataValueDataTemplate", typeof(UInt16));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableInt32MetadataValueDataTemplate", typeof(Int32));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableUInt32MetadataValueDataTemplate", typeof(UInt32));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableInt64MetadataValueDataTemplate", typeof(Int64));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableUInt64MetadataValueDataTemplate", typeof(UInt64));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableSingleMetadataValueDataTemplate", typeof(Single));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableDoubleMetadataValueDataTemplate", typeof(Double));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableCharMetadataValueDataTemplate", typeof(Char));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableBooleanMetadataValueDataTemplate", typeof(Boolean));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableGuidMetadataValueDataTemplate", typeof(Guid));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableDateTimeMetadataValueDataTemplate", typeof(DateTime));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableTimeSpanMetadataValueDataTemplate", typeof(TimeSpan));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritablePointMetadataValueDataTemplate", typeof(Point));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableSizeMetadataValueDataTemplate", typeof(Size));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableRectMetadataValueDataTemplate", typeof(Rect));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableStringMetadataValueDataTemplate", typeof(String));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableBufferMetadataValueDataTemplate", typeof(KStudioMetadataValueBuffer));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.InvalidMetadataValueDataTemplate", typeof(KStudioInvalidMetadataValue));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableByteArrayMetadataValueDataTemplate", typeof(Byte[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableInt16ArrayMetadataValueDataTemplate", typeof(Int16[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableUInt16ArrayMetadataValueDataTemplate", typeof(UInt16[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableInt32ArrayMetadataValueDataTemplate", typeof(Int32[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableUInt32ArrayMetadataValueDataTemplate", typeof(UInt32[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableInt64ArrayMetadataValueDataTemplate", typeof(Int64[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableUInt64ArrayMetadataValueDataTemplate", typeof(UInt64[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableSingleArrayMetadataValueDataTemplate", typeof(Single[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableDoubleArrayMetadataValueDataTemplate", typeof(Double[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableCharArrayMetadataValueDataTemplate", typeof(Char[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableBooleanArrayMetadataValueDataTemplate", typeof(Boolean[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableGuidArrayMetadataValueDataTemplate", typeof(Guid[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableDateTimeArrayMetadataValueDataTemplate", typeof(DateTime[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableTimeSpanArrayMetadataValueDataTemplate", typeof(TimeSpan[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritablePointArrayMetadataValueDataTemplate", typeof(Point[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableSizeArrayMetadataValueDataTemplate", typeof(Size[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableRectArrayMetadataValueDataTemplate", typeof(Rect[]));
                LoadDefaultDataTemplate(this.writableFileMetadata, this.writableStreamMetadata, "KinectStudioPlugin.WritableStringArrayMetadataValueDataTemplate", typeof(String[]));

                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyNumberMetadataValueDataTemplate", typeof(Byte));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyNumberMetadataValueDataTemplate", typeof(Int16));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyNumberMetadataValueDataTemplate", typeof(UInt16));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyNumberMetadataValueDataTemplate", typeof(Int32));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyNumberMetadataValueDataTemplate", typeof(UInt32));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyNumberMetadataValueDataTemplate", typeof(Int64));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyNumberMetadataValueDataTemplate", typeof(UInt64));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyNumberMetadataValueDataTemplate", typeof(Single));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyNumberMetadataValueDataTemplate", typeof(Double));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyStringMetadataValueDataTemplate", typeof(Char));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyBooleanMetadataValueDataTemplate", typeof(Boolean));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyStringMetadataValueDataTemplate", typeof(Guid));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyStringMetadataValueDataTemplate", typeof(DateTime));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyStringMetadataValueDataTemplate", typeof(TimeSpan));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyStringMetadataValueDataTemplate", typeof(Point));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyStringMetadataValueDataTemplate", typeof(Size));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyStringMetadataValueDataTemplate", typeof(Rect));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyStringMetadataValueDataTemplate", typeof(String));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyBufferMetadataValueDataTemplate", typeof(KStudioMetadataValueBuffer));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.InvalidMetadataValueDataTemplate", typeof(KStudioInvalidMetadataValue));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Byte[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Int16[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(UInt16[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Int32[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(UInt32[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Int64[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(UInt64[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Single[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Double[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Char[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Boolean[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Guid[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(DateTime[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(TimeSpan[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Point[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Size[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(Rect[]));
                LoadDefaultDataTemplate(this.readOnlyFileMetadata, this.readOnlyStreamMetadata, "KinectStudioPlugin.ReadOnlyArrayMetadataValueDataTemplate", typeof(String[]));

                foreach (PluginMetadataState pluginState in this.pluginMetadataStates)
                {
                    if (pluginState.IsEnabled)
                    {
                        Debug.Assert(pluginState.Plugin != null);

                        IMetadataPlugin metadataPlugin = pluginState.Plugin as IMetadataPlugin;
                        if (metadataPlugin != null)
                        {
                            LoadMetadataDataTemplates(this.readOnlyFileMetadata, metadataPlugin.FileReadOnlyDataTemplates);
                            LoadMetadataDataTemplates(this.writableFileMetadata, metadataPlugin.FileWritableDataTemplates);
                            LoadMetadataDataTemplates(this.readOnlyStreamMetadata, metadataPlugin.StreamReadOnlyDataTemplates);
                            LoadMetadataDataTemplates(this.writableStreamMetadata, metadataPlugin.StreamWritableDataTemplates);
                        }
                    }
                }
            }
        }

        private static void LoadDefaultDataTemplate(IDictionary<FileMetadataDataTemplateKey, DataTemplate> fileDestination, IDictionary<StreamMetadataDataTemplateKey, DataTemplate> streamDestination, string resourceKey, Type valueType)
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(fileDestination != null);
            Debug.Assert(streamDestination != null);
            Debug.Assert(resourceKey != null);
            Debug.Assert(valueType != null);

            DataTemplate dataTemplate = Application.Current.TryFindResource(resourceKey) as DataTemplate;

            if (dataTemplate != null)
            {
                if (fileDestination != null)
                {
                    FileMetadataDataTemplateKey key = new FileMetadataDataTemplateKey(valueType);
                    fileDestination.Add(key, dataTemplate);
                }

                if (streamDestination != null)
                {
                    StreamMetadataDataTemplateKey key = new StreamMetadataDataTemplateKey(valueType);
                    streamDestination.Add(key, dataTemplate);
                }
            }
        }

        private static void LoadMetadataDataTemplates<T>(IDictionary<T, DataTemplate> destination, IReadOnlyDictionary<T, DataTemplate> source)
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(destination != null);

            if ((destination != null) && (source != null))
            {
                foreach (KeyValuePair<T, DataTemplate> kv in source)
                {
                    if (kv.Value != null)
                    {
                        destination[kv.Key] = kv.Value;
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "plugins"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        private List<IPlugin> LoadPlugins(string path, Assembly pluginAssembly)
        {
            List<IPlugin> plugins = new List<IPlugin>();

            Type pluginType = typeof(IPlugin);
            TypeFilter pluginTypeFilter = new TypeFilter(PluginService.PluginFilter);

            Type[] serviceProviderArgTypes = new Type[]
                {
                    typeof(IServiceProvider),
                };
            Type[] emptyArgTypes = new Type[0];
            object[] serviceProviderArgs = new object[]
                {
                    this.serviceProvider,
                };
            object[] emptyArgs = new object[0];

            foreach (string file in Directory.GetFiles(path, "*KinectStudioPlugin.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    Assembly dll = Assembly.LoadFrom(file);
                    if (dll != pluginAssembly)
                    {
                        IEnumerable<Type> types = dll.ExportedTypes.Where(type => type.FindInterfaces(pluginTypeFilter, pluginType).Length > 0);

                        foreach (Type type in types)
                        {
                            IPlugin plugin = null;

                            try
                            {
                                {
                                    ConstructorInfo constructor = type.GetConstructor(serviceProviderArgTypes);

                                    if (constructor != null)
                                    {
                                        plugin = constructor.Invoke(serviceProviderArgs) as IPlugin;
                                    }
                                }

                                if (plugin == null)
                                {
                                    ConstructorInfo constructor = type.GetConstructor(emptyArgTypes);

                                    if (constructor != null)
                                    {
                                        plugin = constructor.Invoke(emptyArgs) as IPlugin;
                                    }
                                }

                                bool okay = false;

                                if (plugin != null)
                                {
                                    okay = true;

                                    if (plugin.Id == Guid.Empty)
                                    {
                                        if (this.loggingService != null)
                                        {
                                            this.loggingService.LogLine(Strings.Plugin_Error_BadPluginId, plugin.Name, plugin.GetType().FullName, file);
                                        }

                                        okay = false;
                                    }

                                    if (okay)
                                    {
                                        Debug.Assert(plugin != null);

                                        foreach (IPlugin existingPlugin in plugins)
                                        {
                                            if (plugin.Id == existingPlugin.Id)
                                            {
                                                if (this.loggingService != null)
                                                {
                                                    this.loggingService.LogLine(Strings.Plugin_Error_DuplicatePluginId, plugin.Name, plugin.GetType().FullName, file, existingPlugin.Name, existingPlugin.GetType().FullName);
                                                }

                                                okay = false;

                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!okay)
                                {
                                    IDisposable disposable = plugin as IDisposable;
                                    if (disposable != null)
                                    {
                                        disposable.Dispose();
                                    }

                                    continue;
                                }

                                plugins.Add(plugin);

                                IMetadataPlugin metadataPlugin = plugin as IMetadataPlugin;
                                if (metadataPlugin != null)
                                {
                                    PluginMetadataState metadataState = new PluginMetadataState(plugin);

                                    this.pluginMetadataStates.Add(metadataState);
                                }
                            }
                            catch (Exception)
                            {
                                // TODO_LOG
                            }
                        }
                    }
                }
                catch (BadImageFormatException)
                {
                    // ignore non-managed DLLs
                }
                catch (Exception)
                {
                    // TODO_LOG
                }
            }

            return plugins;
        }


        public IEnumerable<IPlugin> Plugins 
        { 
            get 
            {
                DebugHelper.AssertUIThread();

                return this.plugins; 
            }
        }

        public viz.Context GetContext(EventType eventType)
        { 
            DebugHelper.AssertUIThread();

            viz.Context value = null;

            switch (eventType)
            {
                case EventType.Monitor:
                    Debug.Assert(this.monitorData != null);
                    value = this.monitorData.context;
                    break;

                case EventType.Inspection:
                    Debug.Assert(this.inspectionData != null);
                    value = this.inspectionData.context;
                    break;
            }

            return value;
        } 

        public viz.D3DImageContext GetImageContext(EventType eventType)
        {
            DebugHelper.AssertUIThread();

            viz.D3DImageContext value = null;

            switch (eventType)
            {
                case EventType.Monitor:
                    Debug.Assert(this.monitorData != null);
                    value = this.monitorData.imageContext;
                    break;

                case EventType.Inspection:
                    Debug.Assert(this.inspectionData != null);
                    value = this.inspectionData.imageContext;
                    break;
            }

            return value;
        }

        public nui.Registration GetRegistration(EventType eventType)
        {
            DebugHelper.AssertUIThread();

            nui.Registration value = null;

            switch (eventType)
            {
                case EventType.Monitor:
                    Debug.Assert(this.monitorData != null);
                    value = this.monitorData.registration;
                    break;

                case EventType.Inspection:
                    Debug.Assert(this.inspectionData != null);
                    value = this.inspectionData.registration;
                    break;
            }

            return value;
        }

        public DepthIrEngine DepthIrEngine
        {
            get
            {
                return this.depthIrEngine;
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                PluginService.DisposeData(this.monitorData, true);
                PluginService.DisposeData(this.inspectionData, true);

                if (this.depthIrEngine != null)
                {
                    this.depthIrEngine.Dispose();
                    this.depthIrEngine = null;
                }
            }
        }

        private void SystemEvents_DisplaySettingsChanging(object sender, EventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.UninitializeNuiViz(EventType.Monitor, this.monitorData);
            this.UninitializeNuiViz(EventType.Inspection, this.inspectionData);
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.InitializeNuiViz(EventType.Monitor, this.monitorData);
            this.InitializeNuiViz(EventType.Inspection, this.inspectionData);
        }

        private static void DisposeData(EventTypePluginData data, bool doAll)
        {
            Debug.Assert(data != null);

            if (data.context != null)
            {
                data.context.Dispose();
                data.context = null;
            }

            if (data.imageContext != null)
            {
                data.imageContext.Dispose();
                data.imageContext = null;
            }

            if (doAll)
            {
                if (data.registration != null)
                {
                    data.registration.Dispose();
                    data.registration = null;
                }
            }
        }

        private static bool PluginFilter(Type typeObj, Object pluginType)
        {
            return (typeObj == (Type)pluginType);
        }

        private class EventTypePluginData
        {
            public viz.Context context = null;
            public viz.D3DImageContext imageContext = null;
            public nui.Registration registration = null;
        }

        private readonly IServiceProvider serviceProvider;
        private readonly IDictionary<FileMetadataDataTemplateKey, DataTemplate> readOnlyFileMetadata;
        private readonly IDictionary<FileMetadataDataTemplateKey, DataTemplate> writableFileMetadata;
        private readonly IDictionary<StreamMetadataDataTemplateKey, DataTemplate> writableStreamMetadata;
        private readonly IDictionary<StreamMetadataDataTemplateKey, DataTemplate> readOnlyStreamMetadata;
        private readonly List<PluginMetadataState> pluginMetadataStates;
        private IReadOnlyCollection<IPlugin> plugins = null;

        private IUserNotificationService notificationService = null;
        private ILoggingService loggingService = null;
        private ISessionStateService sessionStateService = null;
        private DepthIrEngine depthIrEngine = null;
        private readonly EventTypePluginData monitorData = new EventTypePluginData();
        private readonly EventTypePluginData inspectionData = new EventTypePluginData();
    }
}
