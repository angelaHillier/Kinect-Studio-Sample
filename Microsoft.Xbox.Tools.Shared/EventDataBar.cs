//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class EventDataBar : DataBar
    {
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke", typeof(Brush), typeof(EventDataBar),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectedStrokeProperty = DependencyProperty.Register(
            "SelectedStroke", typeof(Brush), typeof(EventDataBar),
            new FrameworkPropertyMetadata(Brushes.DarkBlue, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HighlightedStrokeProperty = DependencyProperty.Register(
            "HighlightedStroke", typeof(Brush), typeof(EventDataBar),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AxisStrokeProperty = DependencyProperty.Register(
            "AxisStroke", typeof(Brush), typeof(EventDataBar),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            "Fill", typeof(Brush), typeof(EventDataBar),
            new FrameworkPropertyMetadata(Brushes.PowderBlue, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectedFillProperty = DependencyProperty.Register(
            "SelectedFill", typeof(Brush), typeof(EventDataBar),
            new FrameworkPropertyMetadata(SystemColors.HighlightBrush, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty InactiveSelectedFillProperty = DependencyProperty.Register(
            "InactiveSelectedFill", typeof(Brush), typeof(EventDataBar),
            new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HighlightedFillProperty = DependencyProperty.Register(
            "HighlightedFill", typeof(Brush), typeof(EventDataBar),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xff, 0xf0, 0xf0, 0x40)), FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public Brush SelectedStroke
        {
            get { return (Brush)GetValue(SelectedStrokeProperty); }
            set { SetValue(SelectedStrokeProperty, value); }
        }

        public Brush HighlightedStroke
        {
            get { return (Brush)GetValue(HighlightedStrokeProperty); }
            set { SetValue(HighlightedStrokeProperty, value); }
        }

        public Brush AxisStroke
        {
            get { return (Brush)GetValue(AxisStrokeProperty); }
            set { SetValue(AxisStrokeProperty, value); }
        }

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public Brush SelectedFill
        {
            get { return (Brush)GetValue(SelectedFillProperty); }
            set { SetValue(SelectedFillProperty, value); }
        }

        public Brush InactiveSelectedFill
        {
            get { return (Brush)GetValue(InactiveSelectedFillProperty); }
            set { SetValue(InactiveSelectedFillProperty, value); }
        }

        public Brush HighlightedFill
        {
            get { return (Brush)GetValue(HighlightedFillProperty); }
            set { SetValue(HighlightedFillProperty, value); }
        }

        // Note: Changing this value doesn't take effect until new data comes in.
        // The intent is just to support setting this at initialization depending
        // on the desired behavior.
        public bool DynamicBarHeight { get; set; }

        public bool UseEventColor { get; set; }

        ToolTip toolTip = new ToolTip()
        {
            VerticalOffset = 2,
            FontSize = 10,
        };

        Pen axisPen;
        Pen standardPen;
        Pen selectedPen;
        Pen highlightedPen;
        Pen slicingMarkerPen;
        const double verticalMargin = 14d;
        const double halfHeight = 8d;
        StreamGeometry selectionArrowGeometry;
        StreamGeometry hotArrowGeometry;
        Dictionary<uint, Brush> customBrushes = new Dictionary<uint, Brush>();
        IEventDataNode[] sortedNodes;
        IEventDataNode[] slicingMarkers;

        private Typeface labelTypeface = new Typeface(SystemFonts.SmallCaptionFontFamily, SystemFonts.SmallCaptionFontStyle,
            SystemFonts.SmallCaptionFontWeight, FontStretches.Normal);
        private double labelFontSize = 10;

        IEventDataSource dataSource;
        IEventDataSelection dataSelection;
        HashSet<IEventDataNode> selectedChunks = new HashSet<IEventDataNode>();
        IEventDataNode hotChunk; // Visual feedback on chunk under the mouse

        public EventDataBar()
        {
            this.Height = 43;
            toolTip.PlacementTarget = this;
            this.Focusable = true;

            this.axisPen = CreateAndBindPen(1, AxisStrokeProperty);
            this.standardPen = CreateAndBindPen(1, StrokeProperty);
            this.selectedPen = CreateAndBindPen(0.5, SelectedStrokeProperty);
            this.highlightedPen = CreateAndBindPen(0.5, HighlightedStrokeProperty);
            this.slicingMarkerPen = new Pen(Brushes.Black, .5);

            selectionArrowGeometry = new StreamGeometry()
            {
                FillRule = FillRule.EvenOdd
            };

            using (StreamGeometryContext ctx = selectionArrowGeometry.Open())
            {
                ctx.BeginFigure(new Point(0, 0), isFilled: true, isClosed: true);
                ctx.LineTo(new Point(3, -6), isStroked: true, isSmoothJoin: false);
                ctx.LineTo(new Point(-3, -6), isStroked: true, isSmoothJoin: false);
            }

            // The hot arrow needs its own geometry because it will have a different transform
            hotArrowGeometry = selectionArrowGeometry.Clone();

            DrawingBrush unattributedBrush = new DrawingBrush();
            GeometryDrawing square = new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, 10, 10)));
            GeometryDrawing line = new GeometryDrawing(null, slicingMarkerPen, new LineGeometry(new Point(0, 0), new Point(10, 10)));

            DrawingGroup drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(square);
            drawingGroup.Children.Add(line);

            unattributedBrush.Drawing = drawingGroup;
            unattributedBrush.Viewport = new Rect(0, 0, 10, 10);
            unattributedBrush.ViewportUnits = BrushMappingMode.Absolute;
            unattributedBrush.Viewbox = new Rect(0, 0, 10, 10);
            unattributedBrush.ViewboxUnits = BrushMappingMode.Absolute;
            unattributedBrush.TileMode = TileMode.Tile;

            customBrushes[EventColor.UnattributedColor] = unattributedBrush;

            DrawingBrush systemProcessBrush = new DrawingBrush();
            square = new GeometryDrawing(Brushes.Black, null, new RectangleGeometry(new Rect(0, 0, 10, 10)));
            line = new GeometryDrawing(null, new Pen(Brushes.White, 1), new LineGeometry(new Point(0, 0), new Point(10, 10)));

            drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(square);
            drawingGroup.Children.Add(line);

            systemProcessBrush.Drawing = drawingGroup;
            systemProcessBrush.Viewport = new Rect(0, 0, 10, 10);
            systemProcessBrush.ViewportUnits = BrushMappingMode.Absolute;
            systemProcessBrush.Viewbox = new Rect(0, 0, 10, 10);
            systemProcessBrush.ViewboxUnits = BrushMappingMode.Absolute;
            systemProcessBrush.TileMode = TileMode.Tile;

            customBrushes[EventColor.SystemProcessColor] = systemProcessBrush;
        }

        Pen CreateAndBindPen(double thickness, DependencyProperty penBrushProperty)
        {
            Pen pen = new Pen { Thickness = thickness };

            BindingOperations.SetBinding(pen, Pen.BrushProperty, new Binding { Source = this, Path = new PropertyPath(penBrushProperty) });
            return pen;
        }

        public void SetDataSource(IEventDataSource dataSource)
        {
            if (this.dataSource != null)
            {
                this.dataSource.DataChanged -= OnDataChanged;
                this.dataSelection.SelectedNodesChanged -= OnSelectedNodesChanged;
            }

            this.dataSource = dataSource;
            this.dataSource.DataChanged += OnDataChanged;

            this.dataSelection = dataSource as IEventDataSelection;
            if (this.dataSelection == null)
            {
                this.dataSelection = new DefaultEventDataSelection(dataSource);
            }

            this.dataSelection.SelectedNodesChanged += OnSelectedNodesChanged;

            ProcessNodes();
        }

        void OnSelectedNodesChanged(object sender, EventArgs e)
        {
            ulong startTime = ulong.MaxValue;
            ulong endTime = 0;

            this.selectedChunks.Clear();
            foreach (var chunk in this.dataSelection.SelectedNodes)
            {
                this.selectedChunks.Add(chunk);
                startTime = Math.Min(chunk.StartTime, startTime);
                endTime = Math.Max(chunk.StartTime + chunk.Duration, endTime);
            }

            if (startTime < endTime)
            {
                ulong visibleTimeStart = this.TimeAxis.VisibleTimeStart;
                ulong visibleTimeEnd = this.TimeAxis.VisibleTimeEnd;

                // If the selection doesn't fit, zoom.  Otherwise, pan it into view as needed.
                if ((visibleTimeEnd - visibleTimeStart) < (endTime - startTime))
                {
                    TimeAxis.ZoomToHere(startTime, endTime);
                }
                else if (startTime > visibleTimeEnd)
                {
                    long deltaTicks = (long)endTime - (long)visibleTimeEnd;
                    TimeAxis.PanByTime(deltaTicks);
                }
                else if (endTime < visibleTimeStart)
                {
                    long deltaTicks = (long)startTime - (long)visibleTimeStart;
                    TimeAxis.PanByTime(deltaTicks);
                }
            }

            InvalidateVisual();
        }

        void OnDataChanged(object sender, EventArgs e)
        {
            OnSelectedNodesChanged(this.dataSelection, EventArgs.Empty);
            ProcessNodes();
            InvalidateVisual();
        }

        void ProcessNodes()
        {
            this.sortedNodes = this.dataSource.Nodes.Where(n => n.Style != EventRenderStyle.SlicingMarker).OrderBy(n => n.ZIndex).ThenBy(n => n.StartTime).ToArray();
            this.slicingMarkers = this.dataSource.Nodes.Where(n => n.Style == EventRenderStyle.SlicingMarker).ToArray();

            if (DynamicBarHeight)
            {
                int maxVisDepth = -1;
                if (this.sortedNodes.Length > 0)
                {
                    // Uhm, we just sorted by ZIndex, right?
                    maxVisDepth = this.sortedNodes[this.sortedNodes.Length - 1].ZIndex;
                }

                if (maxVisDepth == -1 && this.slicingMarkers.Length == 0)
                {
                    // Compress the bar height dramatically if there are no chunks at all drawn
                    this.Height = 23;
                }
                else
                {
                    // Adjust the bar height based on maximum nested depth of chunks drawn
                    this.Height = 39 + maxVisDepth * 4;
                }
            }
        }

        void SelectChunk(IEventDataNode chunk)
        {
            dataSource.OnSelectionChangedInternal(chunk);
            this.Focus();
        }

        void DrawLastDelayedAndForcedDrawChunks(DrawingContext drawingContext, ChunkDrawData drawData, List<IEventDataNode> forceDrawChunks)
        {
            if (drawData != null)
            {
                drawingContext.DrawRectangle(drawData.Fill, drawData.Pen, drawData.Rect);
            }

            foreach (var forcedChunk in forceDrawChunks)
            {
                Brush brush = this.SelectedFill;
                Pen pen = this.selectedPen;

                if (forcedChunk == this.hotChunk)
                {
                    brush = this.HighlightedFill;
                    pen = this.highlightedPen;
                }

                DrawChunk(drawingContext, forcedChunk, brush, pen, null, forceDraw: true);
            }
        }

        public override void Draw(DrawingContext drawingContext, ulong tickMinVis, ulong tickMaxVis)
        {
            Rect r = new Rect(new Point(0, 0), new Size(this.ActualWidth, this.ActualHeight));

            // Use a slightly oversize clip region.  Otherwise the user wouldn't see the left edge 
            // of the outline on an event starting right at zero.
            Rect r2 = new Rect(new Point(-2, 0), new Size(this.ActualWidth + 4, this.ActualHeight));
            RectangleGeometry rg = new RectangleGeometry(r2);
            drawingContext.PushClip(rg);

            drawingContext.DrawRectangle(Brushes.Transparent, null, r);

            drawingContext.DrawLine(axisPen, new Point(0, this.ActualHeight / 2), new Point(this.ActualWidth, this.ActualHeight / 2));

            bool doOptimizedBorders = false;

            // Chunks are ordered by z-index, then by start time.  For each z-index, chunks should be in left-to-right order
            // and not overlap.  Track the maximum drawn X pixel for each z-index pass, so we can draw very small events
            // without drawing a crazy number of rectangles.  See DrawChunk() for the comparisons against maximumDrawnX.
            int currentZIndex = -1;
            ChunkDrawData previousChunkData = null;
            List<IEventDataNode> forceDrawChunks = new List<IEventDataNode>();

            foreach (var chunk in this.sortedNodes)
            {
                if (chunk.ZIndex > currentZIndex)
                {
                    currentZIndex = chunk.ZIndex;

                    // Draw the previous chunk data (if any) and the forced-drawn (selected/hot) chunks at the previous level
                    DrawLastDelayedAndForcedDrawChunks(drawingContext, previousChunkData, forceDrawChunks);
                    previousChunkData = null;
                    forceDrawChunks.Clear();

                    // If dataSource.ContiguousEvents is true, then we are guaranteed that the events are
                    // always right next to each other -- but only at level 0.  At higher levels, they may
                    // not be contiguous due to the tree expansion state.  The "doOptimizedBorders" mode is to 
                    // fill the entire region with the outline color, then draw the chunks with no borders
                    // (if they are big enough to be visible).
                    doOptimizedBorders = (dataSource.ContiguousEvents && currentZIndex == 0);

                    if (doOptimizedBorders)
                    {
                        // Draw the full bar in the standard pen brush color, as if it were full of 1-px wide chunks.  
                        r = new Rect(new Point(0, verticalMargin), new Size(this.ActualWidth, this.ActualHeight - (verticalMargin * 2)));
                        if (currentZIndex > 0)
                        {
                            double delta = chunk.ZIndex * 2;
                            r.Inflate(0, -delta);
                        }
                        drawingContext.DrawRectangle(this.Stroke, null, r);
                    }
                }

                if (chunk.Visible)
                {
                    Brush brush;
                    Pen pen;

                    if (chunk == hotChunk || this.selectedChunks.Contains(chunk))
                    {
                        // We need to draw these a second time.  If we skip them, it causes different
                        // merging behavior and you get visual artifacts as you select/hover over events.
                        forceDrawChunks.Add(chunk);
                    }

                    pen = doOptimizedBorders ? null : standardPen;

                    if (UseEventColor)
                    {
                        uint nodeColor = chunk.Color;
                        if (!customBrushes.TryGetValue(nodeColor, out brush))
                        {
                            brush = new SolidColorBrush(new EventColor(nodeColor));
                            customBrushes.Add(nodeColor, brush);
                        }
                    }
                    else
                    {
                        brush = this.Fill;
                    }

                    previousChunkData = DrawChunk(drawingContext, chunk, brush, pen, previousChunkData, forceDraw: false);
                }
            }

            DrawLastDelayedAndForcedDrawChunks(drawingContext, previousChunkData, forceDrawChunks);

            double? lastDrawnSlicingMarkerX = null;

            foreach (var marker in this.slicingMarkers)
            {
                lastDrawnSlicingMarkerX = DrawSlicingMarker(drawingContext, marker, lastDrawnSlicingMarkerX);
            }

            List<Rect> labelRects = new List<Rect>();
            FormattedText ftLabel = null;

            // Draw selected chunk arrows, and calculate label rect requirements
            foreach (var selectedChunk in this.selectedChunks)
            {
                if (selectedChunk != hotChunk)
                {
                    var brush = IsFocused ? this.SelectedFill : this.InactiveSelectedFill;
                    var middle = TimeAxis.TimeToScreen(selectedChunk.StartTime + (selectedChunk.Duration / 2));
                    double heightAdjust = 0.5;

                    if (selectedChunk.Style == EventRenderStyle.SlicingMarker)
                    {
                        heightAdjust = verticalMargin - 6;
                    }

                    var geom = selectionArrowGeometry.Clone();
                    geom.Transform = new TranslateTransform(middle, verticalMargin - heightAdjust);
                    drawingContext.DrawGeometry(brush, selectedPen, geom);

                    var x1 = TimeAxis.TimeToScreen(selectedChunk.StartTime);
                    var x2 = TimeAxis.TimeToScreen(selectedChunk.StartTime + selectedChunk.Duration);
                    if (x1 < this.ActualWidth && x2 > 0)
                    {
                        // Draw label, if selected chunk is visible
                        if (ftLabel == null)
                        {
                            // NOTE:  It is assumed that all selected chunks have the same name...
                            ftLabel = new FormattedText(selectedChunk.Name, CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight, labelTypeface, labelFontSize, Brushes.Black);
                        }

                        // Try to center the label under the event.  But adjust its position if it 
                        // is clipped by the left or right edge of the timeline.
                        var left = middle - ftLabel.Width / 2;
                        if (left < 0)
                        {
                            left = 0;
                        }
                        else if (left + ftLabel.Width > this.ActualWidth)
                        {
                            left = this.ActualWidth - ftLabel.Width;
                        }

                        // Create a rect (top/height insignificant) for this label and remember it
                        labelRects.Add(new Rect(left, 0, ftLabel.Width, 10));
                    }
                }
            }

            if (ftLabel != null)
            {
                List<Rect> mergedRects = new List<Rect>(labelRects.Count);

                // Merge the overlapping label rects together
                while (labelRects.Count > 0)
                {
                    Rect rect = labelRects[0];

                    labelRects.RemoveAt(0);

                    for (int i = 0; i < labelRects.Count; )
                    {
                        if (rect.IntersectsWith(labelRects[i]))
                        {
                            rect.Union(labelRects[i]);
                            labelRects.RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }

                    mergedRects.Add(rect);
                }

                // Draw the text centered in each of the remaining label rects
                foreach (var rect in mergedRects)
                {
                    double centerX = rect.Left + (rect.Width / 2) - (ftLabel.Width / 2);
                    drawingContext.DrawText(ftLabel, new Point(centerX, this.ActualHeight - verticalMargin));
                }
            }

            // Draw hot chunk arrow
            if (hotChunk != null)
            {
                var middle = TimeAxis.TimeToScreen(hotChunk.StartTime + (hotChunk.Duration / 2));
                double heightAdjust = 0.5;

                if (hotChunk.Style == EventRenderStyle.SlicingMarker)
                {
                    heightAdjust = verticalMargin - 6;
                }

                hotArrowGeometry.Transform = new TranslateTransform(middle, verticalMargin - heightAdjust);
                drawingContext.DrawGeometry(this.HighlightedFill, this.highlightedPen, hotArrowGeometry);
            }

            drawingContext.Pop(); // pop clip

            UpdateToolTip();
        }

        double? DrawSlicingMarker(DrawingContext drawingContext, IEventDataNode slicingMarker, double? lastDrawnSlicingMarkerX)
        {
            var x = TimeAxis.TimeToScreen(slicingMarker.StartTime);
            if (x >= 0 && x <= this.ActualWidth)
            {
                if (!lastDrawnSlicingMarkerX.HasValue || (x > lastDrawnSlicingMarkerX.Value + 3))
                {
                    drawingContext.DrawLine(this.slicingMarkerPen, new Point(x, verticalMargin / 3), new Point(x, this.ActualHeight - verticalMargin + 2));
                    return x;
                }
            }

            return lastDrawnSlicingMarkerX;
        }

        ChunkDrawData DrawChunk(DrawingContext drawingContext, IEventDataNode chunk, Brush brush, Pen pen, ChunkDrawData previousChunkData, bool forceDraw)
        {
            var x1 = TimeAxis.TimeToScreen(chunk.StartTime);
            var x2 = Math.Max(x1, TimeAxis.TimeToScreen(chunk.StartTime + chunk.Duration));  // Max in case the duration is bogus

            if (x2 < 0 || x1 > this.ActualWidth)
            {
                if (previousChunkData != null)
                {
                    drawingContext.DrawRectangle(previousChunkData.Fill, previousChunkData.Pen, previousChunkData.Rect);
                }

                return null;
            }

            var r = new Rect(new Point(x1, verticalMargin), new Size(x2 - x1, this.ActualHeight - (verticalMargin * 2)));
            var halfHeightRect = new Rect(new Point(x1, (this.ActualHeight - halfHeight) / 2), new Size(x2 - x1, halfHeight));
            var style = chunk.Style;

            if (pen == null)
            {
                // Since there is no stroke (pen == null), this is a "normal" chunk with no border
                // (the bar has already been filled w/ border color) so deflate 1/2 pixel to simulate
                // a border stroke.
                r.Inflate(-0.5, -0.5);
            }
            else
            {
                r.Inflate(0, -0.5);
            }

            const int minWidth = 3;

            // Points-in-time (markers) are always 3px wide
            if (style == EventRenderStyle.ParentedMarker)
            {
                r = new Rect(x1 - Math.Floor((double)minWidth / 2), verticalMargin, minWidth, this.ActualHeight - (verticalMargin * 2));
            }

            // Adjust rect height based on chunk nesting depth
            if (chunk.ZIndex > 0 && !r.IsEmpty && style != EventRenderStyle.HalfHeight)
            {
                double delta = chunk.ZIndex * 2;
                r.Inflate(0, -delta);
            }

            if (forceDraw)
            {
                // This is a special (hot or selected) chunk, so enforce a minimum width and always draw it.
                if (style == EventRenderStyle.HalfHeight)
                {
                    r = halfHeightRect;
                }

                var width = r.Width;
                if (width < minWidth)
                {
                    var halfGap = (minWidth - width) / 2;
                    r = new Rect(new Point(r.Left - halfGap, r.Top), new Point(r.Right + halfGap, r.Bottom));
                }
                drawingContext.DrawRectangle(brush, pen, r);
                return null;
            }

            const double minDistance = 16;

            if (style == EventRenderStyle.HalfHeight)
            {
                r = halfHeightRect;
            }

            // Draw the chunk.  We may need to merge it with the previous rect if there is one.
            if (previousChunkData != null)
            {
                // We have a previous rect.  If it's far enough away from this one, then draw it.
                // OR, if this one is big enough BY ITSELF to be drawn, draw the previous one.
                // Otherwise, merge it with this one, and draw the result if it's wide enough.
                if (previousChunkData.Rect.Right < (r.Left - (minDistance / 4)) || (r.Width >= minDistance))
                {
                    drawingContext.DrawRectangle(previousChunkData.Fill, previousChunkData.Pen, previousChunkData.Rect);
                }
                else
                {
                    previousChunkData.Rect = Rect.Union(previousChunkData.Rect, r);
                    previousChunkData.Fill = Brushes.Gray;

                    if (previousChunkData.Rect.Width >= minDistance)
                    {
                        drawingContext.DrawRectangle(previousChunkData.Fill, previousChunkData.Pen, previousChunkData.Rect);
                        previousChunkData = null;
                    }

                    return previousChunkData;
                }
            }

            if (r.Width >= minDistance)
            {
                drawingContext.DrawRectangle(brush, pen, r);
                previousChunkData = null;
            }
            else
            {
                previousChunkData = new ChunkDrawData { Fill = brush, Rect = r, Pen = pen };
            }

            return previousChunkData;
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            IEventDataNode curHotChunk = null;
            double mouseX = e.GetPosition(this).X;
            ulong tick = TimeAxis.ScreenToTime(mouseX);

            // Look through the slicing markers first
            for (int i = 0; i < this.slicingMarkers.Length; i++)
            {
                var chunk = this.slicingMarkers[i];

                // Slicing markers are only a pixel wide, so make them selectable w/ padding
                if (chunk.Visible)
                {
                    var x = TimeAxis.TimeToScreen(chunk.StartTime);
                    if (mouseX > (x - 3) && mouseX < (x + 3))
                    {
                        curHotChunk = chunk;
                        break;
                    }
                }
            }

            if (curHotChunk == null)
            {
                for (int i = this.sortedNodes.Length - 1; i >= 0; i--)
                {
                    var chunk = this.sortedNodes[i];

                    if (chunk.Visible && (tick >= chunk.StartTime) && (tick < chunk.StartTime + chunk.Duration))
                    {
                        curHotChunk = chunk;
                        break;
                    }

                    // Marker events are drawn at special minimum width, so make them selectable w/ padding
                    if (chunk.Visible && chunk.Style == EventRenderStyle.ParentedMarker)
                    {
                        var x = TimeAxis.TimeToScreen(chunk.StartTime);
                        if (mouseX > (x - 3) && mouseX < (x + 3))
                        {
                            curHotChunk = chunk;
                            break;
                        }
                    }
                }
            }

            if (curHotChunk != hotChunk)
            {
                // Hot chunk has changed
                hotChunk = curHotChunk;
                UpdateToolTip();
                InvalidateVisual();
            }

            base.OnMouseMove(e);
        }

        void UpdateToolTip()
        {
            if (hotChunk == null)
            {
                toolTip.IsOpen = false;
            }
            else
            {
                var x1 = Math.Max(0, TimeAxis.TimeToScreen(hotChunk.StartTime));
                var x2 = Math.Min(TimeAxis.ActualWidth, TimeAxis.TimeToScreen(hotChunk.StartTime + hotChunk.Duration));
                var r = new Rect(new Point(x1, 0), new Point(x2, this.ActualHeight - verticalMargin));
                toolTip.PlacementRectangle = r;
                toolTip.Placement = PlacementMode.Bottom;
                toolTip.Content = string.Format(dataSource.ToolTipFormat, hotChunk.StartTime, hotChunk.Duration, hotChunk.Name);
                toolTip.IsOpen = true;
            }
        }

        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            if (hotChunk != null)
            {
                hotChunk = null;
                toolTip.IsOpen = false;
                InvalidateVisual();
            }

            base.OnMouseLeave(e);
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (hotChunk != null)
            {
                SelectChunk(hotChunk);
            }

            this.Focus();
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool right = (e.Key == Key.Right);
            bool left = (e.Key == Key.Left);
            bool up = (e.Key == Key.Up);
            bool down = (e.Key == Key.Down);

            // There might be a better nav model for keyboard multi-select...
            var selectedChunk = this.selectedChunks.FirstOrDefault();

            if (right || left)
            {
                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    // Ctrl + L/R arrows: pan left and right
                    int panScreen = (int)(this.ActualWidth / 8.0);
                    if (right)
                    {
                        panScreen = -panScreen;
                    }
                    TimeAxis.PanByScreenDelta(panScreen);

                    hotChunk = null;
                    UpdateToolTip();
                }
                else if (selectedChunk != null)
                {
                    IEventDataNode newSelectedChunk = null;

                    if (right)
                    {
                        newSelectedChunk = dataSource.NextNode(selectedChunk);
                    }
                    else if (left)
                    {
                        newSelectedChunk = dataSource.PreviousNode(selectedChunk);
                    }
                    // L/R arrows: select next/prev chunk
                    if (newSelectedChunk != selectedChunk && newSelectedChunk != null)
                    {
                        SelectChunk(newSelectedChunk);
                    }
                }
            }
            else if (up || down)
            {
                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    // Ctrl + U/D arrows: zoom in and out
                    if (up)
                    {
                        TimeAxis.ZoomIn();
                    }
                    else
                    {
                        TimeAxis.ZoomOut();
                    }
                }

                e.Handled = true;
            }

            base.OnKeyDown(e);
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

        class ChunkDrawData
        {
            public Rect Rect { get; set; }
            public Pen Pen { get; set; }
            public Brush Fill { get; set; }
            public EventRenderStyle Style { get; set; }
        }

        class DefaultEventDataSelection : IEventDataSelection
        {
            IEventDataSource source;

            public DefaultEventDataSelection(IEventDataSource source)
            {
                this.source = source;
                this.source.SelectionChangedExternal += OnSourceSelectionChangedExternal;
            }

            public IEnumerable<IEventDataNode> SelectedNodes
            {
                get
                {
                    if (source.SelectedNode == null)
                    {
                        yield break;
                    }

                    yield return source.SelectedNode;
                }
            }

            public event EventHandler SelectedNodesChanged;

            void OnSourceSelectionChangedExternal(object sender, EventArgs e)
            {
                var handler = this.SelectedNodesChanged;

                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }
    }
}
