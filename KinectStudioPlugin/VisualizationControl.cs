//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Xbox.Tools.Shared;
using Microsoft.Kinect.Tools;
using viz = Microsoft.Xbox.Kinect.Viz;
using KinectStudioUtility;

namespace KinectStudioPlugin
{
    public abstract class VisualizationControl : UserControl, IDisposable
    {
        protected VisualizationControl(IServiceProvider serviceProvider, VisualizationViewSettings viewSettings, Func<IPlugin, bool> filterFunc, EventType eventType, IAvailableStreams availableStreamsGetter)
        {
            DebugHelper.AssertUIThread();

            this.DefaultStyleKey = typeof(VisualizationControl);
            this.Style = FindResource("KinectStudioPlugin." + this.GetType().Name + "Style") as Style;

            this.filterFunc = filterFunc;
            this.DataContext = this;
            this.viewSettings = viewSettings;
            this.eventType = eventType;
            this.availableStreamsGetter = availableStreamsGetter;

            if (serviceProvider != null)
            {
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }

            this.Loaded += VisualizationControl_Loaded;
            this.Unloaded += VisualizationControl_Unloaded;
        }

        ~VisualizationControl()
        {
            this.Dispose(false);
        }

        public EventType EventType
        {
            get
            {
                return this.eventType;
            }
        }

