//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    internal class PluginViewState : KStudioUserState, IComparable<PluginViewState>
    {
        public PluginViewState(IPlugin plugin, IPluginViewSettings pluginViewSettings, FrameworkElement hostControl)
        {
            Debug.Assert(plugin != null);

            this.plugin = plugin;
            this.pluginViewSettings = pluginViewSettings;
            this.hostControl = hostControl;
        }

        public PluginViewState(PluginViewState pluginViewState)
        {
            Debug.Assert(pluginViewState != null);

            this.plugin = pluginViewState.plugin;
            this.enabled = pluginViewState.enabled;
            this.order = pluginViewState.order;
            this.hostControl = pluginViewState.hostControl;

            if (pluginViewState.pluginViewSettings == null)
            {
                this.pluginViewSettings = null;
            }
            else
            {
                IPluginEditableViewSettings editableViewSettings = pluginViewState.pluginViewSettings as IPluginEditableViewSettings;
                if (editableViewSettings != null)
                {
                    this.pluginViewSettings = editableViewSettings.CloneForEdit();
                }
            }
        }

        public IPlugin Plugin
        {
            get
            {
                return this.plugin;
            }
        }

        public IPluginViewSettings PluginViewSettings
        {
            get
            {
                return this.pluginViewSettings;
            }
        }

        public FrameworkElement HostControl
        {
            get
            {
                return this.hostControl;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return this.enabled;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.enabled)
                {
                    this.enabled = value;
                    RaisePropertyChanged("IsEnabled");
                }
            }
        }

        public int Order
        {
            get
            {
                return this.order;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.order)
                {
                    this.order = value;
                    RaisePropertyChanged("Order");
                }
            }
        }

        public int CompareTo(PluginViewState other)
        {
            int result = 1;

            if (other != null)
            {
                result = this.order.CompareTo(other.order);
                if (result == 0)
                {
                    result = this.plugin.Id.CompareTo(other.plugin.Id);
                }
            }

            return result;
        }

        private readonly IPlugin plugin;
        private readonly IPluginViewSettings pluginViewSettings;
        private readonly FrameworkElement hostControl;
        private bool enabled = false;
        private int order = int.MaxValue;
    }
}
