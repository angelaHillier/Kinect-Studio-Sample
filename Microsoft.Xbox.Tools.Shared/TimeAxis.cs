//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.Xbox.Tools.Shared
{
    public class TimeAxis : FrameworkElement
    {
        public static readonly DependencyProperty IsLiveProperty = DependencyProperty.Register(
            "IsLive", typeof(bool), typeof(TimeAxis), new FrameworkPropertyMetadata(OnIsLiveChanged));

        public static readonly DependencyProperty IsAutoPanningProperty = DependencyProperty.Register(
            "IsAutoPanning", typeof(bool), typeof(TimeAxis));

        static readonly DependencyPropertyKey pixelWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            "PixelWidth", typeof(int), typeof(TimeAxis), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty PixelWidthProperty = pixelWidthPropertyKey.DependencyProperty;

        public static readonly DependencyProperty AbsoluteTimeStartProperty = DependencyProperty.Register(
            "AbsoluteTimeStart", typeof(ulong), typeof(TimeAxis), new FrameworkPropertyMetadata(0ul, FrameworkPropertyMetadataOptions.AffectsRender, OnStrideAffectingPropertyChanged));

        public static readonly DependencyProperty AbsoluteTimeEndProperty = DependencyProperty.Register(
            "AbsoluteTimeEnd", typeof(ulong), typeof(TimeAxis), new FrameworkPropertyMetadata(1000000ul, FrameworkPropertyMetadataOptions.AffectsRender, OnStrideAffectingPropertyChanged));

        public static readonly DependencyProperty VisibleTimeStartProperty = DependencyProperty.Register(
            "VisibleTimeStart", typeof(ulong), typeof(TimeAxis), new FrameworkPropertyMetadata(0ul, FrameworkPropertyMetadataOptions.AffectsRender, OnStrideAffectingPropertyChanged));

        public static readonly DependencyProperty VisibleTimeEndProperty = DependencyProperty.Register(
            "VisibleTimeEnd", typeof(ulong), typeof(TimeAxis), new FrameworkPropertyMetadata(0ul, FrameworkPropertyMetadataOptions.AffectsRender, OnStrideAffectingPropertyChanged));

        public static readonly DependencyProperty MaximumInitialVisibleTimeRangeProperty = DependencyProperty.Register(
            "MaximumInitialVisibleTimeRange", typeof(ulong), typeof(TimeAxis), new FrameworkPropertyMetadata(ulong.MaxValue));

        static readonly DependencyPropertyKey totalVisibleTimePropertyKey = DependencyProperty.RegisterReadOnly(
            "TotalVisibleTime", typeof(ulong), typeof(TimeAxis), new FrameworkPropertyMetadata(0ul));
        public static readonly DependencyProperty TotalVisibleTimeProperty = totalVisibleTimePropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey timeStridePropertyKey = DependencyProperty.RegisterReadOnly(
            "TimeStride", typeof(ulong), typeof(TimeAxis), new FrameworkPropertyMetadata(1ul, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty TimeStrideProperty = timeStridePropertyKey.DependencyProperty;

        public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register(
            "IsAnimationEnabled", typeof(bool), typeof(TimeAxis), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            "Foreground", typeof(Brush), typeof(TimeAxis), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnPenDependentBrushChanged));

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            "Background", typeof(Brush), typeof(TimeAxis), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MousePointBrushProperty = DependencyProperty.Register(
            "MousePointBrush", typeof(Brush), typeof(TimeAxis), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender, OnPenDependentBrushChanged));

        public static readonly DependencyProperty MouseOverElementProperty = DependencyProperty.Register(
            "MouseOverElement", typeof(FrameworkElement), typeof(TimeAxis), new FrameworkPropertyMetadata(OnMouseOverElementChanged));

        public static readonly DependencyProperty AxisFontFamilyProperty = DependencyProperty.Register(
            "AxisFontFamily", typeof(FontFamily), typeof(TimeAxis), new FrameworkPropertyMetadata(SystemFonts.SmallCaptionFontFamily, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFontPropertyChanged));

        public static readonly DependencyProperty AxisFontSizeProperty = DependencyProperty.Register(
            "AxisFontSize", typeof(double), typeof(TimeAxis), new FrameworkPropertyMetadata(10d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFontPropertyChanged));

        public static readonly DependencyProperty AxisFontWeightProperty = DependencyProperty.Register(
            "AxisFontWeight", typeof(FontWeight), typeof(TimeAxis), new FrameworkPropertyMetadata(SystemFonts.SmallCaptionFontWeight, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFontPropertyChanged));

        public static readonly DependencyProperty AxisFontStyleProperty = DependencyProperty.Register(
            "AxisFontStyle", typeof(FontStyle), typeof(TimeAxis), new FrameworkPropertyMetadata(SystemFonts.SmallCaptionFontStyle, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnFontPropertyChanged));

        const double minorTickHeight = 4;
        const double majorTickHeight = 10;
        const double tickLabelTrailingSpace = 8;
        const double thousand = 1000;
        const double million = thousand * thousand;
        const double billion = million * thousand;

        Typeface relativeTimeTypeface;
        Typeface absoluteTimeTypeface;
        bool computingStride;
        bool isVisibleTimeValid;
        Pen axisPen;
        Pen mousePointPen;
        Pen visibleWindowPen;
        double typicalStringWidth;
        ulong firstVisibleMajorTickMark;
        ulong firstVisibleMinorTickMark;
        ulong majorTickMarkInterval;
        ulong minorTickMarkInterval;
        ulong lastVisibleMajorTickMark;
        ulong lastVisibleMinorTickMark;
        double tickGutterHeight;
        double totalHeight;
        CompositionTarget compositionTarget;
        Matrix xformToDevice;
        Matrix xformFromDevice;
        PanAndZoomAnimator animator;

        public event EventHandler VisibleRangeChanged;

        public TimeAxis()
        {
            this.xformFromDevice = Matrix.Identity;
            this.xformToDevice = Matrix.Identity;
            this.SizeChanged += OnSizeChanged;
            this.animator = new PanAndZoomAnimator(this);

            this.SetBinding(AxisFontFamilyProperty, Theme.CreateBinding("TimelineFontFamily"));
            this.SetBinding(AxisFontSizeProperty, Theme.CreateBinding("TimelineFontSize"));
            this.SetBinding(AxisFontWeightProperty, Theme.CreateBinding("TimelineFontWeight"));
            this.SetBinding(AxisFontStyleProperty, Theme.CreateBinding("TimelineFontStyle"));

            CreatePens();
            CreateFonts();
        }

        public bool IsLive
        {
            get { return (bool)GetValue(IsLiveProperty); }
            set { SetValue(IsLiveProperty, value); }
        }

        public bool IsAutoPanning
        {
            get { return (bool)GetValue(IsAutoPanningProperty); }
            set { SetValue(IsAutoPanningProperty, value); }
        }

        public int PixelWidth
        {
            get { return (int)GetValue(PixelWidthProperty); }
            private set { SetValue(pixelWidthPropertyKey, value); }
        }

        public ulong AbsoluteTimeStart
        {
            get { return (ulong)GetValue(AbsoluteTimeStartProperty); }
            set { SetValue(AbsoluteTimeStartProperty, value); }
        }

        public ulong AbsoluteTimeEnd
        {
            get { return (ulong)GetValue(AbsoluteTimeEndProperty); }
            set { SetValue(AbsoluteTimeEndProperty, value); }
        }

        public ulong VisibleTimeStart
        {
            get { return (ulong)GetValue(VisibleTimeStartProperty); }
            set { SetValue(VisibleTimeStartProperty, value); }
        }

        public ulong VisibleTimeEnd
        {
            get { return (ulong)GetValue(VisibleTimeEndProperty); }
            set { SetValue(VisibleTimeEndProperty, value); }
        }

        public ulong MaximumInitialVisibleTimeRange
        {
            get { return (ulong)GetValue(MaximumInitialVisibleTimeRangeProperty); }
            set { SetValue(MaximumInitialVisibleTimeRangeProperty, value); }
        }

        public ulong TotalVisibleTime
        {
            get { return (ulong)GetValue(TotalVisibleTimeProperty); }
            private set { SetValue(totalVisibleTimePropertyKey, value); }
        }

        public ulong TimeStride
        {
            get { return (ulong)GetValue(TimeStrideProperty); }
            private set { SetValue(timeStridePropertyKey, value); }
        }

        public bool IsAnimationEnabled
        {
            get { return (bool)GetValue(IsAnimationEnabledProperty); }
            set { SetValue(IsAnimationEnabledProperty, value); }
        }

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

        public Brush MousePointBrush
        {
            get { return (Brush)GetValue(MousePointBrushProperty); }
            set { SetValue(MousePointBrushProperty, value); }
        }

        public FrameworkElement MouseOverElement
        {
            get { return (FrameworkElement)GetValue(MouseOverElementProperty); }
            set { SetValue(MouseOverElementProperty, value); }
        }

        public FontFamily AxisFontFamily
        {
            get { return (FontFamily)GetValue(AxisFontFamilyProperty); }
            set { SetValue(AxisFontFamilyProperty, value); }
        }

        public double AxisFontSize
        {
            get { return (double)GetValue(AxisFontSizeProperty); }
            set { SetValue(AxisFontSizeProperty, value); }
        }

        public FontWeight AxisFontWeight
        {
            get { return (FontWeight)GetValue(AxisFontWeightProperty); }
            set { SetValue(AxisFontWeightProperty, value); }
        }

        public FontStyle AxisFontStyle
        {
            get { return (FontStyle)GetValue(AxisFontStyleProperty); }
            set { SetValue(AxisFontStyleProperty, value); }
        }

        Matrix TransformFromDevice
        {
            get
            {
                EnsureTransformMatrices();
                return this.xformFromDevice;
            }
        }

        Matrix TransformToDevice
        {
            get
            {
                EnsureTransformMatrices();
                return this.xformToDevice;
            }
        }

        void EnsureTransformMatrices()
        {
            if (this.compositionTarget == null)
            {
                var source = PresentationSource.FromVisual(this);

                if (source != null)
                {
                    this.compositionTarget = source.CompositionTarget;
                    this.xformFromDevice = this.compositionTarget.TransformFromDevice;
                    this.xformToDevice = this.compositionTarget.TransformToDevice;
                }
            }

        }

        void CreatePens()
        {
            this.axisPen = new Pen(this.Foreground, 1);
            this.mousePointPen = new Pen(this.MousePointBrush, 1);
            this.visibleWindowPen = new Pen(this.Foreground, 2);
        }

        void CreateFonts()
        {
            this.relativeTimeTypeface = new Typeface(this.AxisFontFamily, this.AxisFontStyle, this.AxisFontWeight, FontStretches.Normal);
            this.absoluteTimeTypeface = new Typeface(this.AxisFontFamily, this.AxisFontStyle, FontWeights.Bold, FontStretches.Normal);

            var ft = new FormattedText(CreateAbsoluteTimeLabel(1), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, this.relativeTimeTypeface, this.AxisFontSize, this.Foreground);

            this.typicalStringWidth = ft.Width;
            this.tickGutterHeight = Math.Max(Math.Ceiling(ft.Height) + 1, majorTickHeight);
            this.totalHeight = this.tickGutterHeight + Math.Ceiling(ft.Height);
        }

        public void GetTickInfo(out ulong firstVisible, out ulong lastVisible, out ulong interval)
        {
            firstVisible = this.firstVisibleMajorTickMark;
            lastVisible = this.lastVisibleMajorTickMark;
            interval = this.majorTickMarkInterval;
        }

        public TimeAxisState GetTimeAxisState()
        {
            return new TimeAxisState()
            {
                IsAutoPanning = this.IsAutoPanning,
                VisibleTimeStart = this.VisibleTimeStart,
                VisibleTimeEnd = this.VisibleTimeEnd
            };
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public void SetTimeAxisState(TimeAxisState viewState)
        {
            this.animator.Stop();
            this.VisibleTimeStart = viewState.VisibleTimeStart;
            this.VisibleTimeEnd = viewState.VisibleTimeEnd;
            this.IsAutoPanning = viewState.IsAutoPanning;
        }

        public void SetAbsoluteTimeRange(ulong timeStart, ulong timeEnd, bool invalidateVisibleTime)
        {
            bool wasComputingStride = this.computingStride;

            // This method is merely a batched set of property sets, with validation
            // disabled in between.
            this.computingStride = true;
            try
            {
                this.AbsoluteTimeStart = timeStart;
                this.AbsoluteTimeEnd = timeEnd;
                if (invalidateVisibleTime)
                {
                    this.isVisibleTimeValid = false;
                    this.animator.Stop();
                }
                else
                {
                    if (this.IsAutoPanning)
                    {
                        ulong visibleTime = this.TotalVisibleTime;

                        // Keep the latest data visible by sliding the visible range to the max
                        this.VisibleTimeEnd = this.AbsoluteTimeEnd;
                        this.VisibleTimeStart = this.AbsoluteTimeEnd - visibleTime;
                    }
                }
            }
            finally
            {
                this.computingStride = wasComputingStride;
            }

            RecomputeStride();
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.PixelWidth = (int)Math.Ceiling(this.TransformToDevice.Transform(new Point(this.ActualWidth, this.ActualHeight)).X);
            RecomputeStride();
        }

        void RecomputeStride()
        {
            // We can do nothing if we have no pixel width yet.  Also ignore if we're already here.
            if (this.PixelWidth == 0 || this.computingStride)
            {
                return;
            }

            this.computingStride = true;

            try
            {
                // Do absolute time range validation first.  
                if (this.AbsoluteTimeStart >= this.AbsoluteTimeEnd)
                {
                    this.AbsoluteTimeEnd = this.AbsoluteTimeStart + (ulong)this.PixelWidth;
                }

                ulong totalTime = this.AbsoluteTimeEnd - this.AbsoluteTimeStart;

                // To keep math simpler, we enforce an invariant: one pixel is at least one nanosecond.
                // So if the control is wider than the absolute time range, we fudge the time range.
                if (totalTime < (ulong)this.PixelWidth)
                {
                    this.AbsoluteTimeEnd = this.AbsoluteTimeStart + (ulong)this.PixelWidth;
                    totalTime = (ulong)this.PixelWidth;
                }

                // "isVisibleTimeValid" doesn't mean it's been range checked; it means the current
                // values are intentional.  When invalid, it means we need to reset to full extent.
                // (It's set to false when switching documents, for example.)
                if (this.isVisibleTimeValid)
                {
                    ulong currentVisibleTime = this.VisibleTimeEnd - this.VisibleTimeStart;

                    if (this.VisibleTimeStart < this.AbsoluteTimeStart)
                    {
                        this.VisibleTimeStart = this.AbsoluteTimeStart;

                        // Try to keep existing zoom level (by keeping visible time the same)
                        this.VisibleTimeEnd = Math.Min(this.VisibleTimeStart + currentVisibleTime, this.AbsoluteTimeEnd);
                    }

                    if (this.VisibleTimeEnd > this.AbsoluteTimeEnd)
                    {
                        this.VisibleTimeEnd = this.AbsoluteTimeEnd;

                        // Again, try to keep visible time range the same.
                        this.VisibleTimeStart = Math.Max(this.AbsoluteTimeStart, this.VisibleTimeEnd - currentVisibleTime);
                    }
                }
                else
                {
                    this.VisibleTimeStart = this.AbsoluteTimeStart;
                    this.VisibleTimeEnd = this.AbsoluteTimeEnd;

                    if (VisibleTimeEnd - VisibleTimeStart > MaximumInitialVisibleTimeRange)
                    {
                        VisibleTimeStart = VisibleTimeEnd - MaximumInitialVisibleTimeRange;
                    }

                    this.isVisibleTimeValid = true;
                }

                ulong visibleTime = this.VisibleTimeEnd - this.VisibleTimeStart;

                // Same goes for visible time range.  It can't be smaller than the control has pixels.
                if (visibleTime < (ulong)this.PixelWidth)
                {
                    visibleTime = (ulong)this.PixelWidth;
                }

                // The "stride" value is effectively the duration of a pixel.  This value is computed and rounded
                // to a ulong value, and that rounding causes the following two expressions that you would think
                // are equivalent to not be:
                //
                //  visibleTime = VisibleTimeEnd - VisibleTimeStart
                //  visibleTime = TimeStride * PixelWidth
                //
                // These two calculations can be significantly different.  For consistency (not necessarily accuracy),
                // all places where it is needed, the visible time should be calculated using the second expression,
                // because that is effectively how render data is populated (see IEventLaneDataSource.PopulateLaneRenderData).
                // To help assist with this, we provide a TotalVisibleTime property.
                this.TimeStride = (ulong)(visibleTime / (ulong)this.PixelWidth);

                this.TotalVisibleTime = (this.TimeStride * (ulong)this.PixelWidth);

                this.IsAutoPanning = this.IsLive && (this.VisibleTimeEnd == this.AbsoluteTimeEnd);
            }
            finally
            {
                this.computingStride = false;
            }

            UpdateXAxisValues();

            var handler = this.VisibleRangeChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        string CreateRelativeTimeLabel(double deltaTime, double timePerTickMark)
        {
            string units;

            if (timePerTickMark < thousand)         // Less than microsecond
            {
                units = "ns";
            }
            else if (timePerTickMark < million)     // Less than millisecond
            {
                units = "us";
                deltaTime /= thousand;
            }
            else if (timePerTickMark < billion)     // Less than second
            {
                units = "ms";
                deltaTime /= million;
            }
            else
            {
                units = "sec";
                deltaTime /= billion;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:+#;-#} {1}", deltaTime, units);
        }

        string CreateAbsoluteTimeLabel(double timeInNanoseconds)
        {
            ulong time = (ulong)timeInNanoseconds;
            ulong ms = (time % (ulong)billion) / (ulong)million;
            ulong us = (time % (ulong)million) / (ulong)thousand;
            ulong ns = time % (ulong)thousand;
            string format = "{0:hh\\:mm\\:ss\\.fff}.{1:000}.{2:000}";

            // Drop irrelevant (all zero) units
            if (ns == 0)
            {
                if (us == 0)
                {
                    if (ms == 0)
                    {
                        format = "{0:hh\\:mm\\:ss}";
                    }
                    else
                    {
                        format = "{0:hh\\:mm\\:ss\\.fff}";
                    }
                }
                else
                {
                    format = "{0:hh\\:mm\\:ss\\.fff}.{1:000}";
                }
            }

            return string.Format(format, TimeSpan.FromMilliseconds(time / million), us, ns);
        }

        public double TimeToScreen(ulong time)
        {
            // To maintain accuracy/consistency of rounding, never convert directly between screen and time.
            // Always go "through" pixels.
            return PixelToScreen(TimeToPixel(time));
        }

        public int TimeToPixel(ulong time)
        {
            return (int)(((double)time - this.VisibleTimeStart) / this.TimeStride);
        }

        public double PixelToScreen(int pixel)
        {
            return Math.Ceiling(this.TransformFromDevice.Transform(new Point(pixel, 0)).X);
        }

        public int ScreenToPixel(double x)
        {
            return (int)Math.Floor(this.TransformToDevice.Transform(new Point(x, 0)).X);
        }

        public ulong ScreenToTime(double x)
        {
            // To maintain accuracy/consistency of rounding, never convert directly between screen and time.
            // Always go "through" pixels.
            return PixelToTime(ScreenToPixel(x));
        }

        public ulong PixelToTime(int x)
        {
            return ((ulong)x * this.TimeStride) + this.VisibleTimeStart;
        }

        private void UpdateXAxisValues()
        {
            if (this.AbsoluteTimeEnd - this.AbsoluteTimeStart == 0)
                return;

            if (this.VisibleTimeEnd <= this.VisibleTimeStart)
            {
                this.VisibleTimeStart = this.AbsoluteTimeStart;
                this.VisibleTimeEnd = this.AbsoluteTimeEnd;
                this.TotalVisibleTime = this.VisibleTimeEnd - this.VisibleTimeStart;
            }

            // We need these values in doubles for the math to not round badly...
            double timeStart = this.VisibleTimeStart;
            double timeEnd = this.VisibleTimeEnd;
            double visibleTime = this.TotalVisibleTime;
            double xScale = this.ActualWidth / visibleTime;
            double logR = Math.Floor(Math.Log10(visibleTime));

            majorTickMarkInterval = (ulong)Math.Pow(10, logR);

            if (majorTickMarkInterval * xScale > typicalStringWidth * 10)
            {
                majorTickMarkInterval /= 10;
            }
            else if (majorTickMarkInterval * xScale < typicalStringWidth)
            {
                majorTickMarkInterval *= 10;
            }

            this.minorTickMarkInterval = this.majorTickMarkInterval / 10;
            this.firstVisibleMinorTickMark = (ulong)Math.Ceiling(timeStart / minorTickMarkInterval) * minorTickMarkInterval;
            this.firstVisibleMajorTickMark = (ulong)Math.Ceiling(timeStart / majorTickMarkInterval) * majorTickMarkInterval;
            this.lastVisibleMinorTickMark = (ulong)Math.Ceiling(timeEnd / minorTickMarkInterval) * minorTickMarkInterval;
            this.lastVisibleMajorTickMark = (ulong)Math.Ceiling(timeEnd / majorTickMarkInterval) * majorTickMarkInterval;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = availableSize;

            if (double.IsInfinity(size.Width))
            {
                size.Width = 800;       // This is arbitrary; shouldn't be measured with infinity...
            }

            size.Height = this.totalHeight;
            return size;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.majorTickMarkInterval == 0)
                return;

            Rect r = new Rect(new Point(0, 0), new Size(this.ActualWidth, this.ActualHeight));
            RectangleGeometry rg = new RectangleGeometry(r);

            drawingContext.PushClip(rg);

            drawingContext.DrawRectangle(Brushes.Transparent, null, r);
            drawingContext.DrawLine(axisPen, new Point(0, 0.5), new Point(this.ActualWidth, 0.5));

            double totalTimeWidth = Math.Max((double)this.AbsoluteTimeEnd - (double)this.AbsoluteTimeStart, 1d);
            double visibleTimeWidth = (double)this.TotalVisibleTime;
            double visibleWidthPercent = visibleTimeWidth / totalTimeWidth;
            double visiblePixelWidth = Math.Max(this.ActualWidth * visibleWidthPercent, 10);
            double marginTimeWidth = (double)this.VisibleTimeStart;
            double marginWidthPercent = marginTimeWidth / totalTimeWidth;
            double marginPixelWidth = this.ActualWidth * marginWidthPercent;
            double minorTickPixelWidth = this.minorTickMarkInterval * (this.ActualWidth / (double)this.TotalVisibleTime);
            double majorTickPixelWidth = minorTickPixelWidth * 10;

            drawingContext.DrawLine(visibleWindowPen, new Point(marginPixelWidth, 2), new Point(marginPixelWidth + visiblePixelWidth, 2));

            List<TickRecord> ticks = new List<TickRecord>();
            ulong nextMajorTickTime = this.firstVisibleMajorTickMark;
            bool labelWiderThanMinorTick = false;
            bool labelWiderThanHalfMajorTick = false;
            int minorTicksSinceLastMajorTick = 0;
            int absoluteTickIndex = -1;
            int firstMajorTickIndex = -1;
            bool makeFirstVisibleTickAbsolute = (this.ActualWidth / majorTickPixelWidth) < 2.5;
            ulong absoluteTickTime = makeFirstVisibleTickAbsolute ? this.firstVisibleMinorTickMark : this.firstVisibleMajorTickMark;

            // Create tick records
            for (ulong x = firstVisibleMinorTickMark; x < this.lastVisibleMajorTickMark; x += minorTickMarkInterval)
            {
                double tx = Math.Floor(TimeToScreen(x)) + 0.5;

                if (tx >= 0)
                {
                    bool isAbsoluteTimeTick = makeFirstVisibleTickAbsolute ? (ticks.Count == 0) : (x == this.firstVisibleMajorTickMark);
                    bool isMajor = (x == nextMajorTickTime);
                    double interval = (double)this.minorTickMarkInterval;   // (To make major ticks change units on their own schedule, use majorTickMarkInterval here)
                    string text = isAbsoluteTimeTick ? CreateAbsoluteTimeLabel(x) : CreateRelativeTimeLabel((double)x - (double)absoluteTickTime, interval);
                    FormattedText label = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        isAbsoluteTimeTick ? this.absoluteTimeTypeface : this.relativeTimeTypeface, this.AxisFontSize, this.Foreground);

                    var newTick = new TickRecord
                    {
                        IsMajor = isMajor,
                        IsAbsolute = isAbsoluteTimeTick,
                        Position = tx,
                        Value = x,
                        Label = label
                    };

                    if (isAbsoluteTimeTick)
                    {
                        absoluteTickIndex = ticks.Count;
                    }

                    if (isMajor && (firstMajorTickIndex == -1))
                    {
                        firstMajorTickIndex = ticks.Count;
                    }

                    ticks.Add(newTick);

                    if (isMajor)
                    {
                        nextMajorTickTime += majorTickMarkInterval;
                        minorTicksSinceLastMajorTick = 0;
                    }

                    if (!newTick.IsAbsolute)
                    {
                        if ((newTick.Label.Width + tickLabelTrailingSpace) > minorTickPixelWidth)
                        {
                            labelWiderThanMinorTick = true;
                        }

                        if ((minorTicksSinceLastMajorTick == 0 || minorTicksSinceLastMajorTick == 5) && ((newTick.Label.Width + tickLabelTrailingSpace) > (minorTickPixelWidth * 5)))
                        {
                            labelWiderThanHalfMajorTick = true;
                        }
                    }
                }
            }

            // If we're squeezed small enough, we may have no ticks at all.  Oh well.
            if (ticks.Count > 0)
            {
                Debug.Assert(absoluteTickIndex >= 0);

                // By default we're in "every-minor" label mode -- all ticks get labels.
                // If any label is wider than a minor tick, then we drop to "every-fifth-minor" mode.
                // If any label is wider than half a major tick, then we drop all the way to "every-major" mode.
                // Depending on our labeling mode, cull out unneeded/unwanted labels.  
                // First, take care of any ticks prior to the absolute tick.
                for (int i = absoluteTickIndex - 1; i >= 0; i--)
                {
                    if (labelWiderThanHalfMajorTick || (labelWiderThanMinorTick && (i != absoluteTickIndex - 5)))
                    {
                        // Culled because we're in every-fifth-minor or every-major label mode.
                        ticks[i].Label = null;
                    }
                }

                // And now the rest. 
                int minorTickIndex = (absoluteTickIndex > 0) ? 0 : 10 - firstMajorTickIndex;
                double endOfAbsoluteText = ticks[absoluteTickIndex].Label.Width + ticks[absoluteTickIndex].Position + tickLabelTrailingSpace;

                for (int i = absoluteTickIndex; i < ticks.Count; i++, minorTickIndex++)
                {
                    if (i != absoluteTickIndex && ticks[i].Position < endOfAbsoluteText)
                    {
                        // Remove this label because it overlaps the absolute time.  Need to special case this so
                        // the absolute time doesn't throw all the others into every-fifth-minor or every-major label mode.
                        ticks[i].Label = null;
                    }
                    else if (i != absoluteTickIndex && !ticks[i].IsMajor && (labelWiderThanHalfMajorTick || (labelWiderThanMinorTick && (minorTickIndex != 5))))
                    {
                        // Not the absolute tick, and not a major tick, culled because we're in every-fifth-minor or every-major label mode.
                        ticks[i].Label = null;
                    }

                    if (ticks[i].IsMajor)
                    {
                        minorTickIndex = 0;
                    }
                }
            }

            // Draw ticks and labels
            foreach (var record in ticks)
            {
                if (record.Label != null)
                {
                    drawingContext.DrawLine(axisPen, new Point(record.Position, 0), new Point(record.Position, majorTickHeight));
                    drawingContext.DrawText(record.Label, new Point(record.Position, this.tickGutterHeight));
                }
                else
                {
                    drawingContext.DrawLine(axisPen, new Point(record.Position, 0), new Point(record.Position, minorTickHeight));
                }
            }

            drawingContext.Pop(); // pop clip

            if (this.MouseOverElement != null && this.MouseOverElement.IsMouseOver)
            {
                PathGeometry clipGeometry = null;
                var timeline = this.MouseOverElement as Timeline;

                if (timeline != null && timeline.ClipSpans != null && timeline.ClipSpans.Count != 0)
                {
                    double top = 0;
                    PathFigure figure;

                    clipGeometry = new PathGeometry();

                    foreach (var clip in timeline.ClipSpans)
                    {
                        figure = new PathFigure { IsClosed = true, StartPoint = new Point(0, top) };
                        figure.Segments.Add(new LineSegment(new Point(0, clip.Top), false));
                        figure.Segments.Add(new LineSegment(new Point(this.ActualWidth, clip.Top), false));
                        figure.Segments.Add(new LineSegment(new Point(this.ActualWidth, top), false));
                        figure.Segments.Add(new LineSegment(new Point(0, top), false));
                        top = clip.Top + clip.Height;
                        clipGeometry.Figures.Add(figure);
                    }

                    if (top < this.MouseOverElement.ActualHeight)
                    {
                        figure = new PathFigure { IsClosed = true, StartPoint = new Point(0, top) };
                        figure.Segments.Add(new LineSegment(new Point(0, this.MouseOverElement.ActualHeight), false));
                        figure.Segments.Add(new LineSegment(new Point(this.ActualWidth, this.MouseOverElement.ActualHeight), false));
                        figure.Segments.Add(new LineSegment(new Point(this.ActualWidth, top), false));
                        figure.Segments.Add(new LineSegment(new Point(0, top), false));
                        clipGeometry.Figures.Add(figure);
                    }

                    drawingContext.PushClip(clipGeometry);
                }

                int pixelX = (int)Mouse.GetPosition(this).X;
                int pixelY = (int)Mouse.GetPosition(this).Y;
                string timeString = CreateAbsoluteTimeLabel((double)this.PixelToTime(pixelX));
                FormattedText timeText = new FormattedText(timeString, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, this.relativeTimeTypeface, this.AxisFontSize, this.MousePointBrush);
                double rectHeight = Math.Ceiling(timeText.Height);

                drawingContext.DrawRectangle(this.Background, this.mousePointPen, new Rect(pixelX + 0.5, pixelY + 0.5 - rectHeight, Math.Ceiling(timeText.Width) + 6, Math.Ceiling(timeText.Height)));
                drawingContext.DrawText(timeText, new Point(pixelX + 3, pixelY - rectHeight + 0.5));
                drawingContext.DrawLine(this.mousePointPen, new Point(pixelX + 0.5, 0), new Point(pixelX + 0.5, this.MouseOverElement.ActualHeight));

                if (clipGeometry != null)
                {
                    drawingContext.Pop();
                }
            }

            base.OnRender(drawingContext);
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (hitTestParameters.HitPoint.Y > this.ActualHeight)
            {
                // Prevent mouse hits on our mouse-point line
                return null;
            }

            return base.HitTestCore(hitTestParameters);
        }

        public void ZoomAroundCenter(double zoomFactor)
        {
            ZoomAroundPixel(zoomFactor, this.PixelWidth / 2);
        }

        public void ZoomAroundTime(double zoomFactor, ulong centerTime)
        {
            int centerPixel;

            if (centerTime < this.VisibleTimeStart)
            {
                centerPixel = 0;
            }
            else if (centerTime > this.VisibleTimeEnd)
            {
                centerPixel = this.PixelWidth - 1;
            }
            else
            {
                centerPixel = TimeToPixel(centerTime);
            }

            ZoomAroundPixel(zoomFactor, centerPixel);
        }

        public void ZoomAroundPixel(double zoomFactor, int centerPixel)
        {
            ulong visibleTime = this.VisibleTimeEnd - this.VisibleTimeStart;
            ulong totalTime = this.AbsoluteTimeEnd - this.AbsoluteTimeStart;
            ulong timeAtCenter = this.VisibleTimeStart + ((ulong)centerPixel * this.TimeStride);
            ulong newVisibleTime = Math.Max((ulong)this.PixelWidth, Math.Min(totalTime, (ulong)(visibleTime * zoomFactor)));
            ulong newTimeStride = (ulong)(newVisibleTime / (ulong)this.PixelWidth);

            if (newVisibleTime == visibleTime)
            {
                return;
            }

            // If the time stride didn't change, then no zoom will take place.  Take appropriate actions.
            if (newTimeStride == this.TimeStride)
            {
                // At very high zoom levels (where visible time is close to pixel width), the above logic can
                // round poorly resulting in no change in time stride.  So if we can, force a smaller stride.
                if (zoomFactor < 1)
                {
                    if (newTimeStride > 1)
                    {
                        newTimeStride -= 1;
                    }
                    else
                    {
                        // Can zoom no further in.
                        return;
                    }
                }
                else
                {
                    newTimeStride += 1;
                }

                // Must recompute new visible time based on stride
                newVisibleTime = Math.Min(totalTime, newTimeStride * (ulong)this.PixelWidth);
            }

            ulong desiredVisibleTimeStart = timeAtCenter - Math.Min((timeAtCenter - this.AbsoluteTimeStart), ((ulong)centerPixel * newTimeStride));

            if (desiredVisibleTimeStart + newVisibleTime > totalTime)
            {
                desiredVisibleTimeStart = totalTime - newVisibleTime;
            }

            this.VisibleTimeStart = desiredVisibleTimeStart;
            this.VisibleTimeEnd = this.VisibleTimeStart + newVisibleTime;
        }

        public void ZoomIn()
        {
            ZoomAroundCenter(1 / 1.5);
        }

        public void ZoomOut()
        {
            ZoomAroundCenter(1.5);
        }

        public void ZoomToFit()
        {
            ZoomToHere(this.AbsoluteTimeStart, this.AbsoluteTimeEnd);
        }

        public void ZoomToHere(ulong visibleTimeStart, ulong visibleTimeEnd)
        {
            this.animator.AnimateTo(visibleTimeStart, visibleTimeEnd);
        }

        public void PanByScreenDelta(double delta)
        {
            PanByPixels(ScreenToPixel(delta));
        }

        public void PanByPixels(int deltaPixels)
        {
            PanByTime(deltaPixels * (long)this.TimeStride);
        }

        public void PanByTime(long deltaTime)
        {
            ulong visibleTime = this.VisibleTimeEnd - this.VisibleTimeStart;

            if (deltaTime < 0)
            {
                // Because VisibleTimeStart is unsigned, we need to avoid underflow like this
                this.VisibleTimeStart = (ulong)Math.Max((long)this.VisibleTimeStart + deltaTime, (long)this.AbsoluteTimeStart);
            }
            else
            {
                this.VisibleTimeStart = (ulong)Math.Min(this.AbsoluteTimeEnd - visibleTime, this.VisibleTimeStart + (ulong)deltaTime);
            }

            this.VisibleTimeEnd = this.VisibleTimeStart + visibleTime;
        }

        public void PanTo(ulong visibleStartTime)
        {
            ulong currentlyVisibleTime = this.TotalVisibleTime;

            visibleStartTime = Math.Max(visibleStartTime, this.AbsoluteTimeStart);

            if (visibleStartTime + currentlyVisibleTime > this.AbsoluteTimeEnd)
            {
                visibleStartTime = this.AbsoluteTimeEnd - currentlyVisibleTime;
            }

            bool wasComputingStride = this.computingStride;
            this.computingStride = true;
            try
            {
                this.VisibleTimeStart = visibleStartTime;
                this.VisibleTimeEnd = visibleStartTime + currentlyVisibleTime;
            }
            finally
            {
                this.computingStride = wasComputingStride;
            }

            RecomputeStride();
        }

        public void ToggleScrolling()
        {
            if (this.IsAutoPanning)
            {
                // This one's easy -- just stop scrolling.
                this.IsAutoPanning = false;
            }
            else
            {
                // Turn auto-panning on, and then take advantage of the side effect of
                // setting the absolute time range that immediately shifts the visible
                // range to the end.
                this.IsAutoPanning = true;
                SetAbsoluteTimeRange(this.AbsoluteTimeStart, this.AbsoluteTimeEnd, false);
            }
        }

        static void OnIsLiveChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TimeAxis axis = obj as TimeAxis;

            if (axis != null)
            {
                // As a one-time thing, match the IsScrolling value to the IsLive value to make it
                // track as you'd expect. Can't use a binding because the IsScrolling value is independent
                // afterward.
                axis.IsAutoPanning = axis.IsLive;
            }
        }

        static void OnStrideAffectingPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TimeAxis axis = obj as TimeAxis;

            if (axis != null)
            {
                axis.RecomputeStride();
            }
        }

        static void OnPenDependentBrushChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TimeAxis axis = obj as TimeAxis;

            if (axis != null)
            {
                axis.CreatePens();
            }
        }

        static void OnFontPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TimeAxis axis = obj as TimeAxis;

            if (axis != null)
            {
                axis.CreateFonts();
            }
        }

        void OnMouseOverElementMouseEvent(object sender, MouseEventArgs e)
        {
            InvalidateVisual();
        }

        static void OnMouseOverElementChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TimeAxis axis = obj as TimeAxis;

            if (axis != null)
            {
                var oldElement = e.OldValue as FrameworkElement;

                if (oldElement != null)
                {
                    oldElement.PreviewMouseMove -= axis.OnMouseOverElementMouseEvent;
                    oldElement.MouseEnter -= axis.OnMouseOverElementMouseEvent;
                    oldElement.MouseLeave -= axis.OnMouseOverElementMouseEvent;
                }

                if (axis.MouseOverElement != null)
                {
                    axis.MouseOverElement.PreviewMouseMove += axis.OnMouseOverElementMouseEvent;
                    axis.MouseOverElement.MouseEnter += axis.OnMouseOverElementMouseEvent;
                    axis.MouseOverElement.MouseLeave += axis.OnMouseOverElementMouseEvent;
                }

                axis.InvalidateVisual();
            }
        }

        class TickRecord
        {
            public double Value;
            public double Position;
            public bool IsMajor;
            public bool IsAbsolute;
            public FormattedText Label;
        }

        class PanAndZoomAnimator
        {
            ulong visibleStartDest;
            ulong visibleEndDest;
            UInt64Animation runningAnimation;
            UInt64Animation observedAnimation;
            TimeAxis axis;

            public PanAndZoomAnimator(TimeAxis axis)
            {
                this.axis = axis;
            }

            public void Stop()
            {
                if (this.observedAnimation != null)
                {
                    this.observedAnimation.Completed -= OnRunningAnimationCompleted;
                    this.observedAnimation = null;
                    this.runningAnimation = null;
                    this.axis.BeginAnimation(TimeAxis.VisibleTimeStartProperty, null);
                    this.axis.BeginAnimation(TimeAxis.VisibleTimeEndProperty, null);
                    this.axis.VisibleTimeStart = this.visibleStartDest;
                    this.axis.VisibleTimeEnd = this.visibleEndDest;
                }
            }

            public void AnimateTo(ulong visibleTimeStart, ulong visibleTimeEnd)
            {
                if (visibleTimeStart < this.axis.AbsoluteTimeStart)
                {
                    visibleTimeStart = this.axis.AbsoluteTimeStart;
                }

                if (visibleTimeEnd > this.axis.AbsoluteTimeEnd)
                {
                    visibleTimeEnd = this.axis.AbsoluteTimeEnd;
                }

                if( visibleTimeStart > this.axis.AbsoluteTimeEnd )
                {
                    visibleTimeStart = this.axis.AbsoluteTimeStart;
                }

                if (!axis.IsAnimationEnabled)
                {
                    Stop();
                    this.axis.VisibleTimeStart = visibleTimeStart;
                    this.axis.VisibleTimeEnd = visibleTimeEnd;
                    return;
                }

                double acceleration = 0.3;

                if (this.observedAnimation != null)
                {
                    this.observedAnimation.Completed -= OnRunningAnimationCompleted;
                    acceleration = 0d;
                }

                var startAnim = new UInt64Animation { To = visibleTimeStart, AccelerationRatio = acceleration, DecelerationRatio = 0.4, Duration = TimeSpan.FromMilliseconds(1400) };
                var endAnim = new UInt64Animation { To = visibleTimeEnd, AccelerationRatio = acceleration, DecelerationRatio = 0.4, Duration = TimeSpan.FromMilliseconds(1400) };

                this.runningAnimation = startAnim;
                this.observedAnimation = startAnim;
                this.observedAnimation.Completed += OnRunningAnimationCompleted;
                this.visibleStartDest = visibleTimeStart;
                this.visibleEndDest = visibleTimeEnd;
                this.axis.BeginAnimation(TimeAxis.VisibleTimeStartProperty, startAnim);
                this.axis.BeginAnimation(TimeAxis.VisibleTimeEndProperty, endAnim);
            }

            void OnRunningAnimationCompleted(object sender, EventArgs e)
            {
                if (this.observedAnimation == this.runningAnimation)
                {
                    Stop();
                }
            }
        }
    }

    public class TimeAxisState
    {
        public ulong VisibleTimeStart { get; set; }
        public ulong VisibleTimeEnd { get; set; }
        public bool IsAutoPanning { get; set; }

        public TimeAxisState Clone()
        {
            return new TimeAxisState
            {
                VisibleTimeStart = this.VisibleTimeStart,
                VisibleTimeEnd = this.VisibleTimeEnd,
                IsAutoPanning = this.IsAutoPanning
            };
        }
    }

    public class UInt64Animation : AnimationTimeline
    {
        public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
            "To", typeof(ulong?), typeof(UInt64Animation), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
            "From", typeof(ulong?), typeof(UInt64Animation), new FrameworkPropertyMetadata(null));

        public ulong? To
        {
            get { return (ulong?)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        public ulong? From
        {
            get { return (ulong?)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        public override Type TargetPropertyType
        {
            get { return typeof(ulong); }
        }

        public override bool IsDestinationDefault
        {
            get
            {
                return !this.To.HasValue;
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new UInt64Animation();
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            ulong toVal = (ulong)defaultDestinationValue;
            ulong fromVal = (ulong)defaultOriginValue;

            if (this.To.HasValue)
                toVal = this.To.Value;

            if (this.From.HasValue)
                fromVal = this.From.Value;

            if (toVal > fromVal)
            {
                ulong travel = toVal - fromVal;
                ulong delta = (ulong)(animationClock.CurrentProgress.Value * travel);

                return fromVal + delta;
            }
            else
            {
                ulong travel = fromVal - toVal;
                ulong delta = (ulong)(animationClock.CurrentProgress.Value * travel);

                return fromVal - delta;
            }
        }
    }
}
