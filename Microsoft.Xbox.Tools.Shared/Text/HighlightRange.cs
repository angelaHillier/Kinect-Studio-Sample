//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public class HighlightRange : DependencyObject
    {
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke", typeof(Brush), typeof(HighlightRange));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            "StrokeThickness", typeof(double), typeof(HighlightRange));

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            "Fill", typeof(Brush), typeof(HighlightRange));

        TrackingTextRange trackingRange;

        public HighlightRange(TextBuffer buffer, TextRange range)
        {
            this.Buffer = buffer;
            this.Range = range;
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public TextBuffer Buffer { get; private set; }

        public object Id { get; set; }

        public TextRange Range
        {
            get
            {
                return this.trackingRange.Range;
            }
            set
            {
                if (this.trackingRange != null)
                {
                    this.RangeChanged -= OnTrackingRangeChanged;
                }

                this.trackingRange = new TrackingTextRange(this.Buffer, value);
                this.trackingRange.RangeChanged += OnTrackingRangeChanged;
            }
        }

        public event EventHandler RangeChanged;

        void OnTrackingRangeChanged(object sender,  EventArgs e)
        {
            var handler = this.RangeChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
