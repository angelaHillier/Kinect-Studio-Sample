//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class GraphGrid : FrameworkElement
    {
        public static readonly DependencyProperty TimeAxisProperty = DependencyProperty.Register(
            "TimeAxis", typeof(TimeAxis), typeof(GraphGrid));

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            "Foreground", typeof(Brush), typeof(GraphGrid), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnForegroundChanged));

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            "Background", typeof(Brush), typeof(GraphGrid), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HighlightRangeStartProperty = DependencyProperty.Register(
            "HighlightRangeStart", typeof(ulong), typeof(GraphGrid), new FrameworkPropertyMetadata(ulong.MaxValue, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HighlightRangeStopProperty = DependencyProperty.Register(
            "HighlightRangeStop", typeof(ulong), typeof(GraphGrid), new FrameworkPropertyMetadata(ulong.MaxValue, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HighlightBrushProperty = DependencyProperty.Register(
            "HighlightBrush", typeof(Brush), typeof(GraphGrid), new FrameworkPropertyMetadata(Brushes.LightBlue));

        // These indicate whether to show minor grid lines and ticmarks between the major ones
        private bool xShowTween = false;
        private bool yShowTween = false;
        private Pen gridPen = new Pen(Brushes.Gray, 1);

        public GraphGrid()
        {
            CreatePen();
        }

        public TimeAxis TimeAxis
        {
            get { return (TimeAxis)GetValue(TimeAxisProperty); }
            set { SetValue(TimeAxisProperty, value); }
        }

        public GraphDataSideBar SideBar { get; set; }

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }
        
        public ulong HighlightRangeStart
        {
            get { return (ulong)GetValue(HighlightRangeStartProperty); }
            set { SetValue(HighlightRangeStartProperty, value); }
        }

        public ulong HighlightRangeStop
        {
            get { return (ulong)GetValue(HighlightRangeStopProperty); }
            set { SetValue(HighlightRangeStopProperty, value); }
        }

        public Brush HighlightBrush
        {
            get { return (Brush)GetValue(HighlightBrushProperty); }
            set { SetValue(HighlightBrushProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.TimeAxis != null)
            {
                Draw(drawingContext, this.TimeAxis.VisibleTimeStart, this.TimeAxis.VisibleTimeEnd);
            }
        }

        void CreatePen()
        {
            this.gridPen = new Pen(this.Foreground, 1);
        }

        void Draw(DrawingContext drawingContext, ulong visibleTimeStart, ulong visibleTimeEnd)
        {
            Rect rGraphArea = new Rect(0, 0, this.ActualWidth, this.ActualHeight);

            drawingContext.PushClip(new RectangleGeometry(rGraphArea));
            drawingContext.DrawRectangle(this.Background, null, rGraphArea);

            if (this.HighlightRangeStart != ulong.MaxValue)
            {
                if (this.HighlightRangeStart <= visibleTimeEnd && this.HighlightRangeStop >= visibleTimeStart)
                {
                    double startX = Math.Max(this.TimeAxis.TimeToScreen(this.HighlightRangeStart), 0);
                    double stopX = this.ActualWidth;
                    if (this.HighlightRangeStop != ulong.MaxValue)
                    {
                        stopX = Math.Min(stopX, this.TimeAxis.TimeToScreen(this.HighlightRangeStop));
                    }

                    if (stopX > startX)
                    {
                        Rect highlightRect = new Rect(startX, 0, stopX - startX, this.ActualHeight);
                        drawingContext.DrawRectangle(this.HighlightBrush, null, highlightRect);
                    }
                }
            }

            DrawGrid(drawingContext);
            drawingContext.Pop();
        }

        private void DrawGrid(DrawingContext drawingContext)
        {
            double bottom = this.ActualHeight;
            Rect r = new Rect(0, 0, this.ActualWidth, bottom);
            ulong firstVisibleTick;
            ulong lastVisibleTick;
            ulong tickInterval;

            this.TimeAxis.GetTickInfo(out firstVisibleTick, out lastVisibleTick, out tickInterval);

            if (tickInterval == 0)
                return;

            double yFirstTic = 0;
            double yTicInterval = 1;
            double yMax = 100;

            if (this.SideBar != null)
            {
                this.SideBar.GetTickInfo(out yFirstTic, out yTicInterval, out yMax);
            }

            // Draw all tween grid lines before major grid lines
            if (xShowTween)
            {
                for (ulong x = firstVisibleTick + tickInterval / 2; x <= lastVisibleTick; x += tickInterval)
                {
                    double tx = Math.Floor(TimeAxis.TimeToScreen(x)) + 0.5;
                    drawingContext.DrawLine(gridPen, new Point(tx, r.Top), new Point(tx, r.Bottom));
                }
            }

            if (yShowTween)
            {
                for (double y = yFirstTic + yTicInterval / 2; y < yMax; y += yTicInterval)
                {
                    double ty = Math.Floor((bottom - 1) - SideBar.YToPixels(y)) + 0.5;
                    drawingContext.DrawLine(gridPen, new Point(0, ty), new Point(this.ActualWidth, ty));
                }
            }

            // Draw major grid lines
            for (ulong x = firstVisibleTick; x <= lastVisibleTick; x += tickInterval)
            {
                double tx = Math.Floor(TimeAxis.TimeToScreen(x)) + 0.5;
                drawingContext.DrawLine(gridPen, new Point(tx, r.Top), new Point(tx, r.Bottom));
            }

            if (this.SideBar != null)
            {
                for (double y = yFirstTic; y < yMax; y += yTicInterval)
                {
                    double ty = Math.Floor((bottom - 1) - SideBar.YToPixels(y)) + 0.5;
                    drawingContext.DrawLine(gridPen, new Point(0, ty), new Point(this.ActualWidth, ty));
                }
            }
        }

        static void OnForegroundChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            GraphGrid grid = obj as GraphGrid;

            if (grid != null)
            {
                grid.CreatePen();
            }
        }

    }
}
