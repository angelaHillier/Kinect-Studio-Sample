//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Xbox.Tools.Shared
{
    public class FloatingWindow : Window
    {
        public SplitTabsControl SplitTabsControl { get; private set; }
        public int ActivationIndex { get; set; }
        public Rect NormalRect { get; private set; }

        public FloatingWindow(SplitTabsControl tabControl)
        {
            this.Content = tabControl;
            this.SplitTabsControl = tabControl;
            this.SizeChanged += this.OnSizeChanged;
            this.LocationChanged += this.OnLocationChanged;
        }

        public void SetLocation(double left, double top, double width, double height)
        {
            this.NormalRect = new Rect(left, top, width, height);
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateNormalWindowRect();
        }

        void OnLocationChanged(object sender, EventArgs e)
        {
            UpdateNormalWindowRect();
        }

        void UpdateNormalWindowRect()
        {
            // Need to dispatch this to later -- location change event fires before window state is updated from normal to maximized
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.NormalRect = new Rect(this.Left, this.Top, this.ActualWidth, this.ActualHeight);
                }
            }), null);
        }
    }
}
