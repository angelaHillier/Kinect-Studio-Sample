//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    /// <summary>
    /// This is the base class for a bar of data shown in the Timeline.  DataBars are responsible
    /// for managing their own data and drawing.
    /// </summary>
    public class DataBar : Control
    {
        public static readonly DependencyProperty TimeAxisProperty = DependencyProperty.Register(
            "TimeAxis", typeof(TimeAxis), typeof(DataBar));

        public static readonly DependencyProperty VisibleTimeStartProperty = DependencyProperty.Register(
            "VisibleTimeStart", typeof(ulong), typeof(DataBar), new FrameworkPropertyMetadata(0ul, FrameworkPropertyMetadataOptions.AffectsRender, OnVisibleRangePropertyChanged));

        public static readonly DependencyProperty VisibleTimeEndProperty = DependencyProperty.Register(
            "VisibleTimeEnd", typeof(ulong), typeof(DataBar), new FrameworkPropertyMetadata(1ul, FrameworkPropertyMetadataOptions.AffectsRender, OnVisibleRangePropertyChanged));

        public static readonly DependencyProperty TimeStrideProperty = DependencyProperty.Register(
            "TimeStride", typeof(ulong), typeof(DataBar), new FrameworkPropertyMetadata(1ul, FrameworkPropertyMetadataOptions.AffectsRender));

        public DataBar()
        {
            this.SetBinding(VisibleTimeStartProperty, new Binding { Source = this, Path = new PropertyPath("TimeAxis.VisibleTimeStart") });
            this.SetBinding(VisibleTimeEndProperty, new Binding { Source = this, Path = new PropertyPath("TimeAxis.VisibleTimeEnd") });
            this.SetBinding(TimeStrideProperty, new Binding { Source = this, Path = new PropertyPath("TimeAxis.TimeStride") });
        }

        public TimeAxis TimeAxis
        {
            get { return (TimeAxis)GetValue(TimeAxisProperty); }
            set { SetValue(TimeAxisProperty, value); }
        }

        // NOTE:  The visible time range properties have private setters because they're not intended for direct manipulation.
        //        They are binding targets (from the time axis) so they can't be true read-only DP's.
        public ulong VisibleTimeStart
        {
            get { return (ulong)GetValue(VisibleTimeStartProperty); }
            private set { SetValue(VisibleTimeStartProperty, value); }
        }

        public ulong VisibleTimeEnd
        {
            get { return (ulong)GetValue(VisibleTimeEndProperty); }
            private set { SetValue(VisibleTimeEndProperty, value); }
        }

        public ulong TimeStride
        {
            get { return (ulong)GetValue(TimeStrideProperty); }
            private set { SetValue(TimeStrideProperty, value); }
        }

        public virtual IEnumerable<DataBarClipSpan> SelectionClipSpans
        {
            get
            {
                return Enumerable.Empty<DataBarClipSpan>();
            }
        }

        public event EventHandler SelectionClipSpansChanged;
        public event EventHandler TimeRangeChanged;
        public event EventHandler<ZoomToSelectionRequestedEventArgs> ZoomToSelectionRequested;

        public virtual ulong TimeStart { get { return 0ul; } }
        public virtual ulong TimeEnd { get { return 0ul; } }

        public virtual bool TryGetDataSelectionRange(out ulong selectionTimeStart, out ulong selectionTimeEnd)
        {
            selectionTimeEnd = ulong.MinValue;
            selectionTimeStart = ulong.MaxValue;
            return false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.TimeAxis != null)
            {
                Draw(drawingContext, this.TimeAxis.VisibleTimeStart, this.TimeAxis.VisibleTimeEnd);
            }
        }

        public virtual void OnVisibleRangeChanged()
        {
            InvalidateVisual();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            RaiseSelectionClipSpansChanged();
        }

        protected void RaiseSelectionClipSpansChanged()
        {
            var handler = this.SelectionClipSpansChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected void RaiseTimeRangeChanged()
        {
            var handler = this.TimeRangeChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected void RaiseZoomToSelectionRequested(bool causedByUserInput)
        {
            var handler = this.ZoomToSelectionRequested;

            if (handler != null)
            {
                handler(this, new ZoomToSelectionRequestedEventArgs(causedByUserInput));
            }
        }

        public virtual void Draw(DrawingContext drawingContext, ulong tickMinVis, ulong tickMaxVis)
        {
        }

        static void OnVisibleRangePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            DataBar bar = obj as DataBar;

            if (bar != null)
            {
                bar.OnVisibleRangeChanged();
            }
        }
    }

    public class ZoomToSelectionRequestedEventArgs : EventArgs
    {
        public ZoomToSelectionRequestedEventArgs(bool causedByUserInput)
        {
            this.CausedByUserInput = causedByUserInput;
        }

        public bool CausedByUserInput { get; private set; }
    }

    // Simple struct used to represent a vertical span of the selection visual that
    // should be clipped so as to not obscure part of a data bar (for example, the
    // legend section of a graph bar).
    public struct DataBarClipSpan
    {
        public double Top { get; set; }
        public double Height { get; set; }
    }
}
