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
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows.Threading;
    using KinectStudioUtility;

    internal class RenderViewSettings : IDisposable
    {
        public RenderViewSettings(string title, IEnumerable<PluginViewState> pluginViewStates)
        {
            DebugHelper.AssertUIThread();

            this.title = title;
            this.pluginViewStates = pluginViewStates;

            if (this.pluginViewStates != null)
            {
                foreach (PluginViewState pluginViewState in this.pluginViewStates)
                {
                    IPluginViewSettings pluginViewSettings = pluginViewState.PluginViewSettings;
                    if (pluginViewSettings != null)
                    {
                        pluginViewSettings.PropertyChanged += PluginViewSettings_PropertyChanged;
                        pluginViewState.PropertyChanged += PluginViewSettings_PropertyChanged;
                    }
                }
            }
        }

        ~RenderViewSettings()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Title { get { return this.title; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IEnumerable<PluginViewState> PluginViewStates { get { return this.pluginViewStates; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void PluginViewSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            IPluginViewSettings pluginViewSettings = null;
            PluginViewState pluginViewStateChanging = sender as PluginViewState;

            if (pluginViewStateChanging != null)
            {
                pluginViewSettings = pluginViewStateChanging.PluginViewSettings;
            }
            else
            {
                pluginViewSettings = sender as IPluginViewSettings;
            }

            Debug.Assert(pluginViewSettings != null);

            IPlugin3DViewSettings plugin3DViewSettings = pluginViewSettings as IPlugin3DViewSettings;

            bool isSupplyingSurface = false;
            bool isSupplyingTexture = false;
            bool isRenderingOpaque = false;

            switch (e.PropertyName)
            {
                case "IsEnabled":
                    {
                        Debug.Assert(pluginViewStateChanging != null);
                        if (pluginViewStateChanging.IsEnabled)
                        {
                            if (plugin3DViewSettings != null)
                            {
                                isSupplyingSurface = plugin3DViewSettings.IsSupplyingSurface;
                                isSupplyingTexture = plugin3DViewSettings.IsSupplyingTexture;
                            }
                            isRenderingOpaque = pluginViewSettings.IsRendingOpaque;
                        }
                    }
                    break;

                case "IsSupplyingSurface":
                    if (plugin3DViewSettings != null)
                    {
                        isSupplyingSurface = plugin3DViewSettings.IsSupplyingSurface;
                    }
                    break;

                case "IsSupplyingTexture":
                    if (plugin3DViewSettings != null)
                    {
                        isSupplyingTexture = plugin3DViewSettings.IsSupplyingTexture;
                    }
                    break;

                case "IsRenderingOpaque":
                    isRenderingOpaque = pluginViewSettings.IsRendingOpaque;
                    break;
            }

            if (isSupplyingSurface || isSupplyingTexture || isRenderingOpaque)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    if (isSupplyingSurface)
                    {
                        // tell any other plugin that thought it was supplying the texture that it might not be
                        foreach (PluginViewState pluginViewState in this.pluginViewStates)
                        {
                            IPlugin3DViewSettings other3DViewSettings = pluginViewState.PluginViewSettings as IPlugin3DViewSettings;
                            if ((other3DViewSettings != null) && pluginViewState.IsEnabled && (other3DViewSettings != plugin3DViewSettings))
                            {
                                pluginViewState.IsEnabled = other3DViewSettings.OtherIsSupplyingSurface();
                            }
                        }
                    }

                    if (isSupplyingTexture)
                    {
                        // tell any other plugin that thought it was supplying the texture that it might not be
                        foreach (PluginViewState pluginViewState in this.pluginViewStates)
                        {
                            IPlugin3DViewSettings other3DViewSettings = pluginViewState.PluginViewSettings as IPlugin3DViewSettings;
                            if ((other3DViewSettings != null) && pluginViewState.IsEnabled && (other3DViewSettings != plugin3DViewSettings))
                            {
                                pluginViewState.IsEnabled = other3DViewSettings.OtherIsSupplyingTexture();
                            }
                        }
                    }
                    else
                        if (isRenderingOpaque)
                        {
                            // tell any other plugin that thought it was rendering opaque that it might not be
                            foreach (PluginViewState pluginViewState in this.pluginViewStates)
                            {
                                IPluginViewSettings otherViewSettings = pluginViewState.PluginViewSettings;
                                if ((otherViewSettings != null) && pluginViewState.IsEnabled && (otherViewSettings != pluginViewSettings))
                                {
                                    pluginViewState.IsEnabled = otherViewSettings.OtherIsRenderingOpaque();
                                }
                            }
                        }
                }));
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                DebugHelper.AssertUIThread();

                if (this.pluginViewStates != null)
                {
                    foreach (PluginViewState pluginViewState in this.pluginViewStates)
                    {
                        INotifyPropertyChanged propChanged = pluginViewState.PluginViewSettings as INotifyPropertyChanged;
                        if (propChanged != null)
                        {
                            propChanged.PropertyChanged -= PluginViewSettings_PropertyChanged;
                        }
                    }
                }
            }
        }

        private readonly string title;
        private readonly IEnumerable<PluginViewState> pluginViewStates;
    }
}
