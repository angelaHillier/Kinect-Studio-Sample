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

    public abstract class TimelineTimeProxy : KStudioUserState
    {
        protected TimelineTimeProxy(TimeSpan relativeTime)
        {
            DebugHelper.AssertUIThread();

            if (relativeTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("relativeTime");
            }

            this.relativeTime = relativeTime;
        }

        public TimeSpan RelativeTime
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.relativeTime;
            }
            set
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.Source != null);

                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                if (!this.IsReadOnly && (this.relativeTime != value))
                {
                    TimeSpan oldTime = this.relativeTime;

                    if (value > this.Source.Duration)
                    {
                        value = this.Source.Duration;
                    }

                    this.relativeTime = value;
                    RaisePropertyChanged("RelativeTime");

                    if (this.floating)
                    {
                        this.moved = true;
                    }

                    this.OnDataChanged(oldTime, true, false, !this.floating);
                }
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsEnabled
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.enabled;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.enabled != value)
                {
                    this.enabled = value;
                    RaisePropertyChanged("IsEnabled");

                    this.OnDataChanged(this.relativeTime, true, true, true);
                }
            }
        }

        public bool IsFloating
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.floating;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (!(this.IsReadOnly) && (this.floating != value))
                {
                    this.floating = value;
                    RaisePropertyChanged("IsFloating");

                    if (this.floating)
                    {
                        this.moved = false;
                    }

                    this.OnDataChanged(this.relativeTime, false, !this.floating, !this.floating);
                }
            }
        }

        public bool HasMovedDuringLastFloat
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.moved;
            }
        }

        public abstract KStudioClipSource Source { get; }

        public virtual void Remove() { }

        public void ForceHasMovedDuringLastFloat()
        {
            DebugHelper.AssertUIThread();

            this.moved = true;
        }

        protected abstract void OnDataChanged(TimeSpan oldTime, bool dirty, bool promote, bool save);

        private TimeSpan relativeTime;
        private bool enabled = true;
        private bool floating = false;
        private bool moved = false;
    }
}
