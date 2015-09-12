//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    public class TimelinePausePoint : TimelineTimeProxy
    {
        public TimelinePausePoint(TimelinePausePointsCollection owner, TimeSpan relativeTime)
            : base(relativeTime)
        {
            DebugHelper.AssertUIThread();

            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            this.owner = owner;
            this.marker = null;
        }

        public TimelinePausePoint(TimelinePausePointsCollection owner, TimeSpan relativeTime, TimelineMarker marker)
            : base(relativeTime)
        {
            DebugHelper.AssertUIThread();

            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            if (marker == null)
            {
                throw new ArgumentNullException("marker");
            }

            this.owner = owner;
            this.marker = marker;

            this.marker.PropertyChanged += Marker_PropertyChanged;
        }

        public override KStudioClipSource Source
        {
            get
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.owner != null);

                return this.owner.Source;
            }
        }

        public bool HasCoupledMarker
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.marker != null;
            }
        }

        public string CoupledMarkerName
        {
            get
            {
                DebugHelper.AssertUIThread();

                string value = null;

                if (this.marker != null)
                {
                    value = this.marker.Name;
                }

                return value;
            }
        }

        public override void Remove()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.owner != null);

            this.owner.Remove(this);
        }

        public void DecoupleMarker()
        {
            DebugHelper.AssertUIThread();

            if (this.marker != null)
            {
                this.marker.PropertyChanged -= Marker_PropertyChanged;

                this.marker.DecouplePausePoint(this);
            }
        }

        protected override void OnDataChanged(TimeSpan oldTime, bool dirty, bool promote, bool save)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.owner != null);

            this.owner.OnTimePointDataChanged(this, oldTime, dirty, promote, save);
        }

        private void Marker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.marker != null);
            Debug.Assert(this.marker.CoupledPausePoint == this);

            if (e.PropertyName == "IsFloating")
            {
                if (this.marker.IsFloating)
                {
                    this.owner.OnTimePointDataChanged(this, this.RelativeTime, false, true, false);
                }
            }
        }

        private readonly TimelinePausePointsCollection owner;
        private readonly TimelineMarker marker;
    }
}
