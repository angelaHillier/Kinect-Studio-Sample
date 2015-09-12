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
    using System.Globalization;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    public class TimelineMarker : TimelineTimeProxy, IDisposable
    {
        public TimelineMarker(TimelineMarkersCollection owner, TimeSpan relativeTime, KStudioMarker marker)
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
        }

        ~TimelineMarker()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        public string Name
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
            set
            {
                DebugHelper.AssertUIThread();

                if (this.marker != null)
                {
                    if (this.marker.Name != value)
                    {
                        this.marker.Name = value;
                        var file = this.Source as KStudioWritableEventFile;
                        if (file != null)
                        {
                            file.FlushIndex();
                        }

                        RaisePropertyChanged("Name");
                    }
                }
            }
        }

        public KStudioMarker Marker
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.marker;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                DebugHelper.AssertUIThread();

                return !(this.Source is KStudioWritableEventFile);
            }
        }

        public TimelinePausePoint CoupledPausePoint
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.pausePoint;
            }
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

        public bool HasMetadata
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.metadata != null;
            }
        }

        public MetadataInfo Metadata
        {
            get
            {
                DebugHelper.AssertUIThread();

                if ((this.metadata == null) && (this.marker != null))
                {
                    string title = String.Format(CultureInfo.CurrentCulture, Strings.MarkerMetadata_TitleFormat, this.Name);

                    if (this.IsReadOnly)
                    {
                        this.metadata = new MetadataInfo(false, title, title,
                            this.marker.Metadata, null);
                    }
                    else
                    {
                        this.metadata = new MetadataInfo(false, title, title,
                            new WritableMetadataProxy(this.Source as KStudioEventFile, this.marker.Metadata), null);
                    }
                }

                return this.metadata;
            }
        }

        public override void Remove()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.owner != null);

            if (!this.IsReadOnly)
            {
                this.owner.Remove(this);

                TimelinePausePoint temp = this.pausePoint;
                this.pausePoint = null;

                if (temp != null)
                {
                    temp.Remove();
                }
            }
        }

        public TimelinePausePoint CreateCoupledPausePoint(TimelinePausePointsCollection pausePoints)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.owner != null);

            if (pausePoints == null)
            {
                throw new ArgumentNullException("pausePoints");
            }

            if (this.pausePoint != null)
            {
                throw new InvalidOperationException("already has coupled pause point");
            }

            this.pausePoint = pausePoints.AddAt(this);

            return this.pausePoint;
        }

        public void DecouplePausePoint(TimelinePausePoint pausePointToDecouple)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.owner != null);

            if (pausePointToDecouple == null)
            {
                throw new ArgumentNullException("pausePointToDecouple");
            }

            if (this.pausePoint != null)
            {
                if (this.pausePoint != pausePointToDecouple)
                {
                    throw new InvalidOperationException("pause point not matching");
                }

                this.pausePoint = null;

                this.RaisePropertyChanged("CoupledPausePoint");
            }
        }

        protected override void OnDataChanged(TimeSpan oldTime, bool dirty, bool promote, bool save)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.owner != null);

            if (this.marker != null)
            {
                this.marker.RelativeTime = this.RelativeTime;
                var file = this.Source as KStudioWritableEventFile;
                if (file != null)
                {
                    file.FlushIndex();
                }
            }

            this.owner.OnTimePointDataChanged(this, oldTime, dirty, promote, save);

            if (this.pausePoint != null)
            {
                this.pausePoint.RelativeTime = this.RelativeTime;
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                DebugHelper.AssertUIThread();

                this.marker = null;
                this.metadata = null;
            }
        }

        private readonly TimelineMarkersCollection owner;
        private KStudioMarker marker;
        private MetadataInfo metadata = null;
        private TimelinePausePoint pausePoint = null;
    }
}
