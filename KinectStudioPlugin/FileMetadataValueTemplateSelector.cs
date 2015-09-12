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
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class FileMetadataValueTemplateSelector : DataTemplateSelector
    {
        public FileMetadataValueTemplateSelector()
        {
            DebugHelper.AssertUIThread();

            IServiceProvider serviceProvider = ToolsUIApplication.Instance.RootServiceProvider;
            if (serviceProvider != null)
            {
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DebugHelper.AssertUIThread();

            DataTemplate result = null;

            if (this.pluginService != null)
            {
                if (this.readOnly.GetValueOrDefault(false))
                {
                    if (item is KeyValuePair<string, object>)
                    {
                        KeyValuePair<string, object> keyValue = (KeyValuePair<string, object>)item;
                        result = this.pluginService.GetReadOnlyFileMetadataDataTemplate(keyValue.Value.GetType(), keyValue.Key);
                    }
                }
                else
                {
                    MetadataKeyValuePair keyValue = item as MetadataKeyValuePair;
                    if (keyValue != null)
                    {
                        result = this.pluginService.GetWritableFileMetadataDataTemplate(keyValue.Value.GetType(), keyValue.Key);
                    }
                    else if (item is KeyValuePair<string, object>)
                    {
                        KeyValuePair<string, object> keyValue2 = (KeyValuePair<string, object>)item;
                        result = this.pluginService.GetReadOnlyFileMetadataDataTemplate(keyValue2.Value.GetType(), keyValue2.Key);
                    }
                }
            }

            return result;
        }

        public bool IsReadOnly
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.readOnly.GetValueOrDefault(false);
            }
            set
            {
                DebugHelper.AssertUIThread();

                // not dealing with changing these after initial set up
                if (this.readOnly != null)
                {
                    throw new ArgumentException("IsReadOnly already set");
                }

                this.readOnly = value;
            }
        }

        private bool? readOnly = null;
        private readonly IPluginService pluginService = null;
    }
}
