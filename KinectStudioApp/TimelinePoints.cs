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
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Xml.Linq;
    using Microsoft.Xbox.Tools.Shared;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1708:IdentifiersShouldDifferByMoreThanCase")]
    public abstract class TimelinePointsCollection<T> : KStudioUserState, IReadOnlyCollection<T>, INotifyCollectionChanged
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected TimelinePointsCollection(string targetAlias, KStudioClipSource clipSource)
        {
            DebugHelper.AssertUIThread();

            if (clipSource == null)
            {
                throw new ArgumentNullException("clipSource");
            }

            this.targetAlias = targetAlias;
            this.clipSource = clipSource;
            this.points = new ObservableCollection<T>();
        }

        // IReadOnlyCollection<T>

        public int Count
        {
            get
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.points != null);

                return this.points.Count;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.points != null);

            return ((ICollection<T>)this.points).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.points != null);

            return this.points.GetEnumerator();
        }

        // INotifyCollectionChanged

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.points != null);

                this.points.CollectionChanged += value;
            }
            remove
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.points != null);

                this.points.CollectionChanged -= value;
            }
        }

        // TimelinePointsCollection

        public KStudioClipSource Source
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.clipSource;
            }
        }

        public KStudioPlayback Playback
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playback;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.playback)
                {
                    KStudioPlayback oldPlayback = this.playback;

                    this.playback = value;

                    this.OnPlaybackChanged(oldPlayback);
                }
            }
        }

        protected bool IsDirty
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.dirty;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.dirty = value;
            }
        }

        protected IList<T> Points
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.points;
            }
        }

        public abstract void OnTimePointDataChanged(T point, TimeSpan oldTime, bool doDirty, bool doPromote, bool doSave);

        protected abstract void OnPlaybackChanged(KStudioPlayback oldPlayback);

        protected XElement GetSettings(string settingsKey)
        {
            XElement element = null;

            IFileSettingsService fileSettingsService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(IFileSettingsService)) as IFileSettingsService;
            if (fileSettingsService != null)
            {
                if (this.targetAlias == null)
                {
                    element = fileSettingsService.GetSettings(this.clipSource, settingsKey);
                }
                else
                {
                    element = fileSettingsService.GetSettings(targetAlias, this.clipSource, settingsKey);
                }
            }

            return element;
        }

        protected abstract void OnLoad();
        protected abstract void OnSave();

        private readonly string targetAlias;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        private readonly KStudioClipSource clipSource;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        private readonly ObservableCollection<T> points;

        private KStudioPlayback playback = null;
        private bool dirty = false;
    }
}
