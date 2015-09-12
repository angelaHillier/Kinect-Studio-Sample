//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    public class RelativePanel : Panel
    {
        public ulong Minimum
        {
            get
            {
                return (ulong)GetValue(MinimumProperty);
            }
            set
            {
                SetValue(MinimumProperty, value);
            }
        }

        public ulong Maximum
        {
            get
            {
                return (ulong)GetValue(MaximumProperty);
            }
            set
            {
                SetValue(MaximumProperty, value);
            }
        }

        public static ulong GetPosition(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (ulong)obj.GetValue(PositionProperty);
        }

        public static void SetPosition(DependencyObject obj, ulong value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(PositionProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(ulong), typeof(RelativePanel),
            new FrameworkPropertyMetadata((ulong)0, FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.AffectsParentArrange |
                FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(ulong), typeof(RelativePanel),
            new FrameworkPropertyMetadata((ulong)1, FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.AffectsParentArrange |
                FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.RegisterAttached("Position", typeof(ulong), typeof(RelativePanel), new FrameworkPropertyMetadata((ulong)0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        protected override Size ArrangeOverride(Size finalSize)
        {
            ulong min = this.Minimum;
            ulong max = this.Maximum;

            if (max != min)
            {
                double ratio = 1.0 / (max - min) * finalSize.Width;

                foreach (FrameworkElement child in this.Children)
                {
                    double position = (GetPosition(child) - min) * ratio;
                    double width = child.Width;
                    if (double.IsNaN(width))
                    {
                        width = 12.0;
                    }

                    child.Arrange(new Rect(position, 0.0, width, finalSize.Height));
                }
            }

            return finalSize;
        }
    }
}
