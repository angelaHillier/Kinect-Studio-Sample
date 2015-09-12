//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    /// <summary>
    /// This sits alongside the GraphDataBar and shows the Y axis
    /// </summary>
    public class GraphDataSideBar : FrameworkElement
    {
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            "Foreground", typeof(Brush), typeof(GraphDataSideBar), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            "Background", typeof(Brush), typeof(GraphDataSideBar), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender, OnForegroundChanged));

        private GraphDataBar graphDataBar;

        private Pen axisPen;
        private Typeface axisTypeface = new Typeface(SystemFonts.SmallCaptionFontFamily, SystemFonts.SmallCaptionFontStyle,
            SystemFonts.SmallCaptionFontWeight, FontStretches.Normal);
        private double axisFontSize = 10;

        // Top and bottom of Y axis (not min/max counter data values)
        private double yMin = 0;
        private double yMax = 100;

        // For transformation from Y value to pixel space
        private double yScale = 0.0f;
        private double yTrans = 0.0f;

        // Y ticmark stuff
        private double yFirstTic = 0;
        private double yTicInterval = 1;
        private int yTicDecimalPlaces; // Used when formatting time strings
        private bool yShowTween = false;

        // State variables while panning/zooming the Y axis
        private bool yPanning = false;
        private bool yZooming = false;
        private double yMinInitial;
        private double yMaxInitial;
        private double yMouseInitial; // mouse y at start of zoom or pan

        public event EventHandler YAxisChanged;
        public event EventHandler YAxisDefaultRequested;

        public bool YAxisChangedByUser { get; private set; }

        public GraphDataSideBar(GraphDataBar graphDataBar)
        {
            this.graphDataBar = graphDataBar;
            this.Width = 40;
            UpdateYAxisValues();
            this.SizeChanged += OnSizeChanged;

            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseRightButtonDown += OnMouseRightButtonDown;

            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseRightButtonUp += OnMouseRightButtonUp;
            this.MouseMove += OnMouseMove;
            this.LostMouseCapture += OnLostMouseCapture;

            CreatePen();

            // default scale display converter is just passthrough
            this.YScaleDisplayConverter = y => y;
        }

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public Func<double, double> YScaleDisplayConverter { get; set;  }

        void CreatePen()
        {
            this.axisPen = new Pen(this.Foreground, 1);
        }

        public void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateYAxisValues();
            InvalidateVisual();
            if (YAxisChanged != null)
            {
                YAxisChanged(this, EventArgs.Empty);
            }
        }


        public void GetTickInfo(out double yFirstTic, out double yTicInterval, out double yMax)
        {
            yFirstTic = this.yFirstTic;
            yTicInterval = this.yTicInterval;
            yMax = this.yMax;
        }


        public void SetRange(double yMin, double yMax)
        {
            this.yMin = yMin;
            this.yMax = yMax;
            UpdateYAxisValues();
            InvalidateVisual();
            if (YAxisChanged != null)
            {
                YAxisChanged(this, EventArgs.Empty);
            }
        }


        public void GetRange(out double yMin, out double yMax)
        {
            yMin = this.yMin;
            yMax = this.yMax;
        }


        // Call this when yMax, yMin, or yAxisPanel height changes
        private void UpdateYAxisValues()
        {
            double dy = yMax - yMin;
            // The "-2" fudge factor below is so lines right at yMax are visible,
            // rather than being just off the top of the graph.
            yScale = (this.ActualHeight - 2) / dy;
            yTrans = -(yMin * yScale);
            double yLog = Math.Log10(dy);
            double yLogR = Math.Floor(yLog);
            yTicInterval = Math.Pow(10, yLogR);
            if (yTicInterval * yScale > 150)
                yTicInterval /= 10;
            yShowTween = false;
            if (yTicInterval * yScale > 30)
                yShowTween = true;

            yFirstTic = Math.Floor(yMin / yTicInterval) * yTicInterval;
        }


        public double YToPixels(double y)
        {
            // NOTE: This method doesn't do what you'd think; the value is inverted
            // so callers must do "height - YToPixels(y)" to graph as expected.
            if (double.IsPositiveInfinity(y))
            {
                return this.ActualHeight;
            }
            else if (double.IsNegativeInfinity(y) || double.IsNaN(y))
            {
                return 0;
            }

            return y * yScale + yTrans;
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect r = new Rect(new Point(0, 0), new Size(this.ActualWidth, this.ActualHeight));
            RectangleGeometry rg = new RectangleGeometry(r);

            drawingContext.PushClip(rg);

            // Drawing over the entire rectangle is important in order for mouse events over the background to fire.
            drawingContext.DrawRectangle(this.Background, null, r);

            List<double> yValues = new List<double>();
            List<double> yPositions = new List<double>();

            // Draw tic marks
            bool major = true;
            for (double y = yFirstTic; y <= yMax; y += yTicInterval / 2)
            {
                double ty = (r.Height - 1) - YToPixels(y);
                if (major)
                {
                    drawingContext.DrawLine(axisPen, new Point(r.Right - 7, ty), new Point(r.Right, ty));
                    yValues.Add(y);
                    yPositions.Add(ty);
                }
                else if (yShowTween)
                {
                    drawingContext.DrawLine(axisPen, new Point(r.Right - 5, ty), new Point(r.Right, ty));
                }
                major = !major;
            }

            // Eliminate tic labels that would be even partially cropped vertically
            var ft = new FormattedText("0123456789", CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, axisTypeface, axisFontSize, this.Foreground);
            double stringHeight = ft.Height;
            for (int i = 0; i < yPositions.Count; i++)
            {
                double yMin = yPositions[i] - (stringHeight / 2);
                double yMax = yPositions[i] + (stringHeight / 2);

                if (yMin < 0 || yMax > this.ActualHeight)
                {
                    yValues.RemoveAt(i);
                    yPositions.RemoveAt(i);
                    i--;
                }
            }

            // Determine minimal number of decimal places needed to make
            // all tic labels unique
            string formatString = "F0";
            for (yTicDecimalPlaces = 0; yTicDecimalPlaces < 6; yTicDecimalPlaces++)
            {
                formatString = string.Format("F{0}", yTicDecimalPlaces);
                string prevLabel = "";
                bool allDifferent = true;
                foreach (float y in yValues)
                {
                    double displayConvertedY = this.YScaleDisplayConverter(y);
                    string curLabel = displayConvertedY.ToString(formatString);
                    if (curLabel == prevLabel)
                    {
                        allDifferent = false;
                        break;
                    }
                    prevLabel = curLabel;
                }
                if (allDifferent)
                    break;
            }

            // Draw tic labels
            const int labelOffset = 10;
            for (int i = 0; i < yValues.Count; i++)
            {
                double y = yValues[i];
                double displayConvertedY = this.YScaleDisplayConverter(y);
                double ty = yPositions[i];
                string s = displayConvertedY.ToString(formatString);
                ft = new FormattedText(s, CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, axisTypeface, axisFontSize, this.Foreground);
                drawingContext.DrawText(ft, new Point(r.Right - ft.Width - labelOffset, ty - ft.Height / 2));

                if ((ft.Width + labelOffset) > this.ActualWidth)
                {
                    // Widen the sidebar to make room for the text
                    this.Width = ft.Width + labelOffset;
                }
            }
        }


        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (yPanning)
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                this.YAxisChangedByUser = false;

                // Double-click does a reset to default scale values (as driven by the GraphDataBar,
                // who listens to this event)
                var handler = this.YAxisDefaultRequested;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
            else
            {
                // Single-click/drag does "zoom" scaling
                this.MouseLeftButtonUp += OnMouseLeftButtonUp;
                this.MouseMove += OnMouseMove;
                this.LostMouseCapture += OnLostMouseCapture;
                this.yZooming = true;
                this.yMouseInitial = e.GetPosition(this).Y;
                this.yMinInitial = yMin;
                this.yMaxInitial = yMax;
                e.MouseDevice.Capture(this);
            }

            e.Handled = true;
        }

        public void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (yZooming)
            {
                yZooming = false;
                e.MouseDevice.Capture(null);
                YAxisChangedByUser = true;
            }

            e.Handled = true;
        }


        public void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (yZooming)
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                this.YAxisChangedByUser = false;

                // Double-click does a reset to default scale values (as driven by the GraphDataBar,
                // who listens to this event)
                var handler = this.YAxisDefaultRequested;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
            else
            {
                // Single right-click/drag does panning
                this.MouseRightButtonUp += OnMouseRightButtonUp;
                this.MouseMove += OnMouseMove;
                this.LostMouseCapture += OnLostMouseCapture;
                this.yPanning = true;
                this.yMouseInitial = e.GetPosition(this).Y;
                this.yMinInitial = yMin;
                this.yMaxInitial = yMax;
                e.MouseDevice.Capture(this);
            }

            e.Handled = true;
        }


        public void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (yPanning)
            {
                yPanning = false;
                e.MouseDevice.Capture(null);
                YAxisChangedByUser = true;
            }

            e.Handled = true;
        }


        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (yPanning || yZooming)
            {
                double newYMin = yMin;
                double newYMax = yMax;

                double yRangeInitial = yMaxInitial - yMinInitial;
                double dy = (e.GetPosition(this).Y - this.yMouseInitial);
                double yHeight = this.ActualHeight;

                if (yPanning)
                {
                    double yDeltaNew = dy * yRangeInitial / yHeight;
                    newYMin = yMinInitial + yDeltaNew;
                    newYMax = yMaxInitial + yDeltaNew;
                }

                if (yZooming)
                {
                    double yFactor = 1.0 + (dy / yHeight);
                    double yRangeNew = yRangeInitial * yFactor;
                    // Clamp range to reasonable limits
                    if (yRangeNew > yRangeInitial && yRangeNew > 5000000000.0f)
                        yRangeNew = 5000000000.0f;
                    else if (yRangeNew < yRangeInitial && yRangeNew < 0.01f)
                        yRangeNew = 0.01;

                    newYMax = newYMin + yRangeNew;
                }

                if (newYMin != newYMax)
                {
                    yMin = newYMin;
                    yMax = newYMax;

                    UpdateYAxisValues();
                    InvalidateVisual();
                    if (YAxisChanged != null)
                    {
                        YAxisChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void OnLostMouseCapture(object sender, EventArgs e)
        {
            this.MouseMove -= OnMouseMove;
            this.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            this.MouseRightButtonUp -= OnMouseRightButtonUp;
            this.LostMouseCapture -= OnLostMouseCapture;

            if (this.yPanning || this.yZooming)
            {
                this.yMin = this.yMinInitial;
                this.yMax = this.yMaxInitial;
                UpdateYAxisValues();
                InvalidateVisual();
                if (YAxisChanged != null)
                {
                    YAxisChanged(this, EventArgs.Empty);
                }
            }

            this.yPanning = false;
            this.yZooming = false;
        }

        static void OnForegroundChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            GraphDataSideBar bar = obj as GraphDataSideBar;

            if (bar != null)
            {
                bar.CreatePen();
            }
        }

    }
}
