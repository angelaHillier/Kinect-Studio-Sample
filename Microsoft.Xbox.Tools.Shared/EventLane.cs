//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Xbox.Tools.Shared
{
    public class EventLane : DataBar
    {
        public static readonly DependencyProperty FocusedForegroundProperty = DependencyProperty.Register(
            "FocusedForeground", typeof(Brush), typeof(EventLane), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FocusedBackgroundProperty = DependencyProperty.Register(
            "FocusedBackground", typeof(Brush), typeof(EventLane), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectionForegroundProperty = DependencyProperty.Register(
            "SelectionForeground", typeof(Brush), typeof(EventLane), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectionBackgroundProperty = DependencyProperty.Register(
            "SelectionBackground", typeof(Brush), typeof(EventLane), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FocusedSelectionForegroundProperty = DependencyProperty.Register(
            "FocusedSelectionForeground", typeof(Brush), typeof(EventLane), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FocusedSelectionBackgroundProperty = DependencyProperty.Register(
            "FocusedSelectionBackground", typeof(Brush), typeof(EventLane), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));
        
        public static readonly DependencyProperty BarHeightProperty = DependencyProperty.Register(
            "BarHeight", typeof(double), typeof(EventLane), new FrameworkPropertyMetadata(12d, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty DataSourceProperty = DependencyProperty.Register(
            "DataSource", typeof(IEventLaneDataSource), typeof(EventLane), new FrameworkPropertyMetadata(OnDataSourceChanged));

        public static readonly DependencyProperty LabelFontFamilyProperty = DependencyProperty.Register(
            "LabelFontFamily", typeof(FontFamily), typeof(EventLane), new FrameworkPropertyMetadata(SystemFonts.SmallCaptionFontFamily, OnFontPropertyChanged));

        public static readonly DependencyProperty LabelFontWeightProperty = DependencyProperty.Register(
            "LabelFontWeight", typeof(FontWeight), typeof(EventLane), new FrameworkPropertyMetadata(SystemFonts.SmallCaptionFontWeight, OnFontPropertyChanged));

        public static readonly DependencyProperty LabelFontStyleProperty = DependencyProperty.Register(
            "LabelFontStyle", typeof(FontStyle), typeof(EventLane), new FrameworkPropertyMetadata(SystemFonts.SmallCaptionFontStyle, OnFontPropertyChanged));

        public static readonly DependencyProperty LabelFontSizeProperty = DependencyProperty.Register(
            "LabelFontSize", typeof(double), typeof(EventLane), new FrameworkPropertyMetadata(10d, OnFontPropertyChanged));

        public static readonly DependencyProperty LabelMarginProperty = DependencyProperty.Register(
            "LabelMargin", typeof(double), typeof(EventLane), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty BottomMarginProperty = DependencyProperty.Register(
            "BottomMargin", typeof(double), typeof(EventLane), new FrameworkPropertyMetadata(4d, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty SelectionFanHeightProperty = DependencyProperty.Register(
            "SelectionFanHeight", typeof(double), typeof(EventLane), new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty PixelWidthProperty = DependencyProperty.Register(
            "PixelWidth", typeof(int), typeof(EventLane), new FrameworkPropertyMetadata(OnPixelWidthChanged));

        public static readonly RoutedCommand SelectNodeCommand = new RoutedCommand("Select Node", typeof(EventLane));

        public Brush FocusedForeground
        {
            get { return (Brush)GetValue(FocusedForegroundProperty); }
            set { SetValue(FocusedForegroundProperty, value); }
        }

        public Brush FocusedBackground
        {
            get { return (Brush)GetValue(FocusedBackgroundProperty); }
            set { SetValue(FocusedBackgroundProperty, value); }
        }

        public Brush SelectionForeground
        {
            get { return (Brush)GetValue(SelectionForegroundProperty); }
            set { SetValue(SelectionForegroundProperty, value); }
        }

        public Brush SelectionBackground
        {
            get { return (Brush)GetValue(SelectionBackgroundProperty); }
            set { SetValue(SelectionBackgroundProperty, value); }
        }

        public Brush FocusedSelectionForeground
        {
            get { return (Brush)GetValue(FocusedSelectionForegroundProperty); }
            set { SetValue(FocusedSelectionForegroundProperty, value); }
        }

        public Brush FocusedSelectionBackground
        {
            get { return (Brush)GetValue(FocusedSelectionBackgroundProperty); }
            set { SetValue(FocusedSelectionBackgroundProperty, value); }
        }

        public double BarHeight
        {
            get { return (double)GetValue(BarHeightProperty); }
            set { SetValue(BarHeightProperty, value); }
        }

        public IEventLaneDataSource DataSource
        {
            get { return (IEventLaneDataSource)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }

        public FontStyle LabelFontStyle
        {
            get { return (FontStyle)GetValue(LabelFontStyleProperty); }
            set { SetValue(LabelFontStyleProperty, value); }
        }

        public FontWeight LabelFontWeight
        {
            get { return (FontWeight)GetValue(LabelFontWeightProperty); }
            set { SetValue(LabelFontWeightProperty, value); }
        }

        public FontFamily LabelFontFamily
        {
            get { return (FontFamily)GetValue(LabelFontFamilyProperty); }
            set { SetValue(LabelFontFamilyProperty, value); }
        }

        public double LabelFontSize
        {
            get { return (double)GetValue(LabelFontSizeProperty); }
            set { SetValue(LabelFontSizeProperty, value); }
        }

        public double LabelMargin
        {
            get { return (double)GetValue(LabelMarginProperty); }
            set { SetValue(LabelMarginProperty, value); }
        }

        public double BottomMargin
        {
            get { return (double)GetValue(BottomMarginProperty); }
            set { SetValue(BottomMarginProperty, value); }
        }

        public double SelectionFanHeight
        {
            get { return (double)GetValue(SelectionFanHeightProperty); }
            set { SetValue(SelectionFanHeightProperty, value); }
        }

        public int PixelWidth
        {
            get { return (int)GetValue(PixelWidthProperty); }
            private set { SetValue(PixelWidthProperty, value); }        // NOTE:  Private setter because this is only a binding target (can't be a readonly DP)
        }

        Pen standardPen;
        Typeface labelTypeface;
        double labelTextHeight;
        IEventLaneNode primarySelectedNode;
        IEventLaneNode firstVisibleNode;
        HashSet<IEventLaneNode> selectedNodes = new HashSet<IEventLaneNode>();
        HashSet<IEventLaneNode> selectedNodeParents = new HashSet<IEventLaneNode>();
        IEventLaneNode hotNode;
        ToolTip toolTip = new ToolTip { VerticalOffset = 2, FontSize = 10 };
        bool internalNodeSelection;
        DataTemplate selectNodeMenuItemTemplate;
        VisualCollection childVisuals;
        WriteableBitmap writeableBitmap;
        Image image;
        IEventLaneNode[] renderColumns;
        Palette palette = new Palette();

        public EventLane()
        {
            this.Focusable = true;
            this.FocusVisualStyle = null;
            this.SnapsToDevicePixels = true;
            this.toolTip.PlacementTarget = this;

            this.childVisuals = new VisualCollection(this);
            this.image = new Image();
            RenderOptions.SetBitmapScalingMode(this.image, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(this.image, EdgeMode.Unspecified);
            this.image.Stretch = Stretch.None;
            this.image.HorizontalAlignment = HorizontalAlignment.Left;
            this.image.VerticalAlignment = VerticalAlignment.Top;
            this.image.SnapsToDevicePixels = true;
            this.childVisuals.Add(this.image);

            this.standardPen = CreateAndBindPen(1, ForegroundProperty);

            //var backgroundColorBinding = new Binding { Source = this, Path = new PropertyPath("Background.Color"), FallbackValue = Colors.White };
            //var focusedBackgroundColorBinding = new Binding { Source = this, Path = new PropertyPath("FocusedBackground.Color"), FallbackValue = Colors.White };
            //var labelColorBinding = new Binding { Source = this, Path = new PropertyPath("Foreground.Color"), FallbackValue = Colors.Black };
            //var activeLabelColorBinding = new Binding { Source = this, Path = new PropertyPath("FocusedForeground.Color"), FallbackValue = Colors.Black };

            //this.primarySelectionFanBrush = CreateSelectionFanBrush(activeLabelColorBinding, focusedBackgroundColorBinding);
            //this.nonPrimaryFocusedSelectionFanBrush = CreateSelectionFanBrush(labelColorBinding, focusedBackgroundColorBinding);
            //this.nonPrimaryInactiveSelectionFanBrush = CreateSelectionFanBrush(labelColorBinding, backgroundColorBinding);

            this.SetBinding(PixelWidthProperty, new Binding { Source = this, Path = new PropertyPath("TimeAxis.PixelWidth") });

            this.CommandBindings.Add(new CommandBinding(SelectNodeCommand, OnSelectNodeExecuted));
        }

        void CreateLabelTypeface()
        {
            this.labelTypeface = new Typeface(this.LabelFontFamily, this.LabelFontStyle, this.LabelFontWeight, FontStretches.Normal);
        }

        Pen CreateAndBindPen(double thickness, DependencyProperty penBrushProperty)
        {
            Pen pen = new Pen { Thickness = thickness };
            BindingOperations.SetBinding(pen, Pen.BrushProperty, new Binding { Source = this, Path = new PropertyPath(penBrushProperty) });
            return pen;
        }

        Brush CreateSelectionFanBrush(Binding bottomColorBinding, Binding topColorBinding)
        {
            var lgb = new LinearGradientBrush();
            var gs1 = new GradientStop() { Offset = 0d };
            var gs2 = new GradientStop() { Offset = 0.5d };
            var gs3 = new GradientStop() { Offset = 1d };

            lgb.EndPoint = new Point(0, 1);
            BindingOperations.SetBinding(gs1, GradientStop.ColorProperty, topColorBinding);
            BindingOperations.SetBinding(gs2, GradientStop.ColorProperty, bottomColorBinding);
            BindingOperations.SetBinding(gs3, GradientStop.ColorProperty, bottomColorBinding);
            lgb.GradientStops.Add(gs1);
            lgb.GradientStops.Add(gs2);
            lgb.GradientStops.Add(gs3);

            return lgb;
        }

        void OnSelectedNodesChanged(object sender, EventArgs e)
        {
            ulong startTime = ulong.MaxValue;
            ulong endTime = 0;

            this.selectedNodes.Clear();
            this.selectedNodeParents.Clear();
            foreach (var node in this.DataSource.SelectedNodes)
            {
                this.selectedNodes.Add(node);
                for (var parent = node.Parent; parent != null; parent = parent.Parent)
                {
                    this.selectedNodeParents.Add(parent);
                }

                startTime = Math.Min(node.StartTime, startTime);
                endTime = Math.Max(node.StartTime + node.Duration, endTime);
            }

            if (!this.internalNodeSelection)
            {
                // We didn't cause this selection change, so our primary selected node is bogus.
                this.primarySelectedNode = this.selectedNodes.FirstOrDefault();
            }

            if (this.IsLoaded || this.DoZoomBeforeLoad)
            {
                // Request a zoom-to-selection, indicating whether this was a "user-caused" selection or not.
                this.RaiseZoomToSelectionRequested(this.internalNodeSelection);
            }

            this.InvalidateVisual();
        }

        int GetForegroundColorAsInt()
        {
            return GetColorFromBrushAsInt(this.IsKeyboardFocused ? this.FocusedForeground : this.Foreground);
        }

        int GetBackgroundColorAsInt()
        {
            return GetColorFromBrushAsInt(this.IsKeyboardFocused ? this.FocusedBackground : this.Background);
        }

        int GetColorAsInt(Color color)
        {
            return (color.R << 16) | (color.G << 8) | color.B;
        }

        int GetColorFromBrushAsInt(Brush brush)
        {
            var solidBrush = brush as SolidColorBrush;

            if (solidBrush != null)
            {
                return GetColorAsInt(solidBrush.Color);
            }

            return 0xffffff;
        }

        void UpdateBitmap()
        {
            if (this.renderColumns == null)
            {
                RecreateBitmap();
            }

            if (this.renderColumns == null)
            {
                return;
            }

            Debug.Assert(this.renderColumns.Length == this.PixelWidth);
            Debug.Assert(this.renderColumns.Length == this.TimeAxis.PixelWidth);
            this.DataSource.PopulateLaneRenderData((ulong)this.VisibleTimeStart, this.TimeAxis.TimeStride, renderColumns);

            writeableBitmap.Lock();

            long pBackBuffer = (long)writeableBitmap.BackBuffer;
            int maxHeight = writeableBitmap.PixelHeight;
            int maxWidth = renderColumns.Length;
            int stride = writeableBitmap.BackBufferStride;
            int backgroundColor = GetColorFromBrushAsInt(this.IsKeyboardFocused ? this.FocusedBackground : this.Background);
            int foregroundColor = GetColorFromBrushAsInt(this.IsKeyboardFocused ? this.FocusedForeground : this.Foreground);
            int selectionBackgroundColor = GetColorFromBrushAsInt(this.IsKeyboardFocused ? this.FocusedSelectionBackground : this.SelectionBackground);
            bool barberPole = false;
            int barberPoleIndex = Palette.BarberPoleTop;
            int barberPoleStripeColor = 0;
            IEventLaneNode previousData = null;

            this.firstVisibleNode = null;
            for (int x = 0; x < maxWidth; x++)
            {
                IEventLaneNode nextData = (x < maxWidth - 1) ? renderColumns[x + 1] : null;
                int markerSerif = backgroundColor;

                if (previousData != null && previousData.Style == EventRenderStyle.ParentedMarker)
                {
                    markerSerif = new EventColor(previousData.Color).ColorAsInt;
                }
                else if (nextData != null && nextData.Style == EventRenderStyle.ParentedMarker)
                {
                    markerSerif = new EventColor(nextData.Color).ColorAsInt;
                }

                if (renderColumns[x] == null)
                {
                    this.palette.SliceTop = backgroundColor;
                    this.palette.TopBorder = backgroundColor;
                    this.palette.BarBody = backgroundColor;
                    this.palette.MidLine = foregroundColor;         // Draw the mid-line only -- all else background
                    this.palette.Margin = backgroundColor;
                    this.palette.MarkerTail = backgroundColor;
                    this.palette.MarkerSerif = markerSerif;         // (except marker serifs, which are from adjacent markers)
                    this.palette.BottomBorder = backgroundColor;

                    previousData = null;
                    barberPoleIndex = Palette.BarberPoleTop;
                    barberPole = false;
                }
                else
                {
                    int sliceColor = (renderColumns[x].Style == EventRenderStyle.SlicingMarker) ? foregroundColor : backgroundColor;
                    EventColor barEventColor = new EventColor(renderColumns[x].Color);
                    int barColor = barEventColor.ColorAsInt;
                    int marginColor = backgroundColor;
                    int topBorderColor = backgroundColor;
                    int bottomBorderColor = backgroundColor;
                    int markerColor = backgroundColor;

                    if (this.firstVisibleNode == null)
                    {
                        this.firstVisibleNode = renderColumns[x];
                    }

                    if (this.hotNode != null)
                    {
                        if (this.hotNode == renderColumns[x] || (renderColumns[x].Duration == 0 && renderColumns[x].Parent == this.hotNode))
                        {
                            topBorderColor = selectionBackgroundColor;
                            bottomBorderColor = selectionBackgroundColor;
                        }
                    }

                    if (this.selectedNodes.Contains(renderColumns[x]))
                    {
                        marginColor = selectionBackgroundColor;
                        topBorderColor = selectionBackgroundColor;
                        bottomBorderColor = selectionBackgroundColor;
                    }

                    barberPole = barEventColor.IsBarberPole;
                    barberPoleStripeColor = barEventColor.BarberPoleStripeColorAsInt;

                    if (previousData == renderColumns[x])
                    {
                        if (barberPoleIndex++ == Palette.BarberPoleBottom)
                            barberPoleIndex = Palette.BarberPoleTop;
                    }
                    else
                    {
                        barberPoleIndex = Palette.BarberPoleTop;
                    }

                    if (renderColumns[x].Style == EventRenderStyle.SlicingMarker)
                    {
                        barColor = sliceColor;
                        marginColor = sliceColor;
                        topBorderColor = sliceColor;
                        bottomBorderColor = sliceColor;
                        barberPole = false;
                    }
                    else if (renderColumns[x].Style == EventRenderStyle.ParentedMarker)
                    {
                        // parented markers extend outside the bar, but that extension is selection color (if selected),
                        // so only change color when it is not already different from the background color
                        if (marginColor == backgroundColor)
                        {
                            marginColor = barColor;
                            bottomBorderColor = barColor;
                        }
                        markerColor = barColor;
                        markerSerif = barColor;
                    }
                    else if (x > 0 && ((previousData == null) || (nextData == null) ||
                        (previousData != renderColumns[x] && previousData.Duration > 0)))
                    {
                        barColor = foregroundColor;
                        marginColor = foregroundColor;
                        topBorderColor = foregroundColor;
                        bottomBorderColor = foregroundColor;
                        barberPole = false;
                    }

                    this.palette.SliceTop = sliceColor;
                    this.palette.TopBorder = topBorderColor;
                    this.palette.BarBody = barColor;
                    this.palette.MidLine = barColor;
                    this.palette.Margin = marginColor;
                    this.palette.MarkerTail = markerColor;
                    this.palette.MarkerSerif = markerSerif;
                    this.palette.BottomBorder = bottomBorderColor;

                    previousData = renderColumns[x];
                }

                unsafe
                {
                    for (int y = 0; y < maxHeight; y++)
                    {
                        if (barberPole && (y == barberPoleIndex || y == Palette.BarberPoleTop || y == Palette.BarberPoleBottom))
                        {
                            *(int*)(pBackBuffer + (y * stride) + (x * 4)) = barberPoleStripeColor;
                        }
                        else
                        {
                            *(int*)(pBackBuffer + (y * stride) + (x * 4)) = this.palette.MapPixelIndexToPaletteColor(y);
                        }
                    }
                }
            }

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
            writeableBitmap.Unlock();
        }

        void OnDataSourceRenderInvalidated(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        void OnDataSourceTimeRangeChanged(object sender, EventArgs e)
        {
            RaiseTimeRangeChanged();
        }

        void SelectNode(IEventLaneNode node, bool panIntoView = false)
        {
            this.internalNodeSelection = true;
            try
            {
                this.DataSource.OnNodeSelected(node);
                this.primarySelectedNode = node;

                Focus();
            }
            finally
            {
                this.internalNodeSelection = false;
            }

            if (node != null && panIntoView)
            {
                ulong startTime = node.StartTime;
                ulong endTime = node.StartTime + node.Duration;

                if (startTime < this.TimeAxis.VisibleTimeStart)
                {
                    this.TimeAxis.VisibleTimeStart = startTime;
                }
                else if (endTime > this.TimeAxis.VisibleTimeEnd)
                {
                    this.TimeAxis.VisibleTimeEnd = endTime;
                }
            }
        }

        protected virtual bool DoZoomBeforeLoad
        {
            get
            {
                return true;
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if ((this.labelTextHeight == 0) && (this.LabelFontSize > 0))
            {
                CreateLabelTypeface();

                var measuringText = new FormattedText("X", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, this.labelTypeface, this.LabelFontSize, Brushes.Black);
                this.labelTextHeight = measuringText.Height;
            }

            var height = this.palette.BitmapPixelHeight + this.SelectionFanHeight + this.labelTextHeight + (this.LabelMargin * 2) + this.BottomMargin;

            return new Size(Math.Min(0, constraint.Height), Math.Min(height, constraint.Height));
        }

        protected override Visual GetVisualChild(int index)
        {
            return this.childVisuals[index];
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return this.childVisuals.Count;
            }
        }

        public override ulong TimeStart
        {
            get
            {
                if (this.DataSource != null)
                {
                    return this.DataSource.MinTime;
                }

                return base.TimeStart;
            }
        }

        public override ulong TimeEnd
        {
            get
            {
                if (this.DataSource != null)
                {
                    return this.DataSource.MaxTime;
                }

                return base.TimeEnd;
            }
        }

        public override bool TryGetDataSelectionRange(out ulong selectionTimeStart, out ulong selectionTimeEnd)
        {
            if (this.selectedNodes.Count == 0)
            {
                selectionTimeStart = selectionTimeEnd = 0;
                return false;
            }

            selectionTimeStart = this.selectedNodes.Min(n => n.StartTime);
            selectionTimeEnd = this.selectedNodes.Max(n => n.StartTime + n.Duration);
            return true;
        }

        void RecreateBitmap()
        {
            int width = this.PixelWidth;

            if (width > 0 && (this.writeableBitmap == null || this.writeableBitmap.PixelWidth != width))
            {
                var source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                    double dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;

                    this.writeableBitmap = new WriteableBitmap(width, this.palette.BitmapPixelHeight, dpiX, dpiY, PixelFormats.Bgr32, null);
                    this.renderColumns = new IEventLaneNode[writeableBitmap.PixelWidth];
                    this.image.Source = this.writeableBitmap;
                    InvalidateVisual();
                }
            }
        }

        public override void Draw(DrawingContext drawingContext, ulong firstVisibleTick, ulong lastVisibleTick)
        {
            Rect r = new Rect(0, 0, this.ActualWidth, this.ActualHeight);

            drawingContext.DrawRectangle(this.IsKeyboardFocused ? this.FocusedBackground : this.Background, null, r);

            if (this.DataSource == null)
            {
                return;
            }

            UpdateBitmap();

            drawingContext.PushClip(new RectangleGeometry(r));

            List<LabeledRange> labeledRanges = new List<LabeledRange>();
            FormattedText ftLabel = null;
            const double textMargin = 3;

            if (this.LabelFontSize > 0)
            {
                // Calculate label rect requirements
                foreach (var selectedNode in this.selectedNodes)
                {
                    // We're +1 because there is some kind of sub-pixel misalignment between the event lane itself and the bitmap.
                    var middle = TimeAxis.TimeToScreen(selectedNode.StartTime + (selectedNode.Duration / 2)) + 1;
                    var x1 = TimeAxis.TimeToScreen(selectedNode.StartTime) + 1;
                    var x2 = TimeAxis.TimeToScreen(selectedNode.StartTime + selectedNode.Duration) + 1;
                    if (x1 <= this.PixelWidth && x2 > 0)
                    {
                        if (ftLabel == null)
                        {
                            // NOTE: It is assumed that all selected nodes have the same name...
                            ftLabel = new FormattedText(selectedNode.Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                this.labelTypeface, this.LabelFontSize, this.IsKeyboardFocused ? this.FocusedSelectionForeground : this.SelectionForeground);
                        }

                        // Try to center the label under the node.  But adjust its position if it 
                        // is clipped by the left or right edge of the timeline.
                        var left = middle - ftLabel.Width / 2;
                        if (left < textMargin)
                        {
                            left = textMargin;
                        }
                        else if (left + ftLabel.Width > this.ActualWidth - textMargin)
                        {
                            left = this.ActualWidth - textMargin - ftLabel.Width;
                        }

                        // Create a labeled range for this label
                        var range = new LabeledRange();
                        range.Start = x1;
                        range.End = x2;
                        range.Rect = new Rect(left, this.palette.BitmapPixelHeight, ftLabel.Width, this.SelectionFanHeight + this.labelTextHeight + (this.LabelMargin * 2));
                        range.Rect.Inflate(textMargin, 0);
                        range.ContainsPrimaryNode = (selectedNode == this.primarySelectedNode);

                        labeledRanges.Add(range);
                    }
                }
            }

            if (ftLabel != null)
            {
                List<LabeledRange> mergedRanges = new List<LabeledRange>(labeledRanges.Count);

                // Merge the overlapping label rects together
                while (labeledRanges.Count > 0)
                {
                    LabeledRange range = labeledRanges[0];

                    labeledRanges.RemoveAt(0);

                    for (int i = 0; i < labeledRanges.Count; )
                    {
                        if (range.Rect.IntersectsWith(labeledRanges[i].Rect))
                        {
                            range.Union(labeledRanges[i]);
                            labeledRanges.RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }

                    double centerX = range.Rect.Left + (range.Rect.Width / 2) - (ftLabel.Width / 2);
                    range.Rect = new Rect(centerX, range.Rect.Top, ftLabel.Width, range.Rect.Height);
                    range.Rect.Inflate(textMargin, 0);
                    mergedRanges.Add(range);
                }

                double textRectTop = this.DesiredSize.Height - (ftLabel.Height + this.LabelMargin + this.BottomMargin);
                double textRectBottom = this.DesiredSize.Height - this.BottomMargin;

                // Draw the remaining labeled ranges
                foreach (var range in mergedRanges)
                {
                    double centerX = range.Rect.Left + (range.Rect.Width / 2) - (ftLabel.Width / 2);
                    var geometry = new StreamGeometry() { FillRule = FillRule.EvenOdd };

                    using (StreamGeometryContext ctx = geometry.Open())
                    {
                        ctx.BeginFigure(new Point(range.End, range.Rect.Top), isFilled: true, isClosed: true);
                        ctx.LineTo(new Point(range.Rect.Right, textRectTop), true, true);
                        ctx.LineTo(new Point(range.Rect.Right, textRectBottom), true, true);
                        ctx.LineTo(new Point(range.Rect.Left, textRectBottom), true, true);
                        ctx.LineTo(new Point(range.Rect.Left, textRectTop), true, true);

                        if (range.SubRanges == null)
                        {
                            ctx.LineTo(new Point(range.Start, range.Rect.Top), true, true);
                        }
                        else
                        {
                            double inc = range.Rect.Width / range.SubRanges.Count;
                            double nextValley = range.Rect.Left + inc;

                            for (int i = 0; i < range.SubRanges.Count; i++)
                            {
                                var subrange = range.SubRanges[i];

                                ctx.LineTo(new Point(subrange.Item1, range.Rect.Top), true, true);
                                if (i < range.SubRanges.Count - 1)
                                {
                                    ctx.LineTo(new Point(subrange.Item2, range.Rect.Top), false, true);
                                    ctx.LineTo(new Point(nextValley, textRectTop), true, true);
                                    nextValley += inc;
                                }
                            }
                        }

                        ctx.LineTo(new Point(range.End, range.Rect.Top), false, true);
                    }

                    var brush = this.Background;
                    var foreground = this.Foreground;

                    if (this.IsKeyboardFocused)
                    {
                        if (range.ContainsPrimaryNode)
                        {
                            brush = this.FocusedSelectionBackground;
                            foreground = this.FocusedSelectionForeground;
                        }
                        else
                        {
                            brush = this.FocusedBackground;
                            foreground = this.FocusedForeground;
                        }
                    }

                    drawingContext.DrawGeometry(brush, this.standardPen, geometry);
                    ftLabel.SetForegroundBrush(foreground);
                    drawingContext.DrawText(ftLabel, new Point(centerX, textRectTop));
                }
            }

            drawingContext.Pop();
        }

        protected override void OnIsKeyboardFocusedChanged(DependencyPropertyChangedEventArgs e)
        {
            this.InvalidateVisual();
            base.OnIsKeyboardFocusedChanged(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            IEventLaneNode newHotNode = null;
            Point pos = e.GetPosition(this);

            if (this.DataSource != null && pos.Y <= this.DesiredSize.Height - (this.labelTextHeight + this.LabelMargin * 2))
            {
                newHotNode = this.DataSource.FindNode(this.TimeAxis.ScreenToTime(pos.X), this.TimeAxis.TimeStride);
            }

            if (newHotNode != hotNode)
            {
                hotNode = newHotNode;
                UpdateToolTip();
                InvalidateVisual();
            }

            base.OnMouseMove(e);
        }

        void UpdateToolTip()
        {
            if (hotNode == null)
            {
                toolTip.IsOpen = false;
            }
            else
            {
                var x1 = Math.Max(0, TimeAxis.TimeToScreen(hotNode.StartTime));
                var x2 = Math.Min(TimeAxis.ActualWidth, TimeAxis.TimeToScreen(hotNode.StartTime + hotNode.Duration));
                var r = new Rect(new Point(x1, 0), new Point(x2, this.ActualHeight));
                toolTip.PlacementRectangle = r;
                toolTip.Placement = PlacementMode.Bottom;
                toolTip.Content = this.hotNode.ToolTip;
                toolTip.IsOpen = true;
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (hotNode != null)
            {
                hotNode = null;
                toolTip.IsOpen = false;
                InvalidateVisual();
            }

            base.OnMouseLeave(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (hotNode != null)
            {
                SelectNode(hotNode);
            }

            this.Focus();
            base.OnMouseLeftButtonDown(e);
        }

        int mousePixelAtPanStart;
        ulong visibleStartTimeAtPanStart;
        bool mousePanning;

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (e.MouseDevice.Capture(this))
            {
                this.mousePixelAtPanStart = this.TimeAxis.ScreenToPixel(e.GetPosition(this).X);
                this.visibleStartTimeAtPanStart = this.TimeAxis.VisibleTimeStart;

                this.MouseRightButtonUp += OnPanMouseButtonUp;
                this.MouseMove += OnPanMouseMove;
                this.LostMouseCapture += OnPanLostMouseCapture;
                this.mousePanning = false;
                e.Handled = true;
            }
        }

        void OnPanMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(null);
            e.Handled = this.mousePanning;
        }

        void OnPanMouseMove(object sender, MouseEventArgs e)
        {
            var curPos = e.GetPosition(this);
            var curPixel = this.TimeAxis.ScreenToPixel(curPos.X);
            var pixelDelta = curPixel - this.mousePixelAtPanStart;

            if (!this.mousePanning)
            {
                this.mousePanning = Math.Abs(pixelDelta) > SystemParameters.MinimumHorizontalDragDistance;
            }

            if (this.mousePanning)
            {
                var timeDelta = (long)this.TimeStride * pixelDelta;
                var newStartTime = (ulong)Math.Max((long)this.visibleStartTimeAtPanStart - timeDelta, (long)this.TimeAxis.AbsoluteTimeStart);

                this.TimeAxis.PanTo(newStartTime);
                this.mousePanning = true;
            }
        }

        void OnPanLostMouseCapture(object sender, MouseEventArgs e)
        {
            this.MouseRightButtonUp -= OnPanMouseButtonUp;
            this.MouseMove -= OnPanMouseMove;
            this.LostMouseCapture -= OnPanLostMouseCapture;
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if (!mousePanning)
            {
                var menu = new ContextMenu();

                if (this.selectNodeMenuItemTemplate == null)
                {
                    this.selectNodeMenuItemTemplate = this.FindResource("SelectNodeMenuItemTemplate") as DataTemplate;
                }

                ulong minStart = 0;
                ulong maxEnd = 0;

                for (var node = hotNode; node != null; node = node.Parent)
                {
                    menu.Items.Insert(0, new MenuItem
                    {
                        HeaderTemplate = selectNodeMenuItemTemplate,
                        Command = SelectNodeCommand,
                        CommandTarget = this,
                        CommandParameter = node,
                        IsChecked = this.selectedNodes.Contains(node),
                    });
                    minStart = node.StartTime;
                    maxEnd = node.StartTime + node.Duration;
                }

                double totalPixelWidth = 250;
                double totalTimeWidth = Math.Max((double)maxEnd - (double)minStart, 1d);

                foreach (var item in menu.Items.OfType<MenuItem>())
                {
                    var node = (IEventLaneNode)item.CommandParameter;

                    double nodeTimeWidth = (double)node.Duration;
                    double nodeWidthPercent = Math.Max(nodeTimeWidth / totalTimeWidth, 0.02d);
                    double nodePixelWidth = totalPixelWidth * nodeWidthPercent;
                    double marginTimeWidth = (double)node.StartTime - (double)minStart;
                    double marginWidthPercent = marginTimeWidth / totalTimeWidth;
                    double marginPixelWidth = totalPixelWidth * marginWidthPercent;

                    item.Header = new
                    {
                        Text = node.Name,
                        Fill = new SolidColorBrush(new EventColor(node.Color)),
                        Width = nodePixelWidth,
                        Margin = new Thickness(marginPixelWidth, 3, 0, 0)
                    };
                }

                var timeline = this.FindParent<Timeline>();

                if (timeline != null)
                {
                    if (menu.Items.Count > 0)
                    {
                        menu.Items.Add(new Separator());
                    }

                    foreach (var command in timeline.ContextMenuItems.OfType<ICommand>())
                    {
                        menu.Items.Add(new MenuItem { CommandTarget = timeline, Command = command });
                    }
                }

                menu.IsOpen = true;
                e.Handled = true;
            }
            else
            {
                base.OnMouseRightButtonDown(e);
            }
        }

        void OnSelectNodeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var node = e.Parameter as IEventLaneNode;

            if (node != null)
            {
                SelectNode(node);
            }

            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool right = (e.Key == Key.Right);
            bool left = (e.Key == Key.Left);
            bool up = (e.Key == Key.Up);
            bool down = (e.Key == Key.Down);
            bool plus = (e.Key == Key.Add || e.Key == Key.OemPlus);
            bool minus = (e.Key == Key.Subtract || e.Key == Key.OemMinus);
            bool space = (e.Key == Key.Space || e.Key == Key.Return);
            bool ctrlDown = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control);

            if (ctrlDown || up || down)
            {
                // Control modifier changes arrow keys to pan (L/R)
                if (right || left)
                {
                    this.TimeAxis.PanByScreenDelta((int)((this.ActualWidth * (left ? -1 : 1)) / 8));
                    this.hotNode = null;
                    UpdateToolTip();
                }
                else if (up || down)
                {
                    var newFocus = this.PredictFocus(up ? FocusNavigationDirection.Up : FocusNavigationDirection.Down) as DataBar;
                    if (newFocus != null)
                    {
                        newFocus.Focus();
                    }
                }
                else if (plus)
                {
                    if (this.primarySelectedNode != null)
                    {
                        this.TimeAxis.ZoomAroundTime(0.5d, this.primarySelectedNode.StartTime + (this.primarySelectedNode.Duration / 2));
                    }
                    else
                    {
                        this.TimeAxis.ZoomIn();
                    }
                }
                else if (minus)
                {
                    if (this.primarySelectedNode != null)
                    {
                        this.TimeAxis.ZoomAroundTime(2.0d, this.primarySelectedNode.StartTime + (this.primarySelectedNode.Duration / 2));
                    }
                    else
                    {
                        this.TimeAxis.ZoomOut();
                    }
                }
                else
                {
                    base.OnKeyDown(e);
                    return;
                }

                e.Handled = true;
                return;
            }

            if (this.primarySelectedNode == null)
            {
                e.Handled = (up || down || left || right);

                if (e.Handled && this.firstVisibleNode != null)
                {
                    SelectNode(this.firstVisibleNode, true);
                }

                base.OnKeyDown(e);
                return;
            }

            if (left)
            {
                // Left is the same logic as Up in a standard tree view (so, move to in-order predecessor).
                var predecessor = FindPredecessor(this.primarySelectedNode);

                if (predecessor != null)
                {
                    SelectNode(predecessor, true);
                }
                e.Handled = true;
            }
            else if (right)
            {
                // Right is the same as down, so in-order successor.
                var successor = FindSuccessor(this.primarySelectedNode);

                if (successor != null)
                {
                    SelectNode(successor, true);
                }
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        IEventLaneNode FindPredecessor(IEventLaneNode node)
        {
            var siblings = node.Parent == null ? this.DataSource.Nodes : node.Parent.Children;
            IEventLaneNode predecessor, successor;

            FindNeighbors(siblings, node, out predecessor, out successor);
            if (predecessor != null)
            {
                return FindLast(predecessor);
            }

            return node.Parent;
        }

        IEventLaneNode FindSuccessor(IEventLaneNode node)
        {
            if (node.HasChildren)
            {
                return node.Children.First();
            }

            for (var targetNode = node; targetNode != null; targetNode = targetNode.Parent)
            {
                var siblings = (targetNode.Parent == null) ? this.DataSource.Nodes : targetNode.Parent.Children;
                IEventLaneNode predecessor, successor;

                FindNeighbors(siblings, targetNode, out predecessor, out successor);
                if (successor != null)
                {
                    return successor;
                }
            }

            return null;
        }

        // NOTE: This doesn't consider children, just siblings.
        void FindNeighbors(IEnumerable<IEventLaneNode> nodes, IEventLaneNode node, out IEventLaneNode predecessor, out IEventLaneNode successor)
        {
            predecessor = null;
            successor = null;

            var enumerator = nodes.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (node == enumerator.Current)
                {
                    successor = enumerator.MoveNext() ? enumerator.Current : null;
                    return;
                }

                predecessor = enumerator.Current;
            }

            successor = predecessor = null;
        }

        IEventLaneNode FindLast(IEventLaneNode node)
        {
            if (node.HasChildren)
            {
                return FindLast(node.Children.Last());
            }

            return node;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            InvalidateVisual();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            InvalidateVisual();
            base.OnLostFocus(e);
        }

        static void OnDataSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            EventLane lane = obj as EventLane;

            if (lane != null)
            {
                var oldSource = e.OldValue as IEventLaneDataSource;

                if (oldSource != null)
                {
                    oldSource.RenderInvalidated -= lane.OnDataSourceRenderInvalidated;
                    oldSource.SelectedNodesChanged -= lane.OnSelectedNodesChanged;
                    oldSource.TimeRangeChanged -= lane.OnDataSourceTimeRangeChanged;
                }

                if (lane.DataSource != null)
                {
                    lane.DataSource.RenderInvalidated += lane.OnDataSourceRenderInvalidated;
                    lane.DataSource.SelectedNodesChanged += lane.OnSelectedNodesChanged;
                    lane.DataSource.TimeRangeChanged += lane.OnDataSourceTimeRangeChanged;
                }

                lane.RaiseTimeRangeChanged();
            }
        }

        static void OnFontPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            EventLane lane = obj as EventLane;

            if (lane != null)
            {
                lane.labelTextHeight = 0;
                lane.InvalidateMeasure();
                lane.InvalidateVisual();
            }
        }

        static void OnPixelWidthChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            EventLane lane = obj as EventLane;

            if (lane != null)
            {
                lane.RecreateBitmap();
            }
        }

        class Palette
        {
            static int[] columnPixelColorMap = 
            {
                0,0,0,0,0,0,        // slicing marker top
                1,                  // top border
                4,4,                // margin
                2,2,2,2,            // body
                3,                  // mid line
                2,2,2,2,            // body
                4,4,                // margin
                7,                  // bottom border
                5,5,                // marker tail
                6,                  // marker serif
            };
            public const int BarberPoleTop = 9;
            public const int BarberPoleBottom = 17;

            int[] palette = new int[8];

            public int SliceTop { get { return this.palette[0]; } set { this.palette[0] = value; } }
            public int TopBorder { get { return this.palette[1]; } set { this.palette[1] = value; } }
            public int BarBody { get { return this.palette[2]; } set { this.palette[2] = value; } }
            public int MidLine { get { return this.palette[3]; } set { this.palette[3] = value; } }
            public int Margin { get { return this.palette[4]; } set { this.palette[4] = value; } }
            public int MarkerTail { get { return this.palette[5]; } set { this.palette[5] = value; } }
            public int MarkerSerif { get { return this.palette[6]; } set { this.palette[6] = value; } }
            public int BottomBorder { get { return this.palette[7]; } set { this.palette[7] = value; } }

            public int MapPixelIndexToPaletteColor(int y) { return this.palette[columnPixelColorMap[y]]; }
            public int BitmapPixelHeight { get { return columnPixelColorMap.Length; } }
        }

        struct LabeledRange
        {
            public Rect Rect;
            public List<Tuple<double, double>> SubRanges;
            public double Start;
            public double End;
            public bool ContainsPrimaryNode;

            public void Union(LabeledRange range)
            {
                if (this.SubRanges == null)
                {
                    this.SubRanges = new List<Tuple<double, double>>();
                    this.SubRanges.Add(new Tuple<double, double>(this.Start, this.End));
                }

                this.Start = Math.Min(this.Start, range.Start);
                this.End = Math.Max(this.End, range.End);
                this.Rect.Union(range.Rect);

                if (range.SubRanges == null)
                {
                    this.SubRanges.Add(new Tuple<double, double>(range.Start, range.End));
                }
                else
                {
                    this.SubRanges.AddRange(range.SubRanges);
                }

                this.ContainsPrimaryNode |= range.ContainsPrimaryNode;
            }
        }
    }
}
