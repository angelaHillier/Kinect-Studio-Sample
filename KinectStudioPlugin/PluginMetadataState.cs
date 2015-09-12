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
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    internal class PluginMetadataState : KStudioUserState, IComparable<PluginMetadataState>
    {
        public PluginMetadataState(IPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentOutOfRangeException("plugin");
            }

            this.plugin = plugin;
        }

        public PluginMetadataState(PluginMetadataState pluginMetadataState)
        {
            Debug.Assert(pluginMetadataState != null);

            this.plugin = pluginMetadataState.plugin;
            this.enabled = pluginMetadataState.enabled;
            this.order = pluginMetadataState.order;
        }

        public IPlugin Plugin
        {
            get
            {
                return this.plugin;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
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

        public int CompareTo(PluginMetadataState other)
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
        private bool enabled = false;
        private int order = int.MaxValue;
    }
}
