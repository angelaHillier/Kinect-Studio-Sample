//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Controls;
    using System.Windows;
    using Microsoft.Xbox.Tools.Shared;
    using Microsoft.Kinect.Tools;
    using System.Windows.Input;
    using System.Windows.Threading;

    public class Timeline2 : Timeline
    {
        public Timeline2()
        {
            this.nudgeTimer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(10),
                };

            this.nudgeTimer.Tick += (s, e2) =>
                {
                    this.nudgeTimer.Stop();

                    try
                    {
                        this.SwimlanePanel.Height = double.NaN;
                    }
                    catch (Exception)
                    {
                        // ignore on shutdown
                    }
                };
        }

        public IEnumerable SwimLanesSource
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(Timeline2.SwimLanesSourceProperty) as IEnumerable;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(Timeline2.SwimLanesSourceProperty, value);
            }
        }

        public DataTemplate SidebarTemplate
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(Timeline2.SidebarTemplateProperty) as DataTemplate;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(Timeline2.SidebarTemplateProperty, value);
            }
        }

        public DataTemplateSelector SidebarTemplateSelector
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(Timeline2.SidebarTemplateSelectorProperty) as DataTemplateSelector;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.dataBars.Count > 0)
                {
                    throw new InvalidOperationException("cannot change template selector with items present");
                }

                this.SetValue(Timeline2.SidebarTemplateSelectorProperty, value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Swimlane")]
        public SwimlanePanel SwimlanePanel
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.swimlanePanel;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.swimlanePanel = this.GetTemplateChild("PART_SwimlanePanel") as SwimlanePanel;
        }

        private void OnSwimLanesSourceChanged(IEnumerable values)
        {
            DebugHelper.AssertUIThread();

            foreach (DataBar dataBar in this.dataBars)
            {
                this.RemoveDataBar(dataBar);
            }

            this.dataBars.Clear();

            if (values != null)
            {
                foreach (object value in values)
                {
                    DataBar dataBar = null;
                    FrameworkElement sidebar = null;

                    KStudioSeekableEventStream seekableEventStream = value as KStudioSeekableEventStream;
                    if (seekableEventStream != null)
                    {
                        sidebar = new ContentControl()
                            {
                                Content = seekableEventStream,
                                ContentTemplate = this.SidebarTemplate,
                                ContentTemplateSelector = this.SidebarTemplateSelector,
                            };

                        IEventLaneDataSource eventDataSource = seekableEventStream.UserState as IEventLaneDataSource;
                        if (eventDataSource != null)
                        {
                            EventLane2 eventDataBar = new EventLane2(eventDataSource.MinTime, eventDataSource.MaxTime)
                                {
                                    DataContext = seekableEventStream,
                                    DataSource = eventDataSource,
                                };

                            dataBar = eventDataBar;
                        }
                    }

                    if (dataBar == null)
                    {
                        dataBar = new DataBar();
                    }

                    this.dataBars.Add(dataBar);

                    this.AddDataBar(dataBar, sidebar, GridLength.Auto);
                }
            }
        }

        public IEnumerable<DataBar> DataBars
        {
            get
            {
                return this.dataBars.AsReadOnly();
            }
        }

        public void Nudge()
        {
            // Timeline has issues with sizing; nudge it
            DebugHelper.AssertUIThread();

            if (this.SwimlanePanel != null)
            {
                this.SwimlanePanel.Height = 0;

                this.nudgeTimer.Start();
            }
        }

        private List<DataBar> dataBars = new List<DataBar>();
        private SwimlanePanel swimlanePanel = null;
        private DispatcherTimer nudgeTimer = null;

        public readonly static DependencyProperty SwimLanesSourceProperty = DependencyProperty.Register("SwimLanesSource", typeof(IEnumerable), typeof(Timeline2), new PropertyMetadata(null, OnSwimLanesSourceChanged));
        public readonly static DependencyProperty SidebarTemplateProperty = DependencyProperty.Register("SidebarTemplate", typeof(DataTemplate), typeof(Timeline2));
        public readonly static DependencyProperty SidebarTemplateSelectorProperty = DependencyProperty.Register("SidebarTemplateSelector", typeof(DataTemplateSelector), typeof(Timeline2));

        private static void OnSwimLanesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            Timeline2 control = d as Timeline2;
            if (d != null)
            {
                control.OnSwimLanesSourceChanged(e.NewValue as IEnumerable);

                control.Nudge();
            }
        }
    }
}
