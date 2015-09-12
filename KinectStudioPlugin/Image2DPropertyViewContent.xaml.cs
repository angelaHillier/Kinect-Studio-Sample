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
    using System.Windows;
    using System.Windows.Controls;
    using System.Xml.Linq;
    using KinectStudioUtility;
        
    public partial class Image2DPropertyViewContent : UserControl
    {
        public Image2DPropertyViewContent(IServiceProvider serviceProvider, VisualizationViewSettings viewSettings)
        {
            DebugHelper.AssertUIThread();

            InitializeComponent();

            this.DataContext = this;
            this.viewSettings = viewSettings;

            if (serviceProvider != null)
            {
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }

            this.Loaded += Image2DPropertyViewContent_Loaded;
        }

        private void Image2DPropertyViewContent_Loaded(object source, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.Loaded -= Image2DPropertyViewContent_Loaded;

            if ((this.pluginService != null) && (this.viewSettings != null))
            {
                foreach (IPlugin plugin in pluginService.Plugins)
                {
                    XElement pluginViewSettingsElement = viewSettings.GetPluginViewSettings(plugin.Id);

                    I2DVisualPlugin visualPlugin = plugin as I2DVisualPlugin;
                    if (visualPlugin != null)
                    {
                        ContentControl hostControl = new ContentControl();

                        IPluginViewSettings pluginViewSettings = visualPlugin.Add2DPropertyView(hostControl);

                        if (pluginViewSettings != null)
                        {
                            XElement pluginViewDataElement = null;
                            if (pluginViewSettingsElement != null)
                            {
                                pluginViewDataElement = pluginViewSettingsElement.Element("data");
                            }
                            pluginViewSettings.ReadFrom(pluginViewDataElement);
                        }

                        if ((pluginViewSettings != null) || (hostControl.Content != null))
                        {
                            PluginViewState pluginViewState = new PluginViewState(plugin, pluginViewSettings, hostControl);

                            this.pluginViewStates.Add(pluginViewState);

                            if (hostControl.Content != null)
                            {
                                this.List.Children.Add(hostControl);
                            }
                        }
                    }
                }
            }
        }

        private readonly VisualizationViewSettings viewSettings;
        private readonly List<PluginViewState> pluginViewStates = new List<PluginViewState>();
        private readonly IPluginService pluginService = null;
    }
}
