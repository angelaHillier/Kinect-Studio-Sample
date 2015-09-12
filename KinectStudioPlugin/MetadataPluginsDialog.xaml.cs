//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;

namespace KinectStudioPlugin
{
    public partial class MetadataPluginsDialog : Window
    {
        public MetadataPluginsDialog()
        {
            this.InitializeComponent();
        }

        private void Button_Click_OK(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }

        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Click_MoveUp(object sender, RoutedEventArgs e)
        {
            int index = PluginList.SelectedIndex;
            if (index > 0)
            {
                IList list = PluginList.ItemsSource as IList;
                if (index < list.Count)
                {
                    object item = list[index];
                    list.RemoveAt(index);
                    index--;
                    list.Insert(index, item);
                    PluginList.SelectedIndex = index;
                    PluginList.ScrollIntoView(item);
                }
            }
        }

        private void Button_Click_MoveDown(object sender, RoutedEventArgs e)
        {
            int index = PluginList.SelectedIndex;
            if (index >= 0)
            {
                IList list = PluginList.ItemsSource as IList;
                if (index < (list.Count - 1))
                {
                    object item = list[index];
                    list.RemoveAt(index);
                    index++;
                    list.Insert(index, item);
                    PluginList.SelectedIndex = index;
                    PluginList.ScrollIntoView(item);
                }
            }
        }
    }
}
