//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class TreeGridCell : ContentControl
    {
        public static readonly DependencyProperty IsLeftmostProperty = DependencyProperty.Register(
            "IsLeftmost", typeof(bool), typeof(TreeGridCell));

        public static readonly DependencyProperty OwnerRowProperty = DependencyProperty.Register(
            "OwnerRow", typeof(TreeGridRow), typeof(TreeGridCell));

        public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register(
            "Column", typeof(TreeGridColumn), typeof(TreeGridCell));

        public TreeGridColumn Column
        {
            get { return (TreeGridColumn)GetValue(ColumnProperty); }
            set { SetValue(ColumnProperty, value); }
        }
        
        public TreeGridRow OwnerRow
        {
            get { return (TreeGridRow)GetValue(OwnerRowProperty); }
            set { SetValue(OwnerRowProperty, value); }
        }

        public bool IsLeftmost
        {
            get { return (bool)GetValue(IsLeftmostProperty); }
            set { SetValue(IsLeftmostProperty, value); }
        }
    }
}
