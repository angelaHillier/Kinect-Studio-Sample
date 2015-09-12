//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OutPoints")]
    public class TimelineInOutPointsCollection : TimelinePointsCollection<TimelineInOutPoint>
    {
        public TimelineInOutPointsCollection(string targetAlias, KStudioClipSource clipSource)
            : base(targetAlias, clipSource)
        {
            DebugHelper.AssertUIThread();

            this.OnLoad();
        }

        public bool IsEnabled
        {
            get
            {
                DebugHelper.AssertUIThread();

                bool enabled = false;

                TimelineInOutPoint point = this.Points.FirstOrDefault();
                if (point != null)
                {
                    enabled = point.IsEnabled;
                }

                return enabled;
            }
        }

        public TimeSpan InPoint
        {
            get
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.Points != null);

                TimeSpan value = TimeSpan.Zero;

                TimelineInOutPoint point = this.Points.FirstOrDefault((p) => p.IsInPoint);
                if (point != null)
                {
                    value = point.RelativeTime;
                }

                return value;
            }
            set
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.Points != null);
                Debug.Assert(this.Source != null);

                if (value < TimeSpan.Zero)
                {
                    value = TimeSpan.Zero;
                }
                else if (value > this.Source.Duration)
                {
                    value = this.Source.Duration;
                }

                TimelineInOutPoint point = this.Points.FirstOrDefault((p) => p.IsInPoint);
                if (point != null)
                {
                    point.RelativeTime = value;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OutPoint")]
        public TimeSpan OutPoint
        {
            get
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.Points != null);
                Debug.Assert(this.Source != null);

                TimeSpan value = TimeSpan.Zero;

                TimelineInOutPoint point = this.Points.FirstOrDefault((p) => !p.IsInPoint);
                if (point != null)
                {
                    value = point.RelativeTime;
                }

                return value;
            }
            set
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.Points != null);

                if (value < TimeSpan.Zero)
                {
                    value = TimeSpan.Zero;
                }
                else if (value > this.Source.Duration)
                {
                    value = this.Source.Duration;
                }

                TimelineInOutPoint point = this.Points.FirstOrDefault((p) => !p.IsInPoint);
                if (point != null)
                {
                    point.RelativeTime = value;
                }
            }
        }

        public override void OnTimePointDataChanged(TimelineInOutPoint point, TimeSpan oldTime, bool doDirty, bool doPromote, bool doSave)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);

            if (this.ignore == 0)
            {
                if (doDirty)
                {
                    this.IsDirty = true;
                }

                // if the in and out points cross over, swap them
                if (this.Points.Count >= 2)
                {
                    TimelineInOutPoint inPoint = null;
                    TimelineInOutPoint outPoint = null;

                    TimelineInOutPoint point0 = this.Points[0];
                    TimelineInOutPoint point1 = this.Points[1];

                    Debug.Assert(point0 != null);
                    Debug.Assert(point1 != null);

                    if (point0.RelativeTime > point1.RelativeTime)
                    {
                        inPoint = point1;
                        outPoint = point0;
                    }
                    else
                    {
                        inPoint = point0;
                        outPoint = point1;
                    }

                    Debug.Assert(inPoint != null);
                    Debug.Assert(outPoint != null);

                    inPoint.IsInPoint = true;
                    outPoint.IsInPoint = false;

                    if (doSave)
                    {
                        this.UpdatePlayback();

                        this.OnSave();
                    }
                }
            }
        }

        protected override void OnPlaybackChanged(KStudioPlayback oldPlayback)
        {
            DebugHelper.AssertUIThread();

            if (oldPlayback != null)
            {
                oldPlayback.StateChanged -= Playback_StateChanged;
            }

            KStudioPlayback playback = this.Playback;
            if (playback == null)
            {
                foreach (TimelineTimeProxy inoutPoint in this.Points)
                {
                    inoutPoint.IsEnabled = true;
                }

                RaisePropertyChanged("IsEnabled");
            }
            else
            {
                playback.StateChanged += Playback_StateChanged;

                this.UpdatePlayback();
            }
        }

        protected override void OnLoad()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);
            Debug.Assert(this.Source != null);

            this.Points.Clear();

            TimeSpan duration = this.Source.Duration;

            TimeSpan? inPoint = null; ;
            TimeSpan? outPoint = null;

            XElement element = this.GetSettings("inOutPoints");
            if (element != null)
            {
                inPoint = LoadPoint(element, "inPoint", duration);
                outPoint = LoadPoint(element, "outPoint", duration);
            }

            this.IsDirty = false;

            if (!inPoint.HasValue || (inPoint.Value < TimeSpan.Zero) || (inPoint.Value > duration))
            {
                inPoint = TimeSpan.Zero;
            }

            if (!outPoint.HasValue || (outPoint.Value < TimeSpan.Zero) || (outPoint.Value > duration))
            {
                outPoint = duration;
            }

            Debug.Assert(inPoint.HasValue);
            Debug.Assert(outPoint.HasValue);

            if (outPoint.Value < inPoint.Value)
            {
                TimeSpan temp = outPoint.Value;
                outPoint = inPoint.Value;
                inPoint = temp;
            }

            this.Points.Add(new TimelineInOutPoint(this, inPoint.Value, true));
            this.Points.Add(new TimelineInOutPoint(this, outPoint.Value, false));
        }

        protected override void OnSave()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);
            Debug.Assert(this.Source != null);

            if ((this.ignore == 0) && (this.IsDirty))
            {
                XElement element = this.GetSettings("inOutPoints");
                if (element != null)
                {
                    foreach (TimelineInOutPoint inOutPoint in this.Points)
                    {
                        if (inOutPoint.IsInPoint)
                        {
                            element.SetAttributeValue("inPoint", inOutPoint.RelativeTime.ToString("g", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            element.SetAttributeValue("outPoint", inOutPoint.RelativeTime.ToString("g", CultureInfo.InvariantCulture));
                        }
                    }

                    this.IsDirty = false;
                }
            }
        }

        private void UpdatePlayback()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);

            KStudioPlayback playback = this.Playback;
            if (playback != null)
            {
                playback.InPointByRelativeTime = this.InPoint;
                playback.OutPointByRelativeTime = this.OutPoint;
            }
        }

        private static TimeSpan? LoadPoint(XElement element, string name, TimeSpan duration)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(name));

            TimeSpan? value = null;

            if (element != null)
            {
                TimeSpan temp = XmlExtensions.GetAttribute(element, name, TimeSpan.MinValue);
                if ((temp >= TimeSpan.Zero) && (temp <= duration))
                {
                    value = temp;
                }
            }

            return value;
        }

        private void Playback_StateChanged(object sender, EventArgs e)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);

            KStudioPlayback playback = this.Playback;
            if ((playback != null) && (playback == sender))
            {
                KStudioPlaybackState state = playback.State;
                bool enabled = (state == KStudioPlaybackState.Idle) || (state == KStudioPlaybackState.Paused) || (state == KStudioPlaybackState.Stopped);

                this.ignore++;

                foreach (TimelineInOutPoint point in this.Points)
                {
                    point.IsEnabled = enabled;
                }

                this.ignore--;
            }

            RaisePropertyChanged("IsEnabled");
        }

        private int ignore = 0;
    }
}
