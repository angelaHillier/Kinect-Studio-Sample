//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.Xbox.Tools.Shared
{
    public class BladePanel : Panel
    {
        double totalHeaderHeight;
        double contentHeight;
        double lastLayoutHeight;

        protected override Size MeasureOverride(Size availableSize)
        {
            double maxWidth = 0;
            double maxHeight = 0;
            double maxContentHeight = 0;

            totalHeaderHeight = 0;

            foreach (var blade in this.InternalChildren.OfType<BladePage>())
            {
                blade.Measure(availableSize);
                maxWidth = Math.Max(maxWidth, blade.DesiredSize.Width);
                maxHeight = Math.Max(maxHeight, blade.DesiredSize.Height);
                maxContentHeight = Math.Max(maxContentHeight, blade.DesiredSize.Height - blade.DesiredHeaderHeight);
                totalHeaderHeight += blade.DesiredHeaderHeight;
            }

            if (double.IsInfinity(availableSize.Height))
            {
                this.contentHeight = maxContentHeight;
                return new Size(maxWidth, maxContentHeight + totalHeaderHeight);
            }
            else
            {
                this.contentHeight = Math.Max(0, availableSize.Height - totalHeaderHeight);
                foreach (var blade in this.InternalChildren.OfType<BladePage>())
                {
                    blade.Measure(new Size(availableSize.Width, this.contentHeight + blade.DesiredHeaderHeight));
                }

                return new Size(maxWidth, availableSize.Height);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double runningTop = 0;
            double animationSpeed = 350;
            bool selectedBladeSeen = false;
            BladePage lastBladePage = null;

            if (finalSize.Height != lastLayoutHeight)
            {
                animationSpeed = 0;
            }

            foreach (var blade in this.InternalChildren.OfType<BladePage>())
            {
                lastBladePage = blade;

                var rect = new Rect(0, 0, finalSize.Width, this.contentHeight + blade.DesiredHeaderHeight);
                blade.Arrange(rect);

                var xlat = blade.RenderTransform as TranslateTransform;

                if (xlat == null)
                {
                    xlat = new TranslateTransform();
                    blade.RenderTransform = xlat;
                    AnimateTransformTo(xlat, runningTop, 0);      // First time we've seen it
                }
                else
                {
                    AnimateTransformTo(xlat, runningTop, animationSpeed);
                }

                if (!selectedBladeSeen && blade.IsSelected)
                {
                    selectedBladeSeen = true;
                    runningTop += this.contentHeight + blade.DesiredHeaderHeight;
                }
                else
                {
                    runningTop += blade.DesiredHeaderHeight;
                }
            }

            this.lastLayoutHeight = finalSize.Height;

            if (!selectedBladeSeen && lastBladePage != null)
            {
                // This will force another arrange pass, but should be very rare
                lastBladePage.IsSelected = true;
            }

            return finalSize;
        }

        void AnimateTransformTo(TranslateTransform transform, double height, double milliseconds)
        {
            var da = new DoubleAnimation(height, TimeSpan.FromMilliseconds(milliseconds)) { AccelerationRatio = 0.3, DecelerationRatio = 0.4 };
            transform.BeginAnimation(TranslateTransform.YProperty, da);
        }
    }
}
