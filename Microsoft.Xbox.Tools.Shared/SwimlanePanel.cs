//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Xbox.Tools.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;

    public class SwimlanePanel : Panel
    {
        public static readonly DependencyProperty TotalComputedHeightProperty = DependencyProperty.Register(
            "TotalComputedHeight", typeof(double), typeof(SwimlanePanel));

        public static readonly DependencyProperty ScrollRangeProperty = DependencyProperty.Register(
            "ScrollRange", typeof(double), typeof(SwimlanePanel));

        public static readonly DependencyProperty VisibleHeightProperty = DependencyProperty.Register(
            "VisibleHeight", typeof(double), typeof(SwimlanePanel));

        public static readonly DependencyProperty ScrollOffsetProperty = DependencyProperty.Register(
            "ScrollOffset", typeof(double), typeof(SwimlanePanel), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsArrange));

        static readonly DependencyPropertyKey sideBarWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            "SideBarWidth", typeof(double), typeof(SwimlanePanel), new FrameworkPropertyMetadata(0d));
        public static readonly DependencyProperty SideBarWidthProperty = sideBarWidthPropertyKey.DependencyProperty;

        public static readonly DependencyProperty MinimumSideBarWidthProperty = DependencyProperty.Register(
            "MinimumSideBarWidth", typeof(double), typeof(SwimlanePanel), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsArrange));

        double sideWidth;
        double maxLaneHeight;
        double totalTopDockHeight;
        double totalBottomDockHeight;
        double totalScrollableHeight;
        List<SwimlaneDefinition> topDockLanes = new List<SwimlaneDefinition>();
        List<SwimlaneDefinition> bottomDockLanes = new List<SwimlaneDefinition>();
        List<SwimlaneDefinition> scrollLanes = new List<SwimlaneDefinition>();
        ClipShield clipShield;

        public SwimlanePanel()
        {
            this.Swimlanes = new ObservableCollection<SwimlaneDefinition>();
            this.Swimlanes.CollectionChanged += OnSwimLaneCollectionChanged;
            this.clipShield = new ClipShield();
            this.Children.Add(this.clipShield);
            Panel.SetZIndex(this.clipShield, 1);
            this.clipShield.SetBinding(ClipShield.BackgroundProperty, new Binding { Source = this, Path = new PropertyPath(BackgroundProperty) });
            this.ClipToBounds = true;
        }

        internal ObservableCollection<SwimlaneDefinition> Swimlanes { get; private set; }

        public double TotalComputedHeight
        {
            get { return (double)GetValue(TotalComputedHeightProperty); }
            set { SetValue(TotalComputedHeightProperty, value); }
        }

        public double ScrollOffset
        {
            get { return (double)GetValue(ScrollOffsetProperty); }
            set { SetValue(ScrollOffsetProperty, value); }
        }

        public double VisibleHeight
        {
            get { return (double)GetValue(VisibleHeightProperty); }
            set { SetValue(VisibleHeightProperty, value); }
        }

        public double ScrollRange
        {
            get { return (double)GetValue(ScrollRangeProperty); }
            set { SetValue(ScrollRangeProperty, value); }
        }

        public double SideBarWidth
        {
            get { return (double)GetValue(SideBarWidthProperty); }
            private set { SetValue(sideBarWidthPropertyKey, value); }
        }

        public double MinimumSideBarWidth
        {
            get { return (double)GetValue(MinimumSideBarWidthProperty); }
            set { SetValue(MinimumSideBarWidthProperty, value); }
        }

        void OnSwimLaneCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count == 1)
            {
                var lane = e.NewItems[0] as SwimlaneDefinition;
                this.Children.Add(lane.LaneElement);
                if (lane.SideElement != null)
                {
                    this.Children.Add(lane.SideElement);
                }
                lane.SetZIndex(0);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.Count == 1)
            {
                var lane = e.OldItems[0] as SwimlaneDefinition;
                this.Children.Remove(lane.LaneElement);
                if (lane.SideElement != null)
                {
                    this.Children.Remove(lane.SideElement);
                }
            }
            else
            {
                this.Children.Clear();
                this.Children.Add(this.clipShield);
                foreach (var lane in this.Swimlanes)
                {
                    this.Children.Add(lane.LaneElement);
                    if (lane.SideElement != null)
                    {
                        this.Children.Add(lane.SideElement);
                    }
                    lane.SetZIndex(0);
                }
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                base.OnMouseWheel(e);
                return;
            }

            if (e.Delta > 0)
            {
                if (this.ScrollOffset > 0)
                {
                    this.ScrollOffset = Math.Max(0, this.ScrollOffset - (e.Delta / 3));
                    e.Handled = true;
                }
            }
            else if (e.Delta < 0)
            {
                if (this.ScrollOffset < this.ScrollRange)
                {
                    this.ScrollOffset = Math.Min(this.ScrollRange, this.ScrollOffset - (e.Delta / 3));
                    e.Handled = true;
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double maxDataWidth = 0;
            bool infiniteHeight = double.IsInfinity(availableSize.Height);
            double previousSideWidth = this.sideWidth;

            this.sideWidth = 0;
            this.maxLaneHeight = 0;
            this.totalScrollableHeight = 0;
            this.totalTopDockHeight = 0;
            this.totalBottomDockHeight = 0;
            this.topDockLanes = this.Swimlanes.Where(l => l.Dock == SwimlaneDock.Top).ToList();
            this.bottomDockLanes = this.Swimlanes.Where(l => l.Dock == SwimlaneDock.Bottom).ToList();
            this.scrollLanes = this.Swimlanes.Where(l => l.Dock == SwimlaneDock.None).ToList();

            this.clipShield.Measure(availableSize);

            foreach (var lane in this.topDockLanes)
            {
                lane.Measure(availableSize, previousSideWidth);
                lane.SetZIndex(2);
                this.totalTopDockHeight += lane.ComputedHeight;
                this.maxLaneHeight = Math.Max(this.maxLaneHeight, lane.ComputedHeight);
                this.sideWidth = Math.Max(this.sideWidth, lane.SideWidth);
                maxDataWidth = Math.Max(maxDataWidth, lane.LaneElement.DesiredSize.Width);
            }

            foreach (var lane in this.bottomDockLanes)
            {
                lane.Measure(availableSize, previousSideWidth);
                lane.SetZIndex(2);
                this.totalBottomDockHeight += lane.ComputedHeight;
                this.maxLaneHeight = Math.Max(this.maxLaneHeight, lane.ComputedHeight);
                this.sideWidth = Math.Max(this.sideWidth, lane.SideWidth);
                maxDataWidth = Math.Max(maxDataWidth, lane.LaneElement.DesiredSize.Width);
            }

            double totalStarCount = this.scrollLanes.Where(l => l.Height.IsStar).Sum(l => l.Height.Value);

            if (infiniteHeight || (totalStarCount == 0))
            {
                // No star-sizing logic needed
                foreach (var lane in this.scrollLanes)
                {
                    lane.Measure(availableSize, previousSideWidth);
                    lane.SetZIndex(0);
                    this.totalScrollableHeight += lane.ComputedHeight;
                    this.maxLaneHeight = Math.Max(this.maxLaneHeight, lane.ComputedHeight);
                    this.sideWidth = Math.Max(this.sideWidth, lane.SideWidth);
                    maxDataWidth = Math.Max(maxDataWidth, lane.LaneElement.DesiredSize.Width);
                }
            }
            else
            {
                // Need a two-pass measure, first one for non-star-sized lanes
                foreach (var lane in this.scrollLanes.Where(l => !l.Height.IsStar))
                {
                    lane.Measure(availableSize, previousSideWidth);
                    lane.SetZIndex(0);
                    this.totalScrollableHeight += lane.ComputedHeight;
                    this.maxLaneHeight = Math.Max(this.maxLaneHeight, lane.ComputedHeight);
                    this.sideWidth = Math.Max(this.sideWidth, lane.SideWidth);
                    maxDataWidth = Math.Max(maxDataWidth, lane.LaneElement.DesiredSize.Width);
                }

                // ...and next one for star-sized lanes, who get percentages of the remaining space.
                double remainingSpace = Math.Max(0, availableSize.Height - (this.totalTopDockHeight + this.totalBottomDockHeight + this.totalScrollableHeight));

                foreach (var lane in this.scrollLanes.Where(l => l.Height.IsStar))
                {
                    var computedHeight = Math.Max(remainingSpace * (lane.Height.Value / totalStarCount), lane.MinHeight);
                    lane.Measure(new Size(availableSize.Width, computedHeight), previousSideWidth);
                    lane.ComputedHeight = computedHeight;       // Must set after Measure(), because Measure() whacks this
                    remainingSpace = Math.Max(0, remainingSpace - lane.ComputedHeight);
                    totalStarCount -= lane.Height.Value;
                    lane.SetZIndex(0);
                    this.totalScrollableHeight += lane.ComputedHeight;
                    this.maxLaneHeight = Math.Max(this.maxLaneHeight, lane.ComputedHeight);
                    this.sideWidth = Math.Max(this.sideWidth, lane.SideWidth);
                    maxDataWidth = Math.Max(maxDataWidth, lane.LaneElement.DesiredSize.Width);
                }
            }

            this.sideWidth = Math.Max(this.sideWidth, this.MinimumSideBarWidth);

            // At this point, all (main) lanes have been measured accounting for the previous side width.  If the side width didn't change 
            // size, then those measurements are valid.  Otherwise, we need to re-measure all lanes with the correct width, so that they layout correctly.
            if (this.sideWidth != previousSideWidth)
            {
                foreach (var lane in this.scrollLanes)
                {
                    var oldComputedHeight = lane.ComputedHeight;

                    lane.Measure(new Size(availableSize.Width, lane.ComputedHeight), this.sideWidth);

                    // Measure whacks this value, but it can't change here -- must stay fixed  for the second measure.
                    lane.ComputedHeight = oldComputedHeight;
                }
            }

            if (double.IsInfinity(availableSize.Width))
                availableSize.Width = (this.sideWidth + maxDataWidth);

            if (double.IsInfinity(availableSize.Height))
                availableSize.Height = this.totalTopDockHeight + this.totalBottomDockHeight + this.totalScrollableHeight;

            this.SideBarWidth = this.sideWidth;
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double runningTopHeight = 0;
            double dataWidth = Math.Max(0, finalSize.Width - this.sideWidth);

            while (finalSize.Height - (this.totalTopDockHeight + this.totalBottomDockHeight) < this.maxLaneHeight)
            {
                if (this.bottomDockLanes.Count > 0)
                {
                    var lane = this.bottomDockLanes[0];
                    this.scrollLanes.Add(lane);
                    lane.SetZIndex(0);
                    this.bottomDockLanes.RemoveAt(0);
                    this.totalBottomDockHeight -= lane.ComputedHeight;
                    this.totalScrollableHeight += lane.ComputedHeight;
                }
                else if (this.topDockLanes.Count > 0)
                {
                    var lane = this.topDockLanes[this.topDockLanes.Count - 1];
                    this.scrollLanes.Insert(0, lane);
                    lane.SetZIndex(0);
                    this.topDockLanes.RemoveAt(this.topDockLanes.Count - 1);
                    this.totalTopDockHeight -= lane.ComputedHeight;
                    this.totalScrollableHeight += lane.ComputedHeight;
                }
                else
                {
                    break;
                }
            }

            this.VisibleHeight = Math.Min(this.totalScrollableHeight, finalSize.Height - (this.totalTopDockHeight + this.totalBottomDockHeight));
            this.ScrollRange = Math.Max(0, this.totalScrollableHeight - this.VisibleHeight);
            this.ScrollOffset = Math.Max(0, Math.Min(this.ScrollOffset, this.ScrollRange));

            foreach (var lane in this.topDockLanes)
            {
                lane.Arrange(this.sideWidth, dataWidth, runningTopHeight);
                runningTopHeight += lane.ComputedHeight;
            }

            foreach (var lane in this.scrollLanes)
            {
                lane.Arrange(this.sideWidth, dataWidth, runningTopHeight - this.ScrollOffset);
                runningTopHeight += lane.ComputedHeight;
            }

            runningTopHeight = Math.Min(runningTopHeight, finalSize.Height - this.totalBottomDockHeight);
            foreach (var lane in this.bottomDockLanes)
            {
                lane.Arrange(this.sideWidth, dataWidth, runningTopHeight);
                runningTopHeight += lane.ComputedHeight;
            }

            this.TotalComputedHeight = runningTopHeight;
            this.clipShield.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            this.clipShield.TopEdge = (this.totalTopDockHeight > 0) ? this.totalTopDockHeight : double.NaN;
            this.clipShield.BottomEdge = (this.totalBottomDockHeight > 0) ? this.TotalComputedHeight - this.totalBottomDockHeight : double.NaN;

            return finalSize;
        }

        static void OnLaneChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var newLane = e.NewValue as SwimlaneDefinition;

            if (newLane != null)
            {
                int zindex = 0;

                if (newLane.Dock != SwimlaneDock.None)
                {
                    zindex = 1;
                }

                Panel.SetZIndex(newLane.LaneElement, zindex);
                if (newLane.SideElement != null)
                {
                    Panel.SetZIndex(newLane.SideElement, zindex);
                }
            }
        }

        internal class ClipShield : FrameworkElement
        {
            public static readonly DependencyProperty TopEdgeProperty = DependencyProperty.Register(
                "TopEdge", typeof(double), typeof(ClipShield), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

            public static readonly DependencyProperty BottomEdgeProperty = DependencyProperty.Register(
                "BottomEdge", typeof(double), typeof(ClipShield), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

            public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
                "Background", typeof(Brush), typeof(ClipShield), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

            public double TopEdge
            {
                get { return (double)GetValue(TopEdgeProperty); }
                set { SetValue(TopEdgeProperty, value); }
            }

            public double BottomEdge
            {
                get { return (double)GetValue(BottomEdgeProperty); }
                set { SetValue(BottomEdgeProperty, value); }
            }

            public Brush Background
            {
                get { return (Brush)GetValue(BackgroundProperty); }
                set { SetValue(BackgroundProperty, value); }
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                var brush = this.Background;

                if (brush == null || brush == Brushes.Transparent)
                {
                    brush = Brushes.White;
                }

                if (!double.IsNaN(this.TopEdge))
                {
                    drawingContext.DrawRectangle(brush, null, new Rect(0, 0, this.ActualWidth, this.TopEdge));
                }

                if (!double.IsNaN(this.BottomEdge))
                {
                    drawingContext.DrawRectangle(brush, null, new Rect(0, this.BottomEdge, this.ActualWidth, Math.Max(0, this.ActualHeight - this.BottomEdge)));
                }
            }
        }

        internal enum SwimlaneDock
        {
            None,
            Top,
            Bottom,
        }

        internal class SwimlaneDefinition
        {
            public FrameworkElement LaneElement;
            public FrameworkElement SideElement;
            public GridLength Height;
            public SwimlaneDock Dock;
            public double ComputedHeight;
            public double SideWidth;
            public bool Topmost;

            public double MinHeight
            {
                get
                {
                    if (this.SideElement != null)
                    {
                        return Math.Max(this.SideElement.MinHeight, this.LaneElement.MinHeight);
                    }

                    return this.LaneElement.MinHeight;
                }
            }

            public void Measure(Size availableSize, double sideWidth)
            {
                this.LaneElement.Measure(new Size(Math.Max(availableSize.Width - sideWidth, 0), double.PositiveInfinity));
                if (this.SideElement != null)
                {
                    this.SideElement.Measure(availableSize);
                    this.ComputedHeight = Math.Max(this.LaneElement.DesiredSize.Height, this.SideElement.DesiredSize.Height);
                    this.SideWidth = this.SideElement.DesiredSize.Width;
                }
                else
                {
                    this.ComputedHeight = this.LaneElement.DesiredSize.Height;
                    this.SideWidth = 0;
                }
            }

            public void Arrange(double sideWidth, double dataWidth, double top)
            {
                this.LaneElement.Arrange(new Rect(sideWidth, top, dataWidth, this.ComputedHeight));
                if (this.SideElement != null)
                {
                    this.SideElement.Arrange(new Rect(0, top, sideWidth, this.ComputedHeight));
                }
            }

            public void SetZIndex(int index)
            {
                if (this.Topmost)
                {
                    index = 3;
                }

                Panel.SetZIndex(this.LaneElement, index);
                if (this.SideElement != null)
                {
                    Panel.SetZIndex(this.SideElement, index);
                }
            }
        }
    }
}
