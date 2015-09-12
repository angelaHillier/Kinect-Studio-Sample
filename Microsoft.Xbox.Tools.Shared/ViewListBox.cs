//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ViewListBox : ListBox
    {
        public Action<object, MouseButtonEventArgs> ListBoxButtonDownAction { get; set; }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            var listBoxItem = element as ListBoxItem;

            if (listBoxItem != null && item is AffinitizedViewCreator)
            {
                listBoxItem.PreviewMouseLeftButtonDown += OnListBoxItemLeftButtonDown;
                listBoxItem.Focusable = false;
            }
        }

        void OnListBoxItemLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.ListBoxButtonDownAction != null)
            {
                this.ListBoxButtonDownAction(sender, e);
                e.Handled = true;
            }
        }
    }
}
