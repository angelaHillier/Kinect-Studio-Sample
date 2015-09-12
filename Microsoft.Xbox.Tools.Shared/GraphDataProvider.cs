//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared
{
    public class GraphDataProvider : DependencyObject
    {
        public static readonly DependencyProperty DataSourceProperty = DependencyProperty.Register(
            "DataSource", typeof(IGraphDataSource), typeof(GraphDataProvider), new FrameworkPropertyMetadata(OnDataSourceChanged));

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof(bool), typeof(GraphDataProvider), new FrameworkPropertyMetadata(OnIsSelectedChanged));

        public static readonly DependencyProperty IsGraphedProperty = DependencyProperty.Register(
            "IsGraphed", typeof(bool), typeof(GraphDataProvider));

        public static readonly DependencyProperty ZIndexProperty = DependencyProperty.Register(
            "ZIndex", typeof(int), typeof(GraphDataProvider));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke", typeof(Brush), typeof(GraphDataProvider));

        public static readonly DependencyProperty StrokeDashArrayProperty = DependencyProperty.Register(
            "StrokeDashArray", typeof(DoubleCollection), typeof(GraphDataProvider));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            "StrokeThickness", typeof(double), typeof(GraphDataProvider));

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            "Fill", typeof(Brush), typeof(GraphDataProvider));

        public static readonly DependencyProperty OpacityProperty = DependencyProperty.Register(
            "Opacity", typeof(double), typeof(GraphDataProvider), new FrameworkPropertyMetadata(0.2d));

        static int nextZIndex = 0;

        public IGraphDataSource DataSource
        {
            get { return (IGraphDataSource)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public double Opacity
        {
            get { return (double)GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        public DoubleCollection StrokeDashArray
        {
            get { return (DoubleCollection)GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
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

        public bool IsGraphed
        {
            get { return (bool)GetValue(IsGraphedProperty); }
            set { SetValue(IsGraphedProperty, value); }
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public int ZIndex
        {
            get { return (int)GetValue(ZIndexProperty); }
            set { SetValue(ZIndexProperty, value); }
        }

        GraphDataBar.PenData penData;

        public GraphDataBar.PenData PenData
        {
            get
            {
                return this.penData;
            }
            set
            {
                this.penData = value;
                SetRenderValues();
                this.StrokeDashArray = this.penData.DashStyle.Dashes;
                this.Stroke = this.penData.Brush;
            }
        }

        public event EventHandler DataChanged;

        public IEnumerable<IGraphDataNode> GetNodesInTimespan(ulong startTime, ulong endTime, ulong granularity)
        {
            if (this.DataSource == null)
                yield break;

            IGraphDataNode firstNode = null;

            foreach (var node in SampleNodes(granularity))
            {
                if (node.StartTime >= endTime)
                {
                    if (firstNode != null)
                    {
                        yield return firstNode;
                    }

                    yield return node;
                    yield break;
                }

                if (node.StartTime < startTime)
                {
                    firstNode = node;
                    continue;
                }

                if (firstNode != null)
                {
                    yield return firstNode;
                    firstNode = null;
                }

                yield return node;
            }
        }

        IEnumerable<IGraphDataNode> SampleNodes(ulong granularity)
        {
            IGraphDataNode lastSampledNode = null;
            IGraphDataNode finalNode = null;
            IGraphDataNode minNode = null;
            IGraphDataNode maxNode = null;

            foreach (var node in this.DataSource.Nodes)
            {
                if (lastSampledNode != null && node.StartTime - lastSampledNode.StartTime < granularity)
                {
                    finalNode = node;
                    if (maxNode == null || node.Y > maxNode.Y)
                    {
                        maxNode = node;
                    }
                    if (minNode == null || node.Y < minNode.Y)
                    {
                        minNode = node;
                    }
                    continue;
                }

                foreach (var minMaxNode in MinMaxFinalNodesInOrder(minNode, maxNode, node))
                {
                    yield return minMaxNode;
                }

                lastSampledNode = node;
                finalNode = null;
                minNode = null;
                maxNode = null;
            }

            foreach (var minMaxNode in MinMaxFinalNodesInOrder(minNode, maxNode, finalNode))
            {
                yield return minMaxNode;
            }
        }

        IEnumerable<IGraphDataNode> MinMaxFinalNodesInOrder(IGraphDataNode minNode, IGraphDataNode maxNode, IGraphDataNode finalNode)
        {
            if (minNode != null && maxNode != null && minNode != maxNode)
            {
                if (minNode.StartTime < maxNode.StartTime)
                {
                    yield return minNode;
                    yield return maxNode;
                }
                else
                {
                    yield return maxNode;
                    yield return minNode;
                }
            }
            else if (minNode != null)
            {
                yield return minNode;
            }
            else if (maxNode != null)
            {
                yield return maxNode;
            }

            if (finalNode != null)
            {
                yield return finalNode;
            }
        }

        void OnDataChanged(object sender, EventArgs e)
        {
            var handler = this.DataChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void SetRenderValues()
        {
            if (this.IsSelected)
            {
                this.StrokeThickness = 2;
                this.Fill = new SolidColorBrush(this.PenData.Brush.Color);
                BindingOperations.SetBinding(this.Fill, SolidColorBrush.OpacityProperty, Theme.CreateBinding("ConsoleViewGraphFillOpacity"));
            }
            else
            {
                this.StrokeThickness = 1;
                this.Fill = null;
            }
        }

        static void OnIsSelectedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            GraphDataProvider provider = obj as GraphDataProvider;

            if (provider != null)
            {
                if (provider.IsSelected)
                {
                    provider.ZIndex = ++nextZIndex;
                }

                provider.SetRenderValues();
            }
        }

        static void OnDataSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            GraphDataProvider provider = obj as GraphDataProvider;

            if (provider != null)
            {
                var oldSource = e.OldValue as IGraphDataSource;

                if (oldSource != null)
                {
                    oldSource.DataChanged -= provider.OnDataChanged;
                }

                var newSource = e.NewValue as IGraphDataSource;

                if (newSource != null)
                {
                    newSource.DataChanged += provider.OnDataChanged;
                }
            }
        }

        public override string ToString()
        {
            if (this.DataSource != null)
            {
                // ToString is used by the ListBox on each data item for type-ahead matching.
                return this.DataSource.Name;
            }

            return base.ToString();
        }
    }
}
