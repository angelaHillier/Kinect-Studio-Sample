//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Xbox.Tools.Shared
{
    public class Timeline : Control
    {
        public static readonly DependencyProperty IsLiveProperty = DependencyProperty.Register(
            "IsLive", typeof(bool), typeof(Timeline));

        public static readonly DependencyProperty SelectionTipTextProperty = DependencyProperty.Register(
            "SelectionTipText", typeof(string), typeof(Timeline));

        public static readonly DependencyProperty MousePointBrushProperty = DependencyProperty.Register(
            "MousePointBrush", typeof(Brush), typeof(Timeline));

        static readonly DependencyPropertyKey timeAxisPropertyKey = DependencyProperty.RegisterReadOnly(
            "TimeAxis", typeof(TimeAxis), typeof(Timeline), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty TimeAxisProperty = timeAxisPropertyKey.DependencyProperty;

        public static readonly DependencyProperty SelectionTimeStartProperty = DependencyProperty.Register(
            "SelectionTimeStart", typeof(ulong), typeof(Timeline), new FrameworkPropertyMetadata(0UL));

        public static readonly DependencyProperty SelectionTimeEndProperty = DependencyProperty.Register(
            "SelectionTimeEnd", typeof(ulong), typeof(Timeline), new FrameworkPropertyMetadata(0UL));

        public static readonly RoutedUICommand ZoomInCommand = new RoutedUICommand(StringResources.TimelineContextMenuItem_ZoomIn, "ZoomIn", typeof(Timeline));
        public static readonly RoutedUICommand ZoomOutCommand = new RoutedUICommand(StringResources.TimelineContextMenuItem_ZoomOut, "ZoomOut", typeof(Timeline));
        public static readonly RoutedUICommand ZoomToAllCommand = new RoutedUICommand(StringResources.TimelineContextMenuItem_ZoomToAll, "ZoomToAll", typeof(Timeline));
        public static readonly RoutedUICommand ZoomToSelectionCommand = new RoutedUICommand(StringResources.TimelineContextMenuItem_ZoomToSelection, "ZoomToSelection", typeof(Timeline));
        public static readonly RoutedCommand ToggleScrollingCommand = new RoutedCommand("ToggleScrolling", typeof(Timeline));

        double mouseDownX;
        bool mousePanning;                      // True if panning took place during the current right-drag
        ulong visibleStartTimeAtPanStart;       // Visible start time when panning started
        int mousePixelAtPanStart;               // Pixel location of pan start
        bool selecting;                         // True if actively selecting (mouse down + sufficient drag distance)
        double tipDragOriginY;
        double tipDragDownY;
        ObservableCollection<SwimlanePanel.SwimlaneDefinition> swimlanes;
        TimeAxis timeAxis;
        FrameworkElement selectionTip;
        SwimlanePanel swimlanePanel;
        TimelineSelectionVisual selectionVisual;
        Grid selectionGrid;
        TranslateTransform selectionTipTransform;
        TranslateTransform selectionVisualTransform;
        bool zoomToSelectionRequestPosted;
        bool zoomToSelectionRequestCausedByUserInput;

        const int DataSideBarColumnIndex = 0;
        const int DataBarColumnIndex = 1;

        static Timeline()
        {
            ZoomInCommand.InputGestures.Add(new KeyGesture(Key.Add, ModifierKeys.Control, "Ctrl+Plus"));
            ZoomInCommand.InputGestures.Add(new KeyGesture(Key.OemPlus, ModifierKeys.Control));
            ZoomOutCommand.InputGestures.Add(new KeyGesture(Key.Subtract, ModifierKeys.Control, "Ctrl+Minus"));
            ZoomOutCommand.InputGestures.Add(new KeyGesture(Key.OemMinus, ModifierKeys.Control));
        }

        public Timeline()
        {
            this.ContextMenuItems = new ObservableCollection<object>();
            this.ContextMenuItems.Add(ZoomInCommand);
            this.ContextMenuItems.Add(ZoomOutCommand);
            this.ContextMenuItems.Add(ZoomToAllCommand);
            this.ContextMenuItems.Add(ZoomToSelectionCommand);
            this.CommandBindings.Add(new CommandBinding(ZoomInCommand, OnZoomInExecuted, OnZoomInCanExecute));
            this.CommandBindings.Add(new CommandBinding(ZoomOutCommand, OnZoomOutExecuted, OnZoomOutCanExecute));
            this.CommandBindings.Add(new CommandBinding(ZoomToAllCommand, OnZoomToAllExecuted));
            this.CommandBindings.Add(new CommandBinding(ZoomToSelectionCommand, OnZoomToSelectionExecuted, OnZoomToSelectionCanExecute));
            this.CommandBindings.Add(new CommandBinding(ToggleScrollingCommand, OnToggleScrollingExecuted));
            this.swimlanes = new ObservableCollection<SwimlanePanel.SwimlaneDefinition>();
            this.swimlanes.CollectionChanged += OnSwimLaneCollectionChanged;
            this.timeAxis = new TimeAxis() { HorizontalAlignment = HorizontalAlignment.Stretch, MouseOverElement = this };
            this.timeAxis.SetBinding(TimeAxis.IsLiveProperty, new Binding { Source = this, Path = new PropertyPath(IsLiveProperty) });
            this.timeAxis.SetBinding(TimeAxis.ForegroundProperty, new Binding { Source = this, Path = new PropertyPath(ForegroundProperty) });
            this.timeAxis.SetBinding(TimeAxis.BackgroundProperty, new Binding { Source = this, Path = new PropertyPath(BackgroundProperty) });
            this.TimeAxis = this.timeAxis;

            this.MouseWheel += OnMouseWheel;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseRightButtonDown += OnMouseRightButtonDown;
        }

        public bool IsLive
        {
            get { return (bool)GetValue(IsLiveProperty); }
            set { SetValue(IsLiveProperty, value); }
        }

        public string SelectionTipText
        {
            get { return (string)GetValue(SelectionTipTextProperty); }
            set { SetValue(SelectionTipTextProperty, value); }
        }

        public TimeAxis TimeAxis
        {
            get { return (TimeAxis)GetValue(TimeAxisProperty); }
            private set { SetValue(timeAxisPropertyKey, value); }
        }

        public ulong SelectionTimeStart
        {
            get { return (ulong)GetValue(SelectionTimeStartProperty); }
            set { SetValue(SelectionTimeStartProperty, value); }
        }

        public ulong SelectionTimeEnd
        {
            get { return (ulong)GetValue(SelectionTimeEndProperty); }
            set { SetValue(SelectionTimeEndProperty, value); }
        }


        public Brush MousePointBrush
        {
            get { return (Brush)GetValue(MousePointBrushProperty); }
            set { SetValue(MousePointBrushProperty, value); }
        }

        public ObservableCollection<object> ContextMenuItems { get; private set; }

        public IList<DataBarClipSpan> ClipSpans { get { return this.selectionVisual != null ? this.selectionVisual.ClipSpans : null; } }

        public void SetTimeAxisState(TimeAxisState viewState)
        {
            this.timeAxis.SetTimeAxisState(viewState);
        }

        public TimeAxisState GetTimeAxisState()
        {
            return this.timeAxis.GetTimeAxisState();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            GetTemplateChild("PART_SelectionTip", out this.selectionTip);
            GetTemplateChild("PART_SwimlanePanel", out this.swimlanePanel);
            GetTemplateChild("PART_SelectionVisual", out this.selectionVisual);
            GetTemplateChild("PART_SelectionGrid", out this.selectionGrid);
            GetTemplateChild("PART_SelectionTipTransform", out this.selectionTipTransform);
            GetTemplateChild("PART_SelectionVisualTransform", out this.selectionVisualTransform);

            this.timeAxis.SetBinding(TimeAxis.MousePointBrushProperty, new Binding { Source = this, Path = new PropertyPath(MousePointBrushProperty) });
            this.timeAxis.ContextMenu = new CommandContextMenu(this);
            this.timeAxis.ContextMenu.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = this, Path = new PropertyPath("ContextMenuItems") });
            this.timeAxis.MouseLeftButtonDown += OnTimeAxisMouseLeftButtonDown;
            this.timeAxis.VisibleRangeChanged += OnTimeAxisRangeChanged;
            this.selectionTip.MouseLeftButtonDown += OnSelectionTipMouseLeftButtonDown;

            AddTimeAxisLane();
            foreach (var lane in this.swimlanes)
            {
                AddLaneToPanel(lane);
            }
        }

        void AddTimeAxisLane()
        {
            var lane = new SwimlanePanel.SwimlaneDefinition { LaneElement = this.timeAxis, Dock = SwimlanePanel.SwimlaneDock.Top, Topmost = true };
            this.swimlanePanel.Swimlanes.Add(lane);
        }

        void GetTemplateChild<T>(string name, out T child) where T : class
        {
            child = GetTemplateChild(name) as T;
            if (child == null)
            {
                throw new InvalidOperationException(string.Format("Timeline template must have a {0} named '{1}'", typeof(T).Name, name));
            }
        }

        void OnSwimLaneCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.swimlanePanel != null)
            {
                if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count == 1)
                {
                    AddLaneToPanel((SwimlanePanel.SwimlaneDefinition)e.NewItems[0]);
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.Count == 1)
                {
                    RemoveLaneFromPanel((SwimlanePanel.SwimlaneDefinition)e.OldItems[0]);
                }
                else
                {
                    Debug.Fail("Unsupported action on swim lane collection!");
                }
            }
        }

        public void AddDataBar(DataBar dataBar, FrameworkElement dataSideBar, GridLength barHeight)
        {
            this.swimlanes.Add(new SwimlanePanel.SwimlaneDefinition { LaneElement = dataBar, SideElement = dataSideBar, Height = barHeight });
            
            // Make sure dataSideBar's visibility matches/follows the visibility of the dataBar
            if (dataSideBar != null)
            {
                dataSideBar.SetBinding(VisibilityProperty, new Binding { Source = dataBar, Path = new PropertyPath(VisibilityProperty) });
            }
        }

        public void RemoveDataBar(DataBar dataBar)
        {
            var lane = this.swimlanes.FirstOrDefault(l => l.LaneElement == dataBar);
            if (lane != null)
            {
                this.swimlanes.Remove(lane);
            }
        }

        void AddLaneToPanel(SwimlanePanel.SwimlaneDefinition lane)
        {
            var dataBar = (DataBar)lane.LaneElement;

            dataBar.TimeAxis = this.timeAxis;
            dataBar.SelectionClipSpansChanged += OnDataBarSelectionClipSpansChanged;
            dataBar.TimeRangeChanged += OnDataBarTimeRangeChanged;
            dataBar.ZoomToSelectionRequested += OnDataBarZoomToSelectionRequested;

            this.swimlanePanel.Swimlanes.Add(lane);
            OnDataBarSelectionClipSpansChanged(null, EventArgs.Empty);
            RecomputeAbsoluteTimeRange(true);
        }

        void RemoveLaneFromPanel(SwimlanePanel.SwimlaneDefinition lane)
        {
            var dataBar = (DataBar)lane.LaneElement;

            dataBar.TimeAxis = null;
            dataBar.SelectionClipSpansChanged -= OnDataBarSelectionClipSpansChanged;
            dataBar.TimeRangeChanged -= OnDataBarTimeRangeChanged;
            dataBar.ZoomToSelectionRequested -= OnDataBarZoomToSelectionRequested;

            this.swimlanePanel.Swimlanes.Remove(lane);
            RecomputeAbsoluteTimeRange(true);
        }

        public bool GetSelectionRange(out ulong selectionStart, out ulong selectionEnd)
        {
            if (this.selectionVisual.Visibility == Visibility.Visible)
            {
                selectionStart = this.SelectionTimeStart;
                selectionEnd = this.SelectionTimeEnd;
                return true;
            }

            selectionStart = selectionEnd = 0;
            return false;
        }

        void OnDataBarSelectionClipSpansChanged(object sender, EventArgs e)
        {
            this.selectionVisual.SetClips(this.timeAxis.ActualHeight, this.swimlanePanel.Children.OfType<DataBar>().OrderBy(db => Grid.GetRow(db)));
        }

        void OnDataBarTimeRangeChanged(object sender, EventArgs e)
        {
            RecomputeAbsoluteTimeRange(false);
        }

        void OnDataBarZoomToSelectionRequested(object sender, ZoomToSelectionRequestedEventArgs e)
        {
            if (!this.zoomToSelectionRequestPosted)
            {
                this.zoomToSelectionRequestCausedByUserInput = false;
                this.zoomToSelectionRequestPosted = true;
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.zoomToSelectionRequestPosted = false;

                    // There are multiple bars from which the request may come.  Often times they are
                    // in response to a single selection change event, which may have come from the event
                    // list, or it may have come from another bar.  We "accumulate" the user-caused-this-
                    // selection-change flag from all the requests we get, so if one of them was from the
                    // user, we suppress the zoom-to-selection.  (This is all to keep the selection from
                    // moving out from underneath the mouse.)
                    if (!this.zoomToSelectionRequestCausedByUserInput)
                    {
                        this.ZoomToSelection(zoomOutOnly: true);
                    }
                }));
            }

            if (e.CausedByUserInput)
            {
                this.zoomToSelectionRequestCausedByUserInput = true;
            }
        }

        public void RecomputeAbsoluteTimeRange(bool invalidateVisibleTime)
        {
            ulong timeStart = ulong.MaxValue;
            ulong timeEnd = 0;

            foreach (var bar in this.swimlanes)
            {
                timeStart = Math.Min(((DataBar)bar.LaneElement).TimeStart, timeStart);
                timeEnd = Math.Max(((DataBar)bar.LaneElement).TimeEnd, timeEnd);
            }

            this.timeAxis.SetAbsoluteTimeRange(timeStart, timeEnd, invalidateVisibleTime);
        }

        public void ResetZoom()
        {
            if (this.timeAxis != null)
            {
                this.timeAxis.ZoomToFit();
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                int mouseX = timeAxis.ScreenToPixel(e.GetPosition(timeAxis).X);

                if (e.Delta > 0)
                {
                    timeAxis.ZoomAroundPixel(1 / 1.1, mouseX);
                }
                else
                {
                    timeAxis.ZoomAroundPixel(1.1, mouseX);
                }

                e.Handled = true;
            }
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MouseDevice.Capture(this.selectionGrid))
            {
                this.mouseDownX = e.GetPosition(this.selectionGrid).X;

                this.selectionVisual.Visibility = Visibility.Collapsed;
                this.SelectionTimeStart = 0;
                this.SelectionTimeEnd = 0;
                
                this.selectionTip.Visibility = Visibility.Collapsed;
                this.selectionGrid.MouseLeftButtonUp += OnSelectMouseButtonUp;
                this.selectionGrid.MouseMove += OnSelectMouseMove;
                this.selectionGrid.LostMouseCapture += OnSelectLostMouseCapture;

            }
        }

        void OnSelectMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(null);
        }

        void OnSelectMouseMove(object sender, MouseEventArgs e)
        {
            var newX = e.GetPosition(this.selectionGrid).X;

            if (!this.selecting)
            {
                if (Math.Abs(newX - mouseDownX) >= SystemParameters.MinimumHorizontalDragDistance)
                {
                    this.selecting = true;
                    this.selectionVisual.Visibility = Visibility.Visible;
                    this.timeAxis.IsAutoPanning = false;
                }
                else
                {
                    return;
                }
            }

            this.SelectionTimeStart = timeAxis.ScreenToTime(Math.Min(newX, mouseDownX));
            this.SelectionTimeEnd = timeAxis.ScreenToTime(Math.Max(newX, mouseDownX));
            PositionSelectionVisual();
        }

        void OnSelectLostMouseCapture(object sender, MouseEventArgs e)
        {
            this.selecting = false;
            this.selectionGrid.MouseLeftButtonUp -= OnSelectMouseButtonUp;
            this.selectionGrid.MouseMove -= OnSelectMouseMove;
            this.selectionGrid.LostMouseCapture -= OnSelectLostMouseCapture;
        }

        void PositionSelectionVisual()
        {
            double selectionNs = (this.SelectionTimeEnd - this.SelectionTimeStart);
            double selectionMs = selectionNs / (1000 * 1000);
            double startX = this.timeAxis.TimeToScreen(this.SelectionTimeStart);
            double endX = this.timeAxis.TimeToScreen(this.SelectionTimeEnd);

            startX = Math.Min(Math.Max(startX, -1), this.selectionGrid.ActualWidth + 1);
            endX = Math.Min(Math.Max(endX, -1), this.selectionGrid.ActualWidth + 1);
            this.selectionVisualTransform.X = startX;
            this.selectionVisual.Width = Math.Max(endX - startX, 2);
            this.SelectionTipText = string.Format("Selected Duration: {0:N2}ms", selectionMs);
            this.selectionTip.UpdateLayout();

            if (endX < 0 || startX > this.selectionGrid.ActualWidth)
            {
                this.selectionTip.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.selectionTip.Visibility = Visibility.Visible;
                if (this.selectionGrid.ActualWidth < this.selectionTip.ActualWidth)
                {
                    this.selectionTipTransform.X = 0;
                }
                else
                {
                    this.selectionTipTransform.X = Math.Min(this.selectionGrid.ActualWidth - this.selectionTip.ActualWidth, Math.Max(startX, endX - this.selectionTip.ActualWidth));
                }
            }
        }

        void OnSelectionTipMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MouseDevice.Capture(this.selectionTip))
            {
                this.tipDragOriginY = this.selectionTipTransform.Y;
                this.tipDragDownY = e.GetPosition(this.selectionGrid).Y;
                this.selectionTip.MouseLeftButtonUp += OnTipDragMouseButtonUp;
                this.selectionTip.MouseMove += OnTipDragMouseMove;
                this.selectionTip.LostMouseCapture += OnTipDragLostMouseCapture;
                e.Handled = true;
            }
        }

        void OnTipDragMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(null);
        }

        void OnTipDragMouseMove(object sender, MouseEventArgs e)
        {
            double newY = this.tipDragOriginY + (e.GetPosition(this.selectionGrid).Y - this.tipDragDownY);
            this.selectionTipTransform.Y = Math.Max(0, Math.Min(this.swimlanePanel.ActualHeight - this.selectionTip.ActualHeight, newY));
        }

        void OnTipDragLostMouseCapture(object sender, MouseEventArgs e)
        {
            this.selectionTip.MouseLeftButtonUp -= OnTipDragMouseButtonUp;
            this.selectionTip.MouseMove -= OnTipDragMouseMove;
            this.selectionTip.LostMouseCapture -= OnTipDragLostMouseCapture;
        }

        private void OnTimeAxisMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);

            if (pos.Y > this.timeAxis.ActualHeight)
            {
                // Do not handle this -- it's likely a click on the rendered mouse-point time line
                return;
            }

            if (e.MouseDevice.Capture(this))
            {
                this.mouseDownX = pos.X;
                this.visibleStartTimeAtPanStart = this.timeAxis.VisibleTimeStart;
                this.mousePixelAtPanStart = this.timeAxis.ScreenToPixel(pos.X);
                this.MouseLeftButtonUp += OnPanMouseButtonUp;
                this.MouseMove += OnPanMouseMove;
                this.LostMouseCapture += OnTimeAxisPanLostMouseCapture;
                e.Handled = true;
            }
        }

        // Make right-button drags (everywhere) work just like left-button drags (in the timeAxis).
        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MouseDevice.Capture(this))
            {
                this.mouseDownX = e.GetPosition(this).X;
                this.visibleStartTimeAtPanStart = this.timeAxis.VisibleTimeStart;
                this.mousePixelAtPanStart = this.timeAxis.ScreenToPixel(e.GetPosition(this).X);
                this.MouseRightButtonUp += OnPanMouseButtonUp;
                this.MouseMove += OnPanMouseMove;
                this.LostMouseCapture += OnPanLostMouseCapture;
                this.mousePanning = false;
                e.Handled = true;
            }
        }

        private void OnPanMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(null);
            e.Handled = this.mousePanning;
        }

        private void OnPanMouseMove(object sender, MouseEventArgs e)
        {
            var curPos = e.GetPosition(this);
            var curPixel = this.timeAxis.ScreenToPixel(curPos.X);
            var pixelDelta = curPixel - this.mousePixelAtPanStart;

            if (!this.mousePanning)
            {
                this.mousePanning = Math.Abs(pixelDelta) > SystemParameters.MinimumHorizontalDragDistance;
            }

            if (this.mousePanning)
            {
                var timeDelta = (long)this.timeAxis.TimeStride * pixelDelta;
                var newStartTime = (ulong)Math.Max((long)this.visibleStartTimeAtPanStart - timeDelta, (long)this.timeAxis.AbsoluteTimeStart);

                this.timeAxis.PanTo(newStartTime);
                this.mousePanning = true;
            }
        }

        void OnTimeAxisPanLostMouseCapture(object sender, MouseEventArgs e)
        {
            this.MouseLeftButtonUp -= OnPanMouseButtonUp;
            this.MouseMove -= OnPanMouseMove;
            this.LostMouseCapture -= OnTimeAxisPanLostMouseCapture;
        }

        void OnPanLostMouseCapture(object sender, MouseEventArgs e)
        {
            this.MouseRightButtonUp -= OnPanMouseButtonUp;
            this.MouseMove -= OnPanMouseMove;
            this.LostMouseCapture -= OnPanLostMouseCapture;
        }

        void OnTimeAxisRangeChanged(object sender, EventArgs e)
        {
            if (this.selectionVisual.Visibility == Visibility.Visible && !this.selecting)
            {
                PositionSelectionVisual();
            }
        }

        private void OnZoomInExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            timeAxis.ZoomIn();
        }

        private void OnZoomInCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.timeAxis.TotalVisibleTime > (ulong)this.timeAxis.PixelWidth;
            e.Handled = true;
        }

        private void OnZoomOutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            timeAxis.ZoomOut();
        }

        private void OnZoomOutCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.timeAxis.AbsoluteTimeStart < this.timeAxis.VisibleTimeStart || this.timeAxis.AbsoluteTimeEnd > this.timeAxis.VisibleTimeEnd;
            e.Handled = true;
        }

        private void OnZoomToAllExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            timeAxis.ZoomToFit();
        }

        private void OnZoomToSelectionExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomToSelection(zoomOutOnly: false);
        }

        void ZoomToSelection(bool zoomOutOnly)
        {
            ulong start, end;

            if (TryGetSelectionRange(out start, out end))
            {
                if ((zoomOutOnly && (end - start < this.timeAxis.TotalVisibleTime)) || (end - start < (ulong)this.timeAxis.PixelWidth))
                {
                    var pad = this.timeAxis.TotalVisibleTime / 8;

                    // Not asked to zoom in, or doing so would be pointless.  So keep the same delta between Start and End, and move
                    // (pan) the center of the selection range to the center of the screen, *only* if its outside the center 3/4.
                    if (start < this.timeAxis.VisibleTimeStart + pad || end > this.timeAxis.VisibleTimeEnd - pad)
                    {
                        var center = start + ((end - start) / 2);
                        var halfScreenTime = this.timeAxis.TotalVisibleTime / 2;

                        if (center > halfScreenTime)
                        {
                            start = center - halfScreenTime;
                        }
                        else
                        {
                            start = this.timeAxis.AbsoluteTimeStart;
                        }

                        end = start + this.timeAxis.TotalVisibleTime;

                        timeAxis.ZoomToHere(start, end);
                    }
                }
                else
                {
                    var pad = (ulong)(end - start) / 8;

                    if (start > this.timeAxis.AbsoluteTimeStart + pad)
                    {
                        start -= pad;
                    }

                    if (end < this.timeAxis.AbsoluteTimeEnd - pad)
                    {
                        end += pad;
                    }

                    timeAxis.ZoomToHere(start, end);
                }
            }
        }

        private void OnZoomToSelectionCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            ulong start, end;
            e.CanExecute = TryGetSelectionRange(out start, out end);
            e.Handled = true;
        }

        void OnToggleScrollingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.timeAxis.ToggleScrolling();
        }

        public bool TryGetSelectionRange(out ulong timeStart, out ulong timeEnd)
        {
            // The user-selection has precedence
            if (GetSelectionRange(out timeStart, out timeEnd))
            {
                return true;
            }

            ulong barSelectionStart;
            ulong barSelectionEnd;

            timeStart = ulong.MaxValue;
            timeEnd = 0;
            bool barHadSelection = false;

            // Merge selections of all bars together
            foreach (var bar in this.swimlanes)
            {
                if (((DataBar)bar.LaneElement).TryGetDataSelectionRange(out barSelectionStart, out barSelectionEnd))
                {
                    barHadSelection = true;
                    timeStart = Math.Min(timeStart, barSelectionStart);
                    timeEnd = Math.Max(timeEnd, barSelectionEnd);
                }
            }

            return barHadSelection;
        }

        class CommandContextMenu : ContextMenu
        {
            Timeline owner;

            public CommandContextMenu(Timeline owner)
            {
                this.owner = owner;
            }

            protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
            {
                base.PrepareContainerForItemOverride(element, item);

                var menuItem = element as MenuItem;
                var command = item as RoutedUICommand;

                if (menuItem != null && command != null)
                {
                    menuItem.Command = command;
                    menuItem.CommandTarget = this.owner;
                }
            }
        }
    }
}
