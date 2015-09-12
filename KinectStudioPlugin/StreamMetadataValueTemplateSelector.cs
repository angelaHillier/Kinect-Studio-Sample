//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using Microsoft.Kinect.Tools;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class StreamMetadataValueTemplateSelector : DataTemplateSelector
    {
        public StreamMetadataValueTemplateSelector()
        {
            DebugHelper.AssertUIThread();

            IServiceProvider serviceProvider = ToolsUIApplication.Instance.RootServiceProvider;
            if (serviceProvider != null)
            {
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DebugHelper.AssertUIThread();

            DataTemplate result = null;

            if (this.pluginService != null)
            {
                string key = null;
                Type valueType = null;
                Guid dataTypeId = Guid.Empty;
                Guid semanticId = Guid.Empty;

                MetadataKeyValuePair kv = item as MetadataKeyValuePair;

                if (kv != null)
                {
                    Debug.Assert(kv.Value != null);

                    key = kv.Key;
                    valueType = kv.Value.GetType();

                    kv.GetStreamIds(out dataTypeId, out semanticId);
                }
                else if (item is KeyValuePair<string, object>)
                {
                    KeyValuePair<string, object> kv2 = (KeyValuePair<string, object>)item;
                    Debug.Assert(kv2.Value != null);

                    key = kv2.Key;
                    valueType = kv2.Value.GetType();

                    // In order to keep the standard Metadata collection as a normal IDictionary (and thus have normal
                    // KeyValuePair) and not have to copy over read-only metadata into a separate collection proxy, 
                    // set the stream as the DataContext of the ListView parent, and just look it up.

                    DependencyObject obj = container;
                    while (true)
                    {
                        obj = VisualTreeHelper.GetParent(obj);
                        if (obj == null)
                        {
                            break;
                        }

                        FrameworkElement element = obj as FrameworkElement;
                        if (element != null)
                        {
                            KStudioEventStream stream = element.Tag as KStudioEventStream;
                            if (stream != null)
                            {
                                dataTypeId = stream.DataTypeId;
                                semanticId = stream.SemanticId;
                                break;
                            }
                        }
                    }
                }

                if (this.readOnly.GetValueOrDefault(false))
                {
                    result = this.pluginService.GetReadOnlyStreamMetadataDataTemplate(valueType, key, dataTypeId, semanticId);
                }
                else
                {
                    result = this.pluginService.GetWritableStreamMetadataDataTemplate(valueType, key, dataTypeId, semanticId);
                }
            }

            return result;
        }

        public bool IsReadOnly
        {
            get
            {
                return this.readOnly.GetValueOrDefault(false);
            }

            set
            {
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

