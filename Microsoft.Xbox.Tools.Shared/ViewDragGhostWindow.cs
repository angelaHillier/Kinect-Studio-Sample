//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ViewDragGhostWindow : Window
    {
        public static readonly DependencyProperty ViewNameProperty = DependencyProperty.Register(
            "ViewName", typeof(string), typeof(ViewDragGhostWindow));

        public static readonly DependencyProperty CancelModeProperty = DependencyProperty.Register(
            "CancelMode", typeof(bool), typeof(ViewDragGhostWindow));

        public static readonly DependencyProperty CancelReasonProperty = DependencyProperty.Register(
            "CancelReason", typeof(string), typeof(ViewDragGhostWindow));

        public string ViewName
        {
            get { return (string)GetValue(ViewNameProperty); }
            set { SetValue(ViewNameProperty, value); }
        }

        public bool CancelMode
        {
            get { return (bool)GetValue(CancelModeProperty); }
            set { SetValue(CancelModeProperty, value); }
        }

        public string CancelReason
        {
            get { return (string)GetValue(CancelReasonProperty); }
            set { SetValue(CancelReasonProperty, value); }
        }

        public ViewDragGhostWindow()
        {
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
        }
    }
}
