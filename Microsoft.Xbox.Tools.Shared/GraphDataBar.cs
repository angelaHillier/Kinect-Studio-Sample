//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Xbox.Tools.Shared
{
    public class GraphDataBar : DataBar
    {
        public static readonly DependencyProperty SquarePeaksProperty = DependencyProperty.Register(
            "SquarePeaks", typeof(bool), typeof(GraphDataBar), new FrameworkPropertyMetadata(true, OnSquarePeaksChanged));

        public static readonly DependencyProperty HighlightRangeStartProperty = DependencyProperty.Register(
            "HighlightRangeStart", typeof(ulong), typeof(GraphDataBar), new FrameworkPropertyMetadata(ulong.MaxValue));

        public static readonly DependencyProperty HighlightRangeStopProperty = DependencyProperty.Register(
            "HighlightRangeStop", typeof(ulong), typeof(GraphDataBar), new FrameworkPropertyMetadata(ulong.MaxValue));

        public static readonly DependencyProperty DataRangeStopProperty = DependencyProperty.Register(
            "DataRangeStop", typeof(ulong), typeof(GraphDataBar));

        public static readonly DependencyProperty MinimumGraphTimeProperty = DependencyProperty.Register(
            "MinimumGraphTime", typeof(ulong), typeof(GraphDataBar), new FrameworkPropertyMetadata(10ul * 1000ul * 1000ul * 1000ul));

        public static readonly DependencyProperty DefaultYRangeProperty = DependencyProperty.Register(
            "DefaultYRange", typeof(double), typeof(GraphDataBar), new FrameworkPropertyMetadata((double)100.0f));


        internal static PenData[] graphDataPens = 
        {
            new PenData { Brush = Brushes.Red       , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Green     , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Blue      , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Brown     , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Orange    , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Cyan      , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Magenta   , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.CadetBlue , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Purple    , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Pink      , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Salmon    , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Maroon    , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.LimeGreen , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.LightBlue , DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.LightCoral, DashStyle = DashStyles.Solid },
            new PenData { Brush = Brushes.Gold      , DashStyle = DashStyles.Solid },
        };

        public static readonly int MaxDataSources = graphDataPens.Length;

        GraphGrid graphGrid;
        Canvas canvas;
        ObservableCollection<GraphDataProvider> graphDataProviders = new ObservableCollection<GraphDataProvider>();
        ReadOnlyObservableCollection<GraphDataProvider> readOnlyProviders;
        HashSet<PenData> usedPens = new HashSet<PenData>();
        ulong timeStart;
        ulong timeEnd;

        Dictionary<GraphDataProvider, GraphShape> shapeTable = new Dictionary<GraphDataProvider, GraphShape>();

        IPausePointSource pausePointSource;
        GraphShape pausePointShape = new GraphShape();

        public GraphDataBar()
        {
            this.SideBar = new GraphDataSideBar(this);
            this.SideBar.VerticalAlignment = VerticalAlignment.Top;
            this.SideBar.YAxisChanged += OnYAxisChanged;
            this.SideBar.YAxisDefaultRequested += OnYAxisDefaultRequested;
            this.readOnlyProviders = new ReadOnlyObservableCollection<GraphDataProvider>(graphDataProviders);
            CollectionViewSource.GetDefaultView(readOnlyProviders).Filter = (item =>
                {
                    GraphDataProvider gdp = item as GraphDataProvider;
                    return !(gdp.DataSource is IHiddenGraphDataSource);
                });
            this.timeStart = 0;
            this.DataRangeStop = 0;
            this.timeEnd = this.MinimumGraphTime;
            InitPausePointShape();
        }

        public bool SquarePeaks
        {
            get { return (bool)GetValue(SquarePeaksProperty); }
            set { SetValue(SquarePeaksProperty, value); }
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

        public ulong DataRangeStop
        {
            get { return (ulong)GetValue(DataRangeStopProperty); }
            set { SetValue(DataRangeStopProperty, value); }
        }

        public ulong MinimumGraphTime
        {
            get { return (ulong)GetValue(MinimumGraphTimeProperty); }
            set { SetValue(MinimumGraphTimeProperty, value); }
        }

        public double DefaultYRange
        {
            get { return (double)GetValue(DefaultYRangeProperty); }
            set { SetValue(DefaultYRangeProperty, value); }
        }

        public override ulong TimeStart { get { return this.timeStart; } }
        public override ulong TimeEnd { get { return this.timeEnd; } }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.graphGrid = this.Template.FindName("PART_GraphGrid", this) as GraphGrid;
            this.canvas = this.Template.FindName("PART_Canvas", this) as Canvas;
            Debug.Assert(this.graphGrid != null && this.canvas != null, "Template for GraphDataBar does not have a GraphGrid (named PART_GraphGrid) and/or a Canvas (named PART_Canvas) in it!");
            if (this.graphGrid != null && this.canvas != null)
            {
                this.graphGrid.SideBar = this.SideBar;
                this.SideBar.SetBinding(HeightProperty, new Binding { Source = this.graphGrid, Path = new PropertyPath(GraphGrid.ActualHeightProperty) });
                this.SideBar.SetBinding(GraphDataSideBar.ForegroundProperty, new Binding { Source = this.graphGrid, Path = new PropertyPath(GraphGrid.ForegroundProperty) });
                this.SideBar.SetBinding(GraphDataSideBar.BackgroundProperty, new Binding { Source = this.graphGrid, Path = new PropertyPath(GraphGrid.BackgroundProperty) });

                this.SideBar.SetRange(0, this.DefaultYRange);

                this.graphGrid.SetBinding(GraphGrid.HighlightRangeStartProperty, new Binding { Source = this, Path = new PropertyPath(HighlightRangeStartProperty) });

                var multiBinding = new MultiBinding { Converter = new HighlightStopConverter() };
                multiBinding.Bindings.Add(new Binding { Source = this, Path = new PropertyPath(HighlightRangeStopProperty) });
                multiBinding.Bindings.Add(new Binding { Source = this, Path = new PropertyPath(DataRangeStopProperty) });
                this.graphGrid.SetBinding(GraphGrid.HighlightRangeStopProperty, multiBinding);

                this.graphGrid.SizeChanged += OnYAxisChanged;

                UpdateRangeValues();

                foreach (var shape in this.shapeTable.Values)
                {
                    this.canvas.Children.Add(shape);
                }

                this.canvas.Children.Add(this.pausePointShape);
            }
        }

        public GraphDataSideBar SideBar { get; private set; }
        public ReadOnlyObservableCollection<GraphDataProvider> DataProviders { get { return this.readOnlyProviders; } }

        public override IEnumerable<DataBarClipSpan> SelectionClipSpans
        {
            get
            {
                if (this.graphGrid == null)
                    yield break;    // Can't return any clips until our template is realized

                yield return new DataBarClipSpan() { Top = this.graphGrid.ActualHeight, Height = this.ActualHeight - this.graphGrid.ActualHeight };
            }
        }

        public void Reset()
        {
            this.graphDataProviders.Clear();
            SetPausePointSource(null);
            this.SideBar.SetRange(0, 1);
        }

        public void SetPausePointSource(IPausePointSource source)
        {
            if (this.pausePointSource != null)
            {
                this.pausePointSource.DataChanged -= OnPausePointSourceDataChanged;
            }

            this.pausePointSource = source;

            if (this.pausePointSource != null)
            {
                this.pausePointSource.DataChanged += OnPausePointSourceDataChanged;
            }

            RebuildPausePointGraph();
        }

        void InitPausePointShape()
        {
            const int d = 10;

            var cross = new PathGeometry();
            cross.Figures.Add(new PathFigure(new Point(0, 0), new LineSegment[1] { new LineSegment(new Point(d, d), true) }, false));

            var hatchCanvas = new Canvas();
            hatchCanvas.Children.Add(new Rectangle { Fill = Brushes.Azure, Width = d, Height = d });
            hatchCanvas.Children.Add(new Path { Stroke = Brushes.Purple, Data = cross });

            this.pausePointShape.Stroke = Brushes.Gray;
            this.pausePointShape.StrokeThickness = 1;
            this.pausePointShape.Fill = new VisualBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, d, d),
                ViewportUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, d, d),
                ViewboxUnits = BrushMappingMode.Absolute,
                Visual = hatchCanvas,
            };
            this.pausePointShape.SetValue(Canvas.ZIndexProperty, 100);
        }

        void RebuildPausePointGraph()
        {
            this.pausePointShape.PathGeometry.Clear();

            if (null != this.pausePointSource && null != this.graphGrid)
            {
                double height = this.graphGrid.ActualHeight;
                foreach (var pp in this.pausePointSource.PausePoints)
                {
                    if (pp.Duration > 0)
                    {
                        double x = this.TimeAxis.TimeToScreen(pp.StartTime);
                        var pathFigure = new PathFigure { StartPoint = new Point(x, 1), IsClosed = true };
                        pathFigure.Segments.Add(new LineSegment(new Point(x, height - 1), true));
                        pathFigure.Segments.Add(new LineSegment(new Point(x + 10, height - 1), false));
                        pathFigure.Segments.Add(new LineSegment(new Point(x + 10, 1), true));
                        pathFigure.Segments.Add(new LineSegment(new Point(x, 1), false));
                        this.pausePointShape.PathGeometry.Figures.Add(pathFigure);
                    }
                }
            }
        }

        void OnPausePointSourceDataChanged(object sender, EventArgs e)
        {
            RebuildPausePointGraph();
        }

        public bool AddDataSource(IGraphDataSource graphDataSource)
        {
            if (graphDataProviders.Count == MaxDataSources)
            {
                // Maximum number of data sources has already been reached
                return false;
            }

            var provider = new GraphDataProvider { DataSource = graphDataSource, IsGraphed = true };
            provider.PenData = graphDataPens.FirstOrDefault(p => !usedPens.Contains(p));
            if (!(graphDataSource is IHiddenGraphDataSource))
            {
                this.usedPens.Add(provider.PenData);
            }
            this.graphDataProviders.Add(provider);
            provider.DataChanged += OnProviderDataChanged;

            var shape = new GraphShape { Stroke = provider.PenData.Brush, StrokeDashCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };

            shape.SetBinding(Shape.StrokeThicknessProperty, new Binding { Source = provider, Path = new PropertyPath(GraphDataProvider.StrokeThicknessProperty) });
            shape.SetBinding(Shape.StrokeDashArrayProperty, new Binding { Source = provider, Path = new PropertyPath(GraphDataProvider.StrokeDashArrayProperty) });
            shape.SetBinding(Shape.FillProperty, new Binding { Source = provider, Path = new PropertyPath(GraphDataProvider.FillProperty) });
            shape.SetBinding(Panel.ZIndexProperty, new Binding { Source = provider, Path = new PropertyPath(GraphDataProvider.ZIndexProperty) });

            this.shapeTable.Add(provider, shape);

            if (this.canvas != null)
            {
                this.canvas.Children.Add(shape);
            }

            OnProviderDataChanged(provider, EventArgs.Empty);

            return true;
        }

        public void RemoveDataSource(IGraphDataSource graphDataSource)
        {
            var provider = this.graphDataProviders.FirstOrDefault(s => s.DataSource == graphDataSource);

            if (provider == null)
                return;

            provider.DataChanged -= OnProviderDataChanged;
            this.graphDataProviders.Remove(provider);
            this.usedPens.Remove(provider.PenData);

            // If we still have providers, recompute the Y axis range, scanning all sources, since the range
            // may now be way too big.
            // If there are now no providers, just leave the Y axis alone.
            if (graphDataProviders.Count > 0 && !this.SideBar.YAxisChangedByUser)
            {
                RecomputeYAxisRange();
            }

            if (this.graphGrid != null)
            {
                this.graphGrid.InvalidateVisual();
            }

            if (this.canvas != null)
            {
                this.canvas.Children.Remove(this.shapeTable[provider]);
            }

            this.shapeTable.Remove(provider);

            if (this.graphDataProviders.Count == 0)
            {
                this.timeEnd = MinimumGraphTime;
                this.timeStart = 0;
                this.DataRangeStop = 0;
                RaiseTimeRangeChanged();
            }
        }

        void RecomputeYAxisRange()
        {
            double yMin = 0;
            double yMax = 1;

            foreach (var s in graphDataProviders)
            {
                yMin = Math.Min(yMin, s.DataSource.MinY);
                yMax = Math.Max(yMax, s.DataSource.MaxY);
            }

            this.SideBar.SetRange(yMin, yMax);
        }

        class ExportNode
        {
            public IEnumerator<IGraphDataNode> Nodes;
            public IGraphDataNode Current;
            public string Name;
        }

        public string ExportData()
        {
            var sb = new StringBuilder();
            var timeline = this.FindParent<Timeline>();
            ulong startTime, endTime;

            if (timeline == null || !timeline.GetSelectionRange(out startTime, out endTime))
            {
                startTime = this.TimeAxis.AbsoluteTimeStart;
                endTime = this.TimeAxis.AbsoluteTimeEnd;
            }

            var data = this.graphDataProviders.Where(gdp => !(gdp .DataSource is IHiddenGraphDataSource)).
                                               Select(gdp => new ExportNode { Nodes = gdp.DataSource.Nodes.GetEnumerator(), Current = null, Name = gdp.DataSource.Name }).ToArray();

            sb.AppendLine("Time\t" + string.Join("\t", data.Select(d => d.Name)));

            // Start each node enumerator, zooming up to start time
            bool dataRemaining = false;

            foreach (var d in data)
            {
                while (d.Nodes.MoveNext())
                {
                    if (d.Nodes.Current.StartTime >= startTime)
                    {
                        d.Current = d.Nodes.Current;
                        dataRemaining = true;
                        break;
                    }
                }
            }

            while (dataRemaining)
            {
                ulong time = data.Where(d => d.Current != null).Min(d => d.Current.StartTime);

                if (time > endTime)
                    break;

                if (time > 0)
                {
                    sb.AppendLine(time.ToString() + "\t" + string.Join("\t", data.Select(d => (d.Current != null && d.Current.StartTime == time) ? d.Current.Y.ToString() : string.Empty).ToArray()));
                }

                dataRemaining = false;
                foreach (var d in data)
                {
                    if (d.Current != null && d.Current.StartTime == time)
                    {
                        d.Current = d.Nodes.MoveNext() ? d.Nodes.Current : null;
                    }

                    if (d.Current != null)
                    {
                        dataRemaining = true;
                    }
                }
            }

            return sb.ToString();
        }

        void OnYAxisChanged(object sender, EventArgs e)
        {
            RedrawForVerticalScaleChange();
            RaiseSelectionClipSpansChanged();
        }

        void OnYAxisDefaultRequested(object sender,  EventArgs e)
        {
            RecomputeYAxisRange();
        }

        void RedrawForVerticalScaleChange()
        {
            UpdateAllGraphs();

            if (this.graphGrid != null)
            {
                // Since we're listening to the side bar, no need to make the graph grid do it...
                this.graphGrid.InvalidateVisual();
            }
        }

        public override void OnVisibleRangeChanged()
        {
            if (this.graphGrid != null)
            {
                this.graphGrid.InvalidateVisual();
                UpdateAllGraphs();
            }
        }

        void OnProviderDataChanged(object sender, EventArgs e)
        {
            var provider = sender as GraphDataProvider;

            if (this.graphGrid != null && provider != null)
            {
                UpdateRangeValues();
                UpdateGraph(provider);
            }
        }

        void UpdateRangeValues()
        {
            if (this.DataProviders.Count == 0)
            {
                return;
            }

            ulong newTimeStart = ulong.MaxValue;
            ulong newTimeEnd = ulong.MinValue;
            double yMin = 0;
            double yMax = this.DefaultYRange;

            foreach (var provider in this.DataProviders)
            {
                var source = provider.DataSource;

                if (source.MinX < newTimeStart)
                {
                    newTimeStart = source.MinX;
                }
                if (source.MaxX > newTimeEnd)
                {
                    newTimeEnd = source.MaxX;
                }
                if (source.MinY < yMin)
                {
                    yMin = source.MinY;
                }
                if (source.MaxY > yMax)
                {
                    yMax = source.MaxY;
                }
            }

            if (newTimeStart == ulong.MaxValue || newTimeEnd == ulong.MinValue)
            {
                // We're empty.  Show ten seconds of data anyway.
                newTimeStart = 0;
                this.DataRangeStop = 0;
                newTimeEnd = MinimumGraphTime;      // 10 seconds
            }
            else
            {
                this.DataRangeStop = newTimeEnd;

                if (newTimeEnd - newTimeStart < MinimumGraphTime)
                {
                    newTimeEnd = newTimeStart + MinimumGraphTime;
                }
            }

            double curYMin;
            double curYMax;

            this.SideBar.GetRange(out curYMin, out curYMax);

            if (this.timeStart != newTimeStart || this.timeEnd != newTimeEnd)
            {
                this.timeStart = newTimeStart;
                this.timeEnd = newTimeEnd;
                RaiseTimeRangeChanged();
            }

            if (!this.SideBar.YAxisChangedByUser && (curYMin != yMin || curYMax != yMax))
            {
                this.SideBar.SetRange(yMin, yMax);
            }
        }

        void UpdateAllGraphs()
        {
            if (this.graphGrid != null)
            {
                foreach (var provider in this.DataProviders)
                {
                    UpdateGraph(provider);
                }

                RebuildPausePointGraph();
            }
        }

        void UpdateGraph(GraphDataProvider provider)
        {
            var shape = this.shapeTable[provider];
            RebuildGraphGeometry(provider, shape.PathGeometry);
            shape.InvalidateVisual();
        }

        void RebuildGraphGeometry(GraphDataProvider provider, PathGeometry geometry)
        {
            ulong startTime = this.TimeAxis.VisibleTimeStart;
            ulong endTime = this.TimeAxis.VisibleTimeEnd;
            ulong granularity;

            // "granularity" is the time period at which nodes start to disappear.  The graph data provider
            // will return at most 3 nodes in one granularity unit of time (min, max, last).
            granularity = (ulong)((endTime - startTime) / (this.graphGrid.ActualWidth / 12));

            var pathFigure = new PathFigure { IsClosed = true };
            var nodes = provider.GetNodesInTimespan(startTime, endTime, granularity);
            double height = this.graphGrid.ActualHeight;
            double x = 0, lastY = 0;
            bool first = true;

            foreach (var node in nodes)
            {
                x = this.TimeAxis.TimeToScreen(node.StartTime);
                double y = height - this.SideBar.YToPixels(node.Y);

                if (first)
                {
                    pathFigure.StartPoint = new Point(x, height);
                    pathFigure.Segments.Add(new LineSegment(new Point(x, y), false));
                    first = false;
                }
                else
                {
                    if (this.SquarePeaks)
                    {
                        pathFigure.Segments.Add(new LineSegment(new Point(x, lastY), true));
                    }

                    pathFigure.Segments.Add(new LineSegment(new Point(x, y), true));
                }

                lastY = y;
            }

            geometry.Figures.Clear();

            if (first)
            {
                // No data in this time window
                return;
            }

            pathFigure.Segments.Add(new LineSegment(new Point(x, height), false));
            geometry.Figures.Add(pathFigure);
        }

        static void OnSquarePeaksChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            GraphDataBar bar = obj as GraphDataBar;

            if (bar != null)
            {
                bar.UpdateAllGraphs();
            }
        }

        public class PenData
        {
            public SolidColorBrush Brush { get; set; }
            public DashStyle DashStyle { get; set; }

            public PenData()
            {
                this.DashStyle = DashStyles.Solid;
            }

            public Pen CreatePen(double thickness)
            {
                return new Pen(this.Brush, thickness) { DashStyle = this.DashStyle, DashCap = PenLineCap.Round };
            }
        }

        public class GraphShape : Shape
        {
            public GraphShape()
            {
                this.PathGeometry = new PathGeometry();
            }

            public PathGeometry PathGeometry { get; private set; }

            protected override Geometry DefiningGeometry
            {
                get { return this.PathGeometry; }
            }
        }

        class HighlightStopConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (values.Length != 2 || !(values[0] is ulong) || !(values[1] is ulong))
                {
                    return Binding.DoNothing;
                }

                var highlightStop = (ulong)values[0];
                var dataStop = (ulong)values[1];

                if (highlightStop != ulong.MaxValue)
                {
                    return highlightStop;
                }

                return dataStop;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

    }
}
