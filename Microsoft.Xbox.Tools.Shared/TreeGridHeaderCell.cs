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
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class TreeGridHeaderCell : TreeGridCell
    {
        FrameworkElement thumb;
        Point originSizePos;
        double startingSize;
        bool sizing;

        public TreeGridHeaderCell()
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.thumb = this.Template.FindName("PART_ResizeThumb", this) as FrameworkElement;
            if (this.thumb != null)
            {
                this.thumb.MouseLeftButtonDown += OnThumbButtonDown;
            }
        }

        void OnThumbButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.Column == null)
                return;

            if (e.ClickCount == 2)
            {
                this.Column.Width = double.NaN;
                return;
            }

            originSizePos = e.GetPosition(this);
            startingSize = this.Column.ActualWidth;

            e.MouseDevice.Capture(this.thumb);
            sizing = true;

            this.thumb.MouseMove += OnThumbMouseMove;
            this.thumb.MouseLeftButtonUp += OnThumbButtonUp;
            this.thumb.LostMouseCapture += OnThumbLostMouseCapture;
        }

        void OnThumbMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this);
            this.Column.Width = Math.Max(this.Column.MinWidth, this.startingSize + pos.X - this.originSizePos.X);
        }

        void OnThumbButtonUp(object sender, MouseButtonEventArgs e)
        {
            sizing = false;
            e.MouseDevice.Capture(null);
        }

        void OnThumbLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (sizing)
            {
                this.Column.Width = this.startingSize;
            }

            this.thumb.MouseMove -= this.OnThumbMouseMove;
            this.thumb.MouseLeftButtonUp -= this.OnThumbButtonUp;
            this.thumb.LostMouseCapture -= this.OnThumbLostMouseCapture;
        }
    }
}