        public string Title
        {
            get
            {
                return GetValue(TitleProperty) as string;
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        public virtual void RefreshSettings()
        {
            DebugHelper.AssertUIThread();

            if (this.ViewSettings != null)
            {
                this.OnRefreshSettings(this.ViewSettings.ViewSettingsElement);
            }
        }

        public void ShowSettings()
        {
            DebugHelper.AssertUIThread();

            if (this.viewSettings != null)
            {
                HashSet<KStudioEventStreamIdentifier> availableStreamIds = null;

                if (this.availableStreamsGetter != null)
                {
                    switch (this.eventType)
                    {
                        case EventType.Monitor:
                            availableStreamIds = this.availableStreamsGetter.GetAvailableMonitorStreams();
                            break;

                        case EventType.Inspection:
                            availableStreamIds = this.availableStreamsGetter.GetAvailableComboStreams();
                            break;
                    }
                }

                ObservableCollection<PluginViewState> pluginViewStatesCopy = new ObservableCollection<PluginViewState>();

                foreach (PluginViewState pluginViewState in this.pluginViewStates)
                {
                    PluginViewState pluginViewStateCopy = new PluginViewState(pluginViewState);
                    IPluginViewSettings pluginViewSettingsCopy = pluginViewStateCopy.PluginViewSettings;

                    if (pluginViewSettingsCopy != null)
                    {
                        pluginViewStatesCopy.Add(pluginViewStateCopy);

                        pluginViewSettingsCopy.CheckRequirementsSatisfied(availableStreamIds);
                    }
                }

                string viewTitle = this.Title;

                using (RenderViewSettings settings = new RenderViewSettings(viewTitle, pluginViewStatesCopy))
                {
                    RenderViewSettingsDialog settingsDialog = new RenderViewSettingsDialog()
                        {
                            DataContext = settings,
                            Owner = Window.GetWindow(this),
                            Title = this.SettingsTitle,
                        };

                    XElement viewSettingsElement = viewSettings.ViewSettingsElement;
                    settingsDialog.Width = settingsDialog.Width = XmlExtensions.GetAttribute(viewSettingsElement, "editWidth", settingsDialog.Width);
                    settingsDialog.Height = settingsDialog.Height = XmlExtensions.GetAttribute(viewSettingsElement, "editHeight", settingsDialog.Height);

                    settingsDialog.ShowDialog();

                    if (settingsDialog.DialogResult == true)
                    {
                        int order = 0;

                        foreach (PluginViewState pluginViewState in pluginViewStatesCopy)
                        {
                            XElement pluginViewElement = new XElement("plugin");

                            if (pluginViewState.PluginViewSettings != null)
                            {
                                XElement pluginViewDataElement = new XElement("data");
                                pluginViewState.PluginViewSettings.WriteTo(pluginViewDataElement);
                                pluginViewElement.Add(pluginViewDataElement);
                            }

                            pluginViewState.Order = order;

                            pluginViewElement.SetAttributeValue("enabled", pluginViewState.IsEnabled.ToString(CultureInfo.InvariantCulture));
                            pluginViewElement.SetAttributeValue("order", pluginViewState.Order.ToString(CultureInfo.InvariantCulture));

                            viewSettings.SetPluginViewSettings(pluginViewState.Plugin.Id, pluginViewElement);
                            ++order;
                        }

                        if (viewSettingsElement != null)
                        {
                            viewSettingsElement.SetAttributeValue("editWidth", settingsDialog.ActualWidth.ToString(CultureInfo.InvariantCulture));
                            viewSettingsElement.SetAttributeValue("editHeight", settingsDialog.ActualHeight.ToString(CultureInfo.InvariantCulture));
                        }

                        this.pluginViewStates.Clear();
                        this.pluginViewStates.AddRange(pluginViewStatesCopy);

                        this.OnSettingsChanged();
                    }
                }
            }
        }

        protected virtual void OnRefreshSettings(XElement element)
        {
        }

        protected VisualizationViewSettings ViewSettings
        {
            get
            {
                return this.viewSettings;
            }
        }

        protected abstract string SettingsTitle { get; }

        protected virtual void OnLoaded() { }

        protected virtual void OnUnloaded() { }

        protected virtual void OnSettingsChanged() { }

        protected virtual void OnBindCommands(CommandBindingCollection bindings)
        {
            DebugHelper.AssertUIThread();

            if (bindings != null)
            {
                ICommand cmd = FindResource("KinectStudioPlugin.ShowSettingsCommand") as ICommand;
                if (cmd != null)
                {
                    bindings.Add(new CommandBinding(cmd,
                        (source2, e2) =>
                            {
                                DebugHelper.AssertUIThread();

                                this.ShowSettings();
                                e2.Handled = true;
                            },
                        (source2, e2) =>
                            {
                                e2.Handled = true;
                                e2.CanExecute = true;
                            }));
                }
            }
        }

        protected IPluginService PluginService
        {
            get
            {
                return this.pluginService;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        protected abstract IPluginViewSettings AddView(IPlugin plugin, out Panel hostControl);

        protected virtual void Dispose(bool disposing)
        {
        }

        internal IReadOnlyCollection<PluginViewState> PluginViewStates
        {
            get
            {
                return this.pluginViewStates;
            }
        }

        private void VisualizationControl_Loaded(object source, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.InitializePlugins();

            this.OnLoaded();
        }

        private void VisualizationControl_Unloaded(object sender, RoutedEventArgs e)
        {
            this.OnUnloaded();
        }

        private void InitializePlugins()
        {
            DebugHelper.AssertUIThread();

            if ((this.pluginService != null) && (this.viewSettings != null) && (this.pluginViewStates != null) && (this.pluginViewStates.Count == 0))
            {
                bool hasUserSet = false;
                Guid depthPluginId = new Guid(0x4fc932f6, 0x77a4, 0x4a22, 0xbe, 0x1f, 0x93, 0x42, 0x4d, 0x8e, 0xbb, 0x1a);
                Guid bodyPluginId = new Guid(0x85a371bc, 0x7bb2, 0x4534, 0x86, 0x5d, 0xb7, 0x2, 0x67, 0x54, 0xe8, 0x76);
                Guid accessoryPluginId = new Guid(0xd1ec6fb2, 0xb19d, 0x4285, 0x9b, 0x69, 0xdc, 0x92, 0x1f, 0xeb, 0xf6, 0x9f);

                Dictionary<Guid, PluginViewState> defaultEnabled = new Dictionary<Guid,PluginViewState>();
                defaultEnabled[depthPluginId] = null;
                defaultEnabled[bodyPluginId] = null;
                defaultEnabled[accessoryPluginId] = null;

                foreach (IPlugin plugin in pluginService.Plugins)
                {
                    if ((this.filterFunc == null) || this.filterFunc(plugin))
                    {
                        bool enabled = false;
                        int order = int.MaxValue;

                        XElement pluginViewSettingsElement = viewSettings.GetPluginViewSettings(plugin.Id);
                        enabled = XmlExtensions.GetAttribute(pluginViewSettingsElement, "enabled", enabled);
                        order = XmlExtensions.GetAttribute(pluginViewSettingsElement, "order", order);

                        if (enabled || (order != int.MaxValue)) // user has deliberately made changes
                        {
                            hasUserSet = true;
                        }

                        {
                            Panel hostControl;

                            IPluginViewSettings pluginViewSettings = AddView(plugin, out hostControl);
                            if (pluginViewSettings != null)
                            {
                                XElement pluginViewDataElement = null;
                                if (pluginViewSettingsElement != null)
                                {
                                    pluginViewDataElement = pluginViewSettingsElement.Element("data");
                                }
                                pluginViewSettings.ReadFrom(pluginViewDataElement);

                                PluginViewState pluginViewState = new PluginViewState(plugin, pluginViewSettings, hostControl)
                                    {
                                        IsEnabled = enabled,
                                        Order = order
                                    };

                                this.pluginViewStates.Add(pluginViewState);

                                PluginViewState temp;
                                if (defaultEnabled.TryGetValue(plugin.Id, out temp))
                                {
                                    if (temp == null)
                                    {
                                        defaultEnabled[plugin.Id] = pluginViewState;
                                    }
                                }
                            }
                        }
                    }
                }

                if (!hasUserSet)
                {
                    foreach (PluginViewState pluginViewState in defaultEnabled.Values)
                    {
                        if (pluginViewState != null)
                        {
                            pluginViewState.IsEnabled = true;
                        }
                    }
                }

                this.pluginViewStates.Sort(delegate(PluginViewState a, PluginViewState b)
                    {
                        int result = 0;
                        if (a == null)
                        {
                            if (b != null)
                            {
                                result = -1;
                            }
                        }
                        else
                        {
                            if (b == null)
                            {
                                result = 1;
                            }
                            else
                            {
                                result = a.Order.CompareTo(b.Order);
                                if (result == 0)
                                {
                                    result = a.Plugin.Id.CompareTo(b.Plugin.Id);
                                }
                            }
                        }

                        return result;
                    });
            }
        }

        private readonly IAvailableStreams availableStreamsGetter;
        private readonly EventType eventType;
        private readonly VisualizationViewSettings viewSettings;
        private readonly Func<IPlugin, bool> filterFunc;
        private readonly List<PluginViewState> pluginViewStates = new List<PluginViewState>();

        private readonly static DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(VisualizationControl));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        private readonly IPluginService pluginService = null;
    }
}
    