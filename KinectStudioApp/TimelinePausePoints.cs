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
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;
    using System.Xml.Linq;

    public class TimelinePausePointsCollection : TimelinePointsCollection<TimelinePausePoint>
    {
        public TimelinePausePointsCollection(string targetAlias, KStudioClipSource clipSource, TimelineMarkersCollection markers)
            : base(targetAlias, clipSource)
        {
            DebugHelper.AssertUIThread();

            this.markers = markers;

            this.OnLoad();
        }

        public bool HasEnabled
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.hasEnabled;
            }
            private set
            {
                DebugHelper.AssertUIThread();

                if (this.hasEnabled != value)
                {
                    this.hasEnabled = value;
                    RaisePropertyChanged("HasEnabled");
                }
            }
        }

        public bool HasDisabled
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.hasDisabled;
            }
            private set
            {
                DebugHelper.AssertUIThread();

                if (this.hasDisabled != value)
                {
                    this.hasDisabled = value;
                    RaisePropertyChanged("HasDisabled");
                }
            }
        }

        public TimelineMarkersCollection Markers
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.markers;
            }
        }

        public TimelinePausePoint AddAt(TimeSpan relativeTime)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);
            Debug.Assert(this.Source != null);

            TimelinePausePoint pausePoint = null;
            bool updatePlayback = false;

            if ((relativeTime >= TimeSpan.Zero) && (relativeTime <= this.Source.Duration))
            {
                if (this.ignore == 0)
                {
                    if (this.RemoveInternal(relativeTime))
                    {
                        updatePlayback = true;
                    }
                }

                pausePoint = new TimelinePausePoint(this, relativeTime);

                this.Points.Add(pausePoint);

                if (updatePlayback)
                {
                    this.UpdatePlaybackPausePoints();
                }

                this.HasEnabled = true;

                this.IsDirty = true;

                this.OnSave();
            }

            return pausePoint;
        }

        public TimelinePausePoint AddAt(TimelineMarker marker)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);
            Debug.Assert(this.Source != null);

            if (marker == null)
            {
                throw new ArgumentNullException("marker");
            }

            TimeSpan relativeTime = marker.RelativeTime;

            TimelinePausePoint pausePoint = null;

            pausePoint = new TimelinePausePoint(this, relativeTime, marker);

            this.Points.Add(pausePoint);

            if (this.ignore == 0)
            {
                this.UpdatePlaybackPausePoints();
            }

            this.IsDirty = true;

            this.OnSave();

            return pausePoint;
        }

        public void RemoveAll()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);

            if (this.Points.Count > 0)
            {
                foreach (TimelinePausePoint pausePoint in this.Points)
                {
                    pausePoint.DecoupleMarker();
                }

                this.Points.Clear();

                this.UpdatePlaybackPausePoints();

                this.IsDirty = true;
                this.OnSave();

                this.HasEnabled = false;
                this.HasDisabled = false;
            }
        }

        public void EnableAll()
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(this.Points != null);

            this.ignore++;

            bool changed = false;

            foreach (TimelinePausePoint pausePoint in this.Points)
            {
                if (!pausePoint.IsEnabled)
                {
                    changed = true;
                    pausePoint.IsEnabled = true;
                }
            }

            this.ignore--;

            if (changed)
            {
                this.UpdatePlaybackPausePoints();

                this.IsDirty = true;
                this.OnSave();

                this.HasEnabled = true;
                this.HasDisabled = false;
            }
        }

        public void DisableAll()
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(this.Points != null);

            this.ignore++;

            bool changed = false;

            foreach (TimelinePausePoint pausePoint in this.Points)
            {
                if (pausePoint.IsEnabled)
                {
                    changed = true;
                    pausePoint.IsEnabled = false;
                }
            }

            this.ignore--;

            if (changed)
            {
                this.UpdatePlaybackPausePoints();

                this.IsDirty = true;
                this.OnSave();

                this.HasDisabled = true;
                this.HasEnabled = false;
            }
        }

        public bool RemoveAt(TimeSpan relativeTime)
        {
            bool result = false;

            DebugHelper.AssertUIThread();

            result = this.RemoveInternal(relativeTime);

            if (result)
            {
                this.UpdatePlaybackPausePoints();
            }

            this.OnSave();

            return result;
        }

        public bool Remove(TimelinePausePoint pausePoint)
        {
            DebugHelper.AssertUIThread();

            bool result = this.RemoveInternal(pausePoint);

            if (result)
            {
                this.UpdatePlaybackPausePoints();
            }

            this.OnSave();

            return result;
        }

        public override void OnTimePointDataChanged(TimelinePausePoint point, TimeSpan oldTime, bool doDirty, bool doPromote, bool doSave)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);

            if ((this.ignore == 0) && (point != null))
            {
                if (doDirty)
                {
                    this.IsDirty = true;
                }

                if (doPromote)
                {
                    if (this.Points.Count > 0)
                    {
                        if (this.Points.Last() != point)
                        {
                            // move it to the end of the list for UI purposes
                            if (this.Points.Remove(point))
                            {
                                this.Points.Add(point);
                            }
                        }
                    }
                }

                this.UpdatePlaybackPausePoints();

                if (doSave)
                {
                    this.OnSave();
                }

                this.HasEnabled = this.Points.Any((pp) => pp.IsEnabled);
                this.HasDisabled = this.Points.Any((pp) => !pp.IsEnabled);
            }
        }

        private bool RemoveInternal(TimeSpan relativeTime)
        {
            bool result = false;

            TimelinePausePoint pausePoint = this.Points.FirstOrDefault((pp) => !pp.HasCoupledMarker && (pp.RelativeTime == relativeTime));
            if (pausePoint != null)
            {
                result = this.RemoveInternal(pausePoint);

                Debug.Assert(result == true);
            }

            return result;
        }

        private bool RemoveInternal(TimelinePausePoint pausePoint)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);

            bool result = false;

            if (pausePoint == null)
            {
                throw new ArgumentNullException("pausePoint");
            }

            if (this.Points.Remove(pausePoint))
            {
                this.IsDirty = true;
                result = true;

                pausePoint.DecoupleMarker();                

                if (pausePoint.IsEnabled)
                {
                    this.HasEnabled = this.Points.Any((pp) => pp.IsEnabled);
                }
                else
                {
                    this.HasDisabled = this.Points.Any((pp) => !pp.IsEnabled);
                }
            }

            return result;
        }

        protected override void OnPlaybackChanged(KStudioPlayback oldPlayback)
        {
            DebugHelper.AssertUIThread();

            this.UpdatePlaybackPausePoints();
        }

        protected override void OnLoad()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);
            Debug.Assert(this.Source != null);

            this.Points.Clear();
            bool newHasEnabled = false;
            bool newHasDisabled = false;

            this.ignore++;

            XElement element = this.GetSettings("pausePoints");
            if (element != null)
            {
                foreach (XElement pausePointElement in element.Elements("pausePoint"))
                {
                    TimeSpan time = XmlExtensions.GetAttribute(pausePointElement, "time", TimeSpan.MinValue);
                    bool enabled = XmlExtensions.GetAttribute(pausePointElement, "enabled", true);
                    string markerName = XmlExtensions.GetAttribute(pausePointElement, "marker", (string)null);

                    TimelinePausePoint pausePoint = null;

                    // Marker names are not unique in a file, so we have to do a little rough guessing
                    // 1. the first marker found with the given name and time will be given the pause point
                    // 2. if no such, then the first marker found with the given name will be given the pause point
                    // 3. if no such, then the pause point will just be entered as a pause point by time

                    if (markerName != null)
                    {
                        if (this.markers != null)
                        {
                            TimelineMarker marker = markers.FirstOrDefault(m => (m.CoupledPausePoint == null) && (m.Name == markerName) && (m.RelativeTime == time));
                            if (marker == null)
                            {
                                marker = this.markers.FirstOrDefault(m => (m.CoupledPausePoint == null) && (m.Name == markerName));
                            }

                            if (marker != null)
                            {
                                pausePoint = marker.CreateCoupledPausePoint(this);
                            }
                        }
                    }

                    if (pausePoint == null)
                    {
                        if ((time >= TimeSpan.Zero) && (time <= this.Source.Duration))
                        {
                            pausePoint = this.AddAt(time);
                        }
                    }

                    if (pausePoint != null)
                    {
                        if (enabled)
                        {
                            newHasEnabled = true;
                        }
                        else
                        {
                            newHasDisabled = true;
                            pausePoint.IsEnabled = false;
                        }
                    }
                }
            }

            this.ignore--;

            if (newHasEnabled)
            {
                this.UpdatePlaybackPausePoints();
            }
    
            this.IsDirty = false;

            this.HasEnabled = newHasEnabled;
            this.HasDisabled = newHasDisabled;
        }

        protected override void OnSave()
        {
            DebugHelper.AssertUIThread();
            
            Debug.Assert(this.Points != null);
            Debug.Assert(this.Source != null);

            if ((this.ignore == 0) && (this.IsDirty))
            {
                XElement element = this.GetSettings("pausePoints");

                if (element != null)
                {
                    element.RemoveAll();

                    foreach (TimelinePausePoint pausePoint in this.Points)
                    {
                        XElement pausePointElement = new XElement("pausePoint");
                        pausePointElement.SetAttributeValue("time", pausePoint.RelativeTime.ToString("g", CultureInfo.InvariantCulture));
                        pausePointElement.SetAttributeValue("enabled", pausePoint.IsEnabled.ToString(CultureInfo.InvariantCulture));

                        string markerName = pausePoint.CoupledMarkerName;
                        if (markerName != null)
                        {
                            pausePointElement.SetAttributeValue("marker", markerName);
                        }

                        element.Add(pausePointElement);
                    }
                }

                this.IsDirty = false;
            }
        }

        private void UpdatePlaybackPausePoints()
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(this.Points != null);

            List<TimeSpan> pausePoints = new List<TimeSpan>();
            foreach (TimelinePausePoint pausePoint in this.Points)
            {
                if (pausePoint.IsEnabled)
                {
                    pausePoints.Add(pausePoint.RelativeTime);
                }
            }

            KStudioPlayback playback = this.Playback;
            if (playback != null)
            {
                playback.SetPausePointsByRelativeTime(pausePoints);
            }
        }

        private readonly TimelineMarkersCollection markers;
        private bool hasEnabled = false;
        private bool hasDisabled = false;
        private int ignore = 0;
    }
}
