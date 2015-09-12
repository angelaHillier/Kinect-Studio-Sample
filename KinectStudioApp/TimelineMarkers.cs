//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    public class TimelineMarkersCollection : TimelinePointsCollection<TimelineMarker>, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public TimelineMarkersCollection(string targetAlias, KStudioEventFile file)
            : base(targetAlias, file)
        {
            DebugHelper.AssertUIThread();

            if (file != null)
            {
                foreach (KStudioMarker marker in file.Markers)
                {
                    TimelineMarker markerProxy = new TimelineMarker(this, marker.RelativeTime, marker);

                    this.Points.Add(markerProxy);
                }
            }
        }

        ~TimelineMarkersCollection()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        public bool IsReadOnly
        {
            get
            {
                DebugHelper.AssertUIThread();

                return !(this.Source is KStudioWritableEventFile);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public TimelineMarker AddAt(TimeSpan relativeTime, string markerName)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);

            TimelineMarker markerProxy = null;

            KStudioWritableEventFile file = this.Source as KStudioWritableEventFile;
            if (file != null)
            {
                KStudioMarker marker = file.Markers.Add(markerName, relativeTime);
                if (marker != null)
                {
                    markerProxy = new TimelineMarker(this, relativeTime, marker);

                    this.Points.Add(markerProxy);
                }
                file.FlushIndex();
            }

            return markerProxy;
        }

        public void RemoveAll()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);

            KStudioWritableEventFile file = this.Source as KStudioWritableEventFile;
            if ((file != null) && (this.Points.Count > 0))
            {
                file.Markers.Clear();
                this.Points.Clear();

                this.IsDirty = true;
                file.FlushIndex();
            }
        }

        public bool Remove(TimelineMarker marker)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);

            bool result = false;

            if (marker == null)
            {
                throw new ArgumentNullException("marker");
            }

            KStudioWritableEventFile file = this.Source as KStudioWritableEventFile;
            if (file != null)
            {
                if (this.Points.Remove(marker))
                {
                    result = true;

                    file.Markers.Remove(marker.Marker);
                    file.FlushIndex();
                }
            }

            return result;
        }

        public override void OnTimePointDataChanged(TimelineMarker point, TimeSpan oldTime, bool doDirty, bool doPromote, bool doSave)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.Points != null);

            if (point != null)
            {
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
            }
        }

        protected override void OnPlaybackChanged(KStudioPlayback oldPlayback)
        {
        }

        protected override void OnLoad()
        {
        }

        protected override void OnSave()
        {
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.Points != null);

                foreach (TimelineMarker marker in this.Points)
                {
                    marker.Dispose();
                }
            }
        }
    }
}
