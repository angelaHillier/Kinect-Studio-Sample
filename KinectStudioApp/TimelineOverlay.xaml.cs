//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Collections.Generic;
    using System.Windows.Input;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    public partial class TimelineOverlay : UserControl
    {
        public TimelineOverlay()
        {
            DebugHelper.AssertUIThread();

            this.InitializeComponent();

            Loaded += TimelineOverlay_Loaded;
        }

        public ulong Minimum
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (ulong)GetValue(MinimumProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                SetValue(MinimumProperty, value);
            }
        }

        public ulong Maximum
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (ulong)GetValue(MaximumProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                SetValue(MaximumProperty, value);
            }
        }

        public TimelineMarkersCollection MarkersSource
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(MarkersSourceProperty) as TimelineMarkersCollection;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(MarkersSourceProperty, value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OutPoints")]
        public TimelineInOutPointsCollection InOutPointsSource
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(InOutPointsSourceProperty) as TimelineInOutPointsCollection;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(InOutPointsSourceProperty, value);
            }
        }

        public TimelinePausePointsCollection PausePointsSource
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(PausePointsSourceProperty) as TimelinePausePointsCollection;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(PausePointsSourceProperty, value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public IEnumerable<MetadataView> MetadataViews
        {
            get
            {
                IEnumerable<MetadataView> value = null;

                IMetadataViewService metadataViewService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(IMetadataViewService)) as IMetadataViewService;

                if (metadataViewService != null)
                {
                    value = metadataViewService.GetMetadataViews(Window.GetWindow(this));
                }

                return value;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        {
                            if (this.panel != null)
                            {
                                this.newPointMouseEventArgs = e;

                                Point pt = e.GetPosition(this.panel);
                                ulong min = this.panel.Minimum;
                                ulong max = this.panel.Maximum;
                                double v = min + (max - min) * (pt.X / this.panel.ActualWidth);

                                TimeSpan relativeTime = TimeSpan.FromTicks((long)v);
                                Point ptMarkers = e.GetPosition(this.MarkersItemsControl);
                                Point ptPausePoints = e.GetPosition(this.PausePointsItemsControl);

                                TimelineTimeProxy point = null;

                                if ((ptMarkers.X >= 0) && (ptMarkers.Y >= 0) && (ptMarkers.X <= this.MarkersItemsControl.ActualWidth) && (ptMarkers.Y <= this.MarkersItemsControl.ActualHeight))
                                {
                                    if ((this.MarkersSource != null) && !this.MarkersSource.IsReadOnly)
                                    {
                                        string name = this.GetUniqueMarkerName();

                                        point = this.MarkersSource.AddAt(relativeTime, name);
                                    }
                                }

                                if ((ptPausePoints.X >= 0) && (ptPausePoints.Y >= 0) && (ptPausePoints.X <= this.PausePointsItemsControl.ActualWidth) && (ptPausePoints.Y <= this.PausePointsItemsControl.ActualHeight))
                                {
                                    if (this.PausePointsSource != null)
                                    {
                                        point = this.PausePointsSource.AddAt(relativeTime);
                                    }
                                }

                                if (point != null)
                                {
                                    // immediately let the pause point be draggable
                                    point.IsFloating = true;
                                    point.ForceHasMovedDuringLastFloat();
                                }
                            }

                            break;
                        }

                    case MouseButton.Right:
                        {
                            if (this.panel != null)
                            {
                                Point pt = e.GetPosition(this.panel);
                                this.newPointTime = TimeSpan.FromTicks((long)(this.panel.Minimum + ((this.panel.Maximum - this.panel.Minimum) * (pt.X / this.panel.ActualWidth))));
                            }

                            break;
                        }
                }
            }

            base.OnMouseDown(e);
        }

        private void TimelineOverlay_Loaded(object sender, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.panel = this.GetVisualChild<RelativePanel>();
        }

        private void PausePointDecouple_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                TimelinePausePoint pausePoint = e.Parameter as TimelinePausePoint;

                if ((pausePoint != null) && pausePoint.HasCoupledMarker)
                {
                    e.Handled = true;

                    pausePoint.Remove();
                }
            }
        }

        private void PausePointDecouple_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                TimelinePausePoint pausePoint = e.Parameter as TimelinePausePoint;

                if (pausePoint != null)
                {
                    e.Handled = true;
                    e.CanExecute = pausePoint.HasCoupledMarker;
                }
            }
        }

        private void TimePointRemove_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                TimelineTimeProxy point = e.Parameter as TimelineTimeProxy;

                if (point != null)
                {
                    e.Handled = true;

                    point.Remove();
                }
            }
        }

        private void TimePointRemove_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                TimelineTimeProxy point = e.Parameter as TimelineTimeProxy;

                if (point != null)
                {
                    e.Handled = true;
                    e.CanExecute = !point.IsReadOnly;
                }
            }
        }

        private void MarkerAdd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.MarkersSource != null) && (this.newPointTime.HasValue))
            {
                e.Handled = true;

                Window window = Window.GetWindow(this);

                string name = GetUniqueMarkerName();

                EditStringDialog dialog = new EditStringDialog()
                    {
                        Owner = window,
                        Title = Strings.TimelineMarker_Add_Title,
                        Prompt = Strings.TimelineMarker_Name_Prompt, 
                        Value = name,
                        MaximumLength = 63,
                    };

                if (dialog.ShowDialog() == true)
                {
                    this.MarkersSource.AddAt(this.newPointTime.Value, dialog.Value.Trim());
                }
            }

            this.newPointTime = null;
        }

        private void MarkerAdd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.MarkersSource != null))
            {
                e.Handled = true;
                e.CanExecute = !this.MarkersSource.IsReadOnly;
            }
        }

        private void MarkerEditName_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                TimelineMarker marker = e.Parameter as TimelineMarker;

                if (marker != null)
                {
                    e.Handled = true;

                    Window window = Window.GetWindow(this);

                    EditStringDialog dialog = new EditStringDialog()
                    {
                        Owner = window,
                        Title = Strings.EditTimeMarker_Title,
                        Prompt = Strings.TimelineMarker_Name_Prompt,
                        Value = marker.Name,
                        MaximumLength = 63,
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        marker.Name = dialog.Value.Trim();
                    }
                }
            }
        }

        private void MarkerEditName_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                TimelineMarker marker = e.Parameter as TimelineMarker;

                if (marker != null)
                {
                    e.Handled = true;
                    e.CanExecute = !marker.IsReadOnly;
                }
            }
        }

        private void SetInPoint_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.InOutPointsSource != null))
            {
                e.Handled = true;

                TimelineMarker marker = e.Parameter as TimelineMarker;

                if (marker == null)
                {
                    if (this.newPointTime.HasValue)
                    {
                        this.InOutPointsSource.InPoint = this.newPointTime.Value;
                    }
                }
                else
                {
                    this.InOutPointsSource.InPoint = marker.RelativeTime;
                }
            }

            this.newPointTime = null;
        }

        private void SetOutPoint_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if ((e != null) && (this.InOutPointsSource != null))
            {
                e.Handled = true;

                TimelineMarker marker = e.Parameter as TimelineMarker;

                if (marker == null)
                {
                    if (this.newPointTime.HasValue)
                    {
                        this.InOutPointsSource.OutPoint = this.newPointTime.Value;
                    }
                }
                else
                {
                    this.InOutPointsSource.OutPoint = marker.RelativeTime;
                }
            }

            this.newPointTime = null;
        }

        private void SetInOutPoint_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.InOutPointsSource != null))
            {
                e.Handled = true;
                e.CanExecute = (this.InOutPointsSource != null) && (this.InOutPointsSource.IsEnabled);
            }
        }

        private void PausePointAdd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.PausePointsSource != null))
            {
                e.Handled = true;

                TimelineMarker marker = e.Parameter as TimelineMarker;
                if (marker == null)
                {
                    if (this.newPointTime.HasValue)
                    {
                        this.PausePointsSource.AddAt(this.newPointTime.Value);
                    }
                }
                else
                {
                    this.PausePointsSource.AddAt(marker.RelativeTime);
                }
            }

            this.newPointTime = null;
        }

        private void PausePointAdd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.PausePointsSource != null))
            {
                e.Handled = true;
                e.CanExecute = true; 
            }
        }

        private void PausePointCouple_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.PausePointsSource != null))
            {
                e.Handled = true;

                TimelineMarker marker = e.Parameter as TimelineMarker;
                if (marker != null)
                {
                    if (marker.CoupledPausePoint == null)
                    {
                        marker.CreateCoupledPausePoint(this.PausePointsSource);
                    }
                }
            }

            this.newPointTime = null;
        }

        private void PausePointCouple_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.PausePointsSource != null))
            {
                e.Handled = true;

                TimelineMarker marker = e.Parameter as TimelineMarker;
                if (marker != null)
                {
                    e.CanExecute = marker.CoupledPausePoint == null;
                }
            }
        }

        private void PausePointToggle_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                TimelinePausePoint pausePoint = e.Parameter as TimelinePausePoint;

                if (pausePoint != null)
                {
                    e.Handled = true;

                    pausePoint.IsEnabled = !pausePoint.IsEnabled;
                }
            }
        }

        private void TimePointEdit_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                TimelineTimeProxy timePoint = e.Parameter as TimelineTimeProxy;

                if (timePoint != null)
                {
                    e.Handled = true;

                    Window window = Window.GetWindow(this);

                    EditTimeSpanDialog dialog = new EditTimeSpanDialog()
                        {
                            Owner = window,
                            Title = (timePoint is TimelinePausePoint) ? Strings.EditTimelinePausePoint_Title : Strings.EditTimeMarker_Title,
                            Minimum = TimeSpan.Zero,
                            Maximum = timePoint.Source.Duration,
                            Value = timePoint.RelativeTime,
                        };

                    if (dialog.ShowDialog() == true)
                    {
                        if (dialog.Value != timePoint.RelativeTime)
                        {
                            this.PausePointsSource.RemoveAt(dialog.Value);

                            timePoint.RelativeTime = dialog.Value;
                        }
                    }
                }
            }
        }

        private void TimePointEdit_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                TimelineTimeProxy point = e.Parameter as TimelineTimeProxy;

                if (point != null)
                {
                    e.Handled = true;
                    e.CanExecute = !point.IsReadOnly;
                }
            }
        }

        private void Point_Loaded(object sender, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (this.newPointMouseEventArgs != null)
            {
                MouseButtonEventArgs mbe = this.newPointMouseEventArgs;
                this.newPointMouseEventArgs = null;

                Thumb2 thumb = sender as Thumb2;
                if (thumb != null)
                {
                    thumb.ForceDrag(mbe);
                }
            }
        }

        private void Point_DragStarted(object sender, DragStartedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            FrameworkElement frameworkElement = sender as FrameworkElement;
            if (frameworkElement != null)
            {
                TimelineTimeProxy point = frameworkElement.DataContext as TimelineTimeProxy;
                if (point != null)
                {
                    e.Handled = true;

                    TimelinePausePoint pausePoint = point as TimelinePausePoint;

                    if ((pausePoint != null) && pausePoint.HasCoupledMarker)
                    {
                        if (this.PausePointsSource != null)
                        {
                            this.PausePointsSource.OnTimePointDataChanged(pausePoint, pausePoint.RelativeTime, false, true, false);
                        }
                    }
                    else if ((pausePoint != null) || point.IsEnabled)
                    {
                        point.IsFloating = true;
                    }
                }
            }
        }

        private void Point_DragDelta(object sender, DragDeltaEventArgs e)
        {
            DebugHelper.AssertUIThread();

            FrameworkElement frameworkElement = sender as FrameworkElement;
            if (frameworkElement != null)
            {
                if (this.panel != null)
                {
                    TimelineTimeProxy point = frameworkElement.DataContext as TimelineTimeProxy;
                    if ((point != null) && point.IsFloating)
                    {
                        e.Handled = true;

                        if ((point is TimelinePausePoint) || point.IsEnabled)
                        {
                            long ticks = point.RelativeTime.Ticks + (long)((e.HorizontalChange / this.panel.ActualWidth) * (this.panel.Maximum - this.panel.Minimum));
                            ticks = Math.Min(Math.Max(ticks, (long)panel.Minimum), (long)panel.Maximum);
                            point.RelativeTime = TimeSpan.FromTicks(ticks);
                        }
                    }
                }
            }
        }

        private void Point_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            FrameworkElement frameworkElement = sender as FrameworkElement;
            if (frameworkElement != null)
            {
                TimelineTimeProxy point = frameworkElement.DataContext as TimelineTimeProxy;
                if ((point != null) && point.IsFloating)
                {
                    TimelinePausePoint pausePoint = point as TimelinePausePoint;
                    if (pausePoint != null)
                    {
                        if (this.PausePointsSource != null)
                        {
                            if (!point.HasMovedDuringLastFloat)
                            {
                                this.PausePointsSource.Remove(pausePoint);
                            }
                        }
                    }

                    point.IsFloating = false;
                }
            }
        }

        private void MetadataMenuItemSubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                menuItem.ItemsSource = null;
                menuItem.Items.Clear();
                menuItem.ItemsSource = this.MetadataViews;
            }
        }

        private string GetUniqueMarkerName()
        {
            string value = Strings.TimelineMarker_NewName;
            TimelineMarkersCollection markers = this.MarkersSource;

            if (markers != null)
            {
                if (markers.Any(m => m.Name == value))
                {
                    int i = 2;
                    while (true)
                    {
                        value = String.Format(CultureInfo.CurrentCulture, Strings.TimelineMarker_NewName_Format, i);

                        if (!markers.Any(m => m.Name == value))
                        {
                            break;
                        }

                        ++i;
                    }
                }
            }

            return value;
        }

        private RelativePanel panel = null;
        private MouseButtonEventArgs newPointMouseEventArgs = null;
        private TimeSpan? newPointTime = null;

        private static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(ulong), typeof(TimelineOverlay));
        private static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(ulong), typeof(TimelineOverlay));
        private static readonly DependencyProperty MarkersSourceProperty = DependencyProperty.Register("MarkersSource", typeof(TimelineMarkersCollection), typeof(TimelineOverlay));
        private static readonly DependencyProperty InOutPointsSourceProperty = DependencyProperty.Register("InOutPointsSource", typeof(TimelineInOutPointsCollection), typeof(TimelineOverlay));
        private static readonly DependencyProperty PausePointsSourceProperty = DependencyProperty.Register("PausePointsSource", typeof(TimelinePausePointsCollection), typeof(TimelineOverlay));
    }
}
