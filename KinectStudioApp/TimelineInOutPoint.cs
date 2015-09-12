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
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OutPoint")]
    public class TimelineInOutPoint : TimelineTimeProxy
    {
        public TimelineInOutPoint(TimelineInOutPointsCollection owner, TimeSpan relativeTime, bool inPoint)
            : base(relativeTime)
        {
            DebugHelper.AssertUIThread();

            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            this.owner = owner;
            this.inPoint = inPoint;
        }

        public bool IsInPoint
        {
            get
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.owner != null);

                return this.inPoint;
            }
            internal set
            {
                DebugHelper.AssertUIThread();

                if (this.inPoint != value)
                {
                    this.inPoint = value;
                    RaisePropertyChanged("IsInPoint");
                }
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

        protected override void OnDataChanged(TimeSpan oldTime, bool dirty, bool promote, bool save)
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(this.owner != null);

            this.owner.OnTimePointDataChanged(this, oldTime, dirty, promote, save);
        }

        private readonly TimelineInOutPointsCollection owner;
        private bool inPoint;
    }
}
