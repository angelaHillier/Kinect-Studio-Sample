//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System.Windows;
    using System.Windows.Controls;
    using KinectStudioUtility;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class PluginViewSettingsTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DebugHelper.AssertUIThread();

            DataTemplate result = null;

            IPluginEditableViewSettings pluginViewSettings = item as IPluginEditableViewSettings;
            if (pluginViewSettings != null)
            {
                result = pluginViewSettings.SettingsEditDataTemplate;
            }

            if (result == null)
            {
                result = PluginViewSettingsTemplateSelector.emptyDataTemplate;
            }

            return result;
        }

        private static DataTemplate emptyDataTemplate = new DataTemplate();
    }
}
