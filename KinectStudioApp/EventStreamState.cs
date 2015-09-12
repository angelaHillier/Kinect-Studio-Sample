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
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Windows.Data;
    using Microsoft.Kinect.Tools;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;
    using System.Collections;

    public class EventStreamState : KStudioUserState, IEventLaneDataSource
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "kstudio")]
        public EventStreamState(KStudioEventStream stream, IKStudioService kstudioService, bool obscureStream)
        {
            DebugHelper.AssertUIThread();

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (kstudioService == null)
            {
                throw new ArgumentNullException("kstudioService");
            }

            this.stream = stream;
            this.kstudioService = kstudioService;
            this.obscureStream = obscureStream;

            this.ShortName = stream.Name;
            if (String.IsNullOrWhiteSpace(this.ShortName))
            {
                this.ShortName = stream.DataTypeName;
            }
            if (String.IsNullOrWhiteSpace(this.ShortName))
            {
                this.ShortName = stream.DataTypeId.ToString() + ":" + stream.SemanticId;
            }
            this.ShortNameUppercase = this.ShortName.ToUpperInvariant();

            StringBuilder sb = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(stream.DataTypeName))
            {
                sb.Append(stream.DataTypeName);
                sb.AppendLine();
            }
            if (!String.IsNullOrWhiteSpace(stream.Name))
            {
                sb.Append(stream.Name);
                sb.AppendLine();
            }
            sb.Append(stream.DataTypeId);
            sb.Append(":");
            sb.Append(stream.SemanticId);

            this.LongName = sb.ToString();
        }

        public IKStudioService KStudioService
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.kstudioService;
            }
        }

        public void SetDuration(TimeSpan sourceDuration)
        {
            DebugHelper.AssertUIThread();

            if ((sourceDuration >= TimeSpan.Zero) && (this.duration == 0))
            {
                this.duration = ((ulong)sourceDuration.Ticks * EventStreamState.cTimeSpanTicksToTimelineTicks) + EventStreamState.cLastEventDuration;

                EventHandler handler = timeRangeChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        public void SetupForPlayback(ReadOnlyObservableCollection<KStudioEventStream> clientEventStreams)
        {
            DebugHelper.AssertUIThread();

            if ((this.livePlaybackStreams == null) && (clientEventStreams != null))
            {
                ListCollectionView targetPlaybackableStreams = new ListCollectionView(clientEventStreams);
                targetPlaybackableStreams.Filter = (object item) =>
                    {
                        KStudioEventStream s = item as KStudioEventStream;
                        Debug.Assert(s != null);

                        return s.IsPlaybackable && s.IsFromService && (s.DataTypeId == this.stream.DataTypeId);
                    };
                targetPlaybackableStreams.SortDescriptions.Clear();
                targetPlaybackableStreams.SortDescriptions.Add(new SortDescription("UserState.ShortNameUppercase", ListSortDirection.Ascending));
                targetPlaybackableStreams.Refresh();

                this.livePlaybackStreams = targetPlaybackableStreams;
                this.RaisePropertyChanged("LivePlaybackStreams");
            }
        }

        public string Identifier
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.stream.DataTypeId + ":" + this.stream.SemanticId;
            }
        }

        public string ShortName { get; private set; }

        public string ShortNameUppercase { get; private set; }

        public string LongName { get; private set; }

        public IEventLaneNode SelectedNode
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.selectedNode;
            }
        }

        public bool IsObscureStream
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.obscureStream;
            }
        }

        public string RequirementsForTargetRecordingToolTip
        {
            get
            {
                DebugHelper.AssertUIThread();

                string value = null;
                if (this.kstudioService != null)
                {
                    value = EventStreamState.BuildRequirementsToolTip(this.stream, this.kstudioService.TargetRecordableStreams, KStudioEventStreamSelectorRequirementFlags.ProcessRecord);
                }

                if (!String.IsNullOrWhiteSpace(value))
                {
                    value = "\n\n" + value;
                }

                return this.LongName + value;
            }
        }

        public bool IsEnabledForTargetRecording
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                return this.targetRecordingEnabled; 
            }

            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.targetRecordingEnabled)
                {
                    this.targetRecordingEnabled = value;
                    this.RaisePropertyChanged("IsEnabledForTargetRecording");

                    if (!this.targetRecordingEnabled)
                    {
                        this.IsSelectedForTargetRecordingNoCheck = false;
                    }
                }
            }
        }

        public bool IsSelectedForTargetRecording
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                return this.targetRecordingSelected; 
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.kstudioService.DoTargetRecordingDance(this.stream, value))
                {
                    if (value != this.targetRecordingSelected)
                    {
                        this.targetRecordingSelected = value;
                        this.RaisePropertyChanged("IsSelectedForTargetRecording");
                    }
                }
            }
        }

        public bool IsSelectedForTargetRecordingNoCheck
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                return this.targetRecordingSelected; 
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.targetRecordingSelected)
                {
                    this.targetRecordingSelected = value;
                    this.RaisePropertyChanged("IsSelectedForTargetRecording");
                }
            }
        }

        public bool IsVisibleForTargetRecording
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.targetRecordingVisible;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.targetRecordingVisible)
                {
                    this.targetRecordingVisible = value;
                    this.RaisePropertyChanged("IsVisibleForTargetRecording");

                    if (this.kstudioService != null)
                    {
                        this.kstudioService.UpdateTargetRecordingVisibility(this.stream);
                    }
                }
            }
        }

        public string RequirementsForTargetPlaybackToolTip
        {
            get
            {
                DebugHelper.AssertUIThread();

                string value = null;

                if (this.selectedLivePlaybackStream != null)
                {
                    value = EventStreamState.BuildRequirementsToolTip(this.selectedLivePlaybackStream, this.kstudioService.PlaybackableFileStreams, KStudioEventStreamSelectorRequirementFlags.ProcessPlayback);
                }

                if (!String.IsNullOrWhiteSpace(value))
                {
                    value = "\n\n" + value;
                }

                return this.LongName + value;
            }
        }

        public bool IsEnabledForTargetPlayback
        {
            get 
            { 
                DebugHelper.AssertUIThread();
                return this.targetPlaybackEnabled; 
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.targetPlaybackEnabled)
                {
                    this.targetPlaybackEnabled = value;
                    this.RaisePropertyChanged("IsEnabledForTargetPlayback");

                    if (!this.targetPlaybackEnabled)
                    {
                        this.IsSelectedForTargetPlaybackNoCheck = false;
                    }
                }
            }
        }

        public bool IsSelectedForTargetPlayback
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                return this.targetPlaybackSelected; 
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.kstudioService.DoTargetPlaybackDance(this.stream, value))
                {
                    if (value != this.targetPlaybackSelected)
                    {
                        this.targetPlaybackSelected = value;
                        this.RaisePropertyChanged("IsSelectedForTargetPlayback");
                    }
                }
            }
        }

        public bool IsSelectedForTargetPlaybackNoCheck
        {
            get 
            { 
                DebugHelper.AssertUIThread();
                return this.targetPlaybackSelected; 
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.targetPlaybackSelected)
                {
                    this.targetPlaybackSelected = value;
                    this.RaisePropertyChanged("IsSelectedForTargetPlayback");
                }
            }
        }

        public bool IsVisibleForPlayback
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackVisible;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.playbackVisible)
                {
                    this.playbackVisible = value;
                    this.RaisePropertyChanged("IsVisibleForPlayback");

                    if (this.kstudioService != null)
                    {
                        this.kstudioService.UpdatePlaybackVisibility(this.stream);
                    }
                }
            }
        }

        public string RequirementsForTargetMonitorToolTip
        {
            get
            {
                DebugHelper.AssertUIThread();

                string value = null;

                if (this.kstudioService != null)
                {
                    value = EventStreamState.BuildRequirementsToolTip(this.stream, this.kstudioService.TargetMonitorableStreams, KStudioEventStreamSelectorRequirementFlags.ProcessMonitor);
                }

                if (!String.IsNullOrWhiteSpace(value))
                {
                    value = "\n\n" + value;
                }

                return this.LongName + value;
            }
        }

        public bool IsEnabledForTargetMonitor
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                return this.targetMonitorEnabled; 
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.targetMonitorEnabled)
                {
                    this.targetMonitorEnabled = value;
                    this.RaisePropertyChanged("IsEnabledForTargetMonitor");

                    if (!this.targetMonitorEnabled)
                    {
                        this.IsSelectedForTargetMonitorNoCheck = false;
                    }
                }
            }
        }

        public bool IsSelectedForTargetMonitor
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                return this.targetMonitorSelected; 
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.kstudioService.DoTargetMonitorDance(this.stream, value))
                {
                    if (value != this.targetMonitorSelected)
                    {
                        this.targetMonitorSelected = value;
                        this.RaisePropertyChanged("IsSelectedForTargetMonitor");
                    }
                }
            }
        }

        public bool IsSelectedForTargetMonitorNoCheck
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                return this.targetMonitorSelected; 
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.targetMonitorSelected)
                {
                    this.targetMonitorSelected = value;
                    this.RaisePropertyChanged("IsSelectedForTargetMonitor");
                }
            }
        }

        public bool IsVisibleForTargetMonitor
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.targetMonitorVisible;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.targetMonitorVisible)
                {
                    this.targetMonitorVisible = value;
                    this.RaisePropertyChanged("IsVisibleForTargetMonitor");

                    if (this.kstudioService != null)
                    {
                        this.kstudioService.UpdateTargetMonitorVisibility(this.stream);
                    }
                }
            }
        }

        public MetadataInfo Metadata
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                return this.metadata; 
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.metadata)
                {
                    this.metadata = value;
                    this.RaisePropertyChanged("Metadata");
                }
            }
        }

        public ICollectionView LivePlaybackStreams 
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.livePlaybackStreams;
            }
        }

        public KStudioEventStream SelectedFilePlaybackStream
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                return this.selectedFilePlaybackStream; 
            }
            private set
            {
                DebugHelper.AssertUIThread();

                if (value != this.selectedFilePlaybackStream)
                {
                    this.selectedFilePlaybackStream = value;
                    RaisePropertyChanged("SelectedFilePlaybackStream");
                }
            }
        }

        public KStudioEventStream SelectedLivePlaybackStream
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                return this.selectedLivePlaybackStream; 
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.selectedLivePlaybackStream)
                {
                    if (this.selectedLivePlaybackStream != null)
                    {
                        EventStreamState selectedLivePlaybackStreamState = this.selectedLivePlaybackStream.UserState as EventStreamState;
                        if (selectedLivePlaybackStreamState != null)
                        {
                            if (selectedLivePlaybackStreamState.SelectedFilePlaybackStream != null)
                            {
                                selectedLivePlaybackStreamState.SelectedFilePlaybackStream = null;
                                selectedLivePlaybackStreamState.IsSelectedForTargetPlayback = false;
                            }
                        }
                    }

                    this.selectedLivePlaybackStream = value;

                    if (this.selectedLivePlaybackStream != null)
                    {
                        EventStreamState selectedLivePlaybackStreamState = this.selectedLivePlaybackStream.UserState as EventStreamState;
                        if (selectedLivePlaybackStreamState != null)
                        {
                            selectedLivePlaybackStreamState.SelectedFilePlaybackStream = this.stream;
                        }
                    }

                    this.RaisePropertyChanged("SelectedLivePlaybackStream");
                    this.RaisePropertyChanged("RequirementsForTargetPlaybackToolTip");
                }
            }
        }

        public ulong MaxTime
        {
            get 
            {
                DebugHelper.AssertUIThread();

                ulong value = this.duration;

                if (value == 0)
                {
                    KStudioSeekableEventStream seekableStream = this.stream as KStudioSeekableEventStream;
                    if (seekableStream == null)
                    {
                        value = TimeSpan.TicksPerSecond * EventStreamState.cTimeSpanTicksToTimelineTicks;
                    }
                    else
                    {
                        value = ((ulong)seekableStream.EndRelativeTime.Ticks) * EventStreamState.cTimeSpanTicksToTimelineTicks + EventStreamState.cLastEventDuration;
                    }
                }

                return value;
            }
        }

        public ulong MinTime
        {
            get 
            {
                DebugHelper.AssertUIThread();

#if false
                ulong value;

                KStudioSeekableEventStream seekableStream = this.stream as KStudioSeekableEventStream;
                if (seekableStream == null)
                {
                    value = 0;
                }
                else
                {
                    value = (ulong)seekableStream.StartRelativeTime.Ticks * EventStreamState.cTimeSpanTicksToTimelineTicks;
                }

                return value;
#else
                return 0;
#endif
            }
        }

        public IEnumerable<IEventLaneNode> Nodes
        {
            get 
            { 
                DebugHelper.AssertUIThread();

                LoadEvents();

                return this.readOnlyEvents;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public bool UpdateTime(TimeSpan time, out uint foundIndex) 
        {
            DebugHelper.AssertUIThread();

            LoadEvents();

            bool result = false;
            foundIndex = 0;

            EventData newSelectedNode = null;
            if (this.events != null)
            {
                KStudioSeekableEventStream seekableStream = this.stream as KStudioSeekableEventStream;
                if (seekableStream != null)
                {
                    TimeSpan foundTime;

                    if (seekableStream.FindEvent(time, out foundIndex, out foundTime))
                    {
                        if ((foundIndex >= 0) && (foundIndex < this.events.Length))
                        {
                            newSelectedNode = this.events[(int)foundIndex];
                            result = true;
                        }
                    }
                }
            }

            this.UpdateSelection(newSelectedNode);

            return result;
        }

        public void ClearTime()
        {
            DebugHelper.AssertUIThread();

            this.UpdateSelection(null);
        }

        public IEventLaneNode FindNode(ulong time, ulong timeStride)
        {
            DebugHelper.AssertUIThread();

            LoadEvents();

            IEventLaneNode value = null;

            if ((this.events != null) && (this.events.Length > 0))
            {
                int i = Array.BinarySearch(this.events, time, new EventStreamState.CompareEventTime());

                if (i < 0)
                {
                    i = ~i;
                }

                if (i > 0)
                {
                    value = this.events[i - 1];
                }
            }

            return value;
        }

        public void OnNodeSelected(IEventLaneNode node)
        {
            DebugHelper.AssertUIThread();

            EventData eventData = node as EventData;

            if ((eventData != null) && (this.kstudioService != null) && this.kstudioService.HasPlaybackFile && (this.kstudioService.PlaybackState != KStudioPlaybackState.Playing))
            {
                if (this.UpdateSelection(eventData))
                {
                    TimeSpan time = TimeSpan.FromTicks((long)(eventData.StartTime / EventStreamState.cTimeSpanTicksToTimelineTicks));
                    this.kstudioService.SeekPlayback(time);
                }
            }
        }

        public void PopulateLaneRenderData(ulong startTime, ulong timeStride, IEventLaneNode[] columns)
        {
            DebugHelper.AssertUIThread();

            LoadEvents();

            if ((this.events != null) && (this.events.Length > 0) && (columns != null))
            {
                int i = Array.BinarySearch(this.events, startTime, new EventStreamState.CompareEventTime());

                if (i < 0)
                {
                    i = ~i;
                }

                int j = 0;

                if (i > 0)
                {
                    i--;
                }
                else
                {
                    while (j < columns.Length)
                    {
                        if (startTime >= this.events[i].StartTime)
                        {
                            break;
                        }

                        startTime += timeStride;
                        ++j;
                    }
                }

                while (j < columns.Length)
                {
                    while (startTime > this.events[i].StartTime + this.events[i].Duration)
                    {
                        ++i;
                        if (i >= this.events.Length)
                        {
                            return;
                        }
                    }

                    columns[j] = this.events[i];
                    startTime += timeStride;
                    ++j;
                }
            }
        }

#pragma warning disable 0067
        public event EventHandler RenderInvalidated;
#pragma warning restore 0067

        public IEnumerable<IEventLaneNode> SelectedNodes
        {
            get
            {
                DebugHelper.AssertUIThread();

                IEnumerable<IEventLaneNode> value;

                if (this.selectedNode == null)
                {
                    value = new IEventLaneNode[0];
                }
                else
                {
                    value = new IEventLaneNode[] { this.selectedNode };
                }

                return value;
            }
        }

        public event EventHandler SelectedNodesChanged
        {
            add
            {
                DebugHelper.AssertUIThread();

                this.selectedNodesChanged += value;
            }
            remove
            {
                DebugHelper.AssertUIThread();

                this.selectedNodesChanged -= value;
            }
        }

        public event EventHandler TimeRangeChanged
        {
            add
            {
                DebugHelper.AssertUIThread();

                this.timeRangeChanged += value;
            }
            remove
            {
                DebugHelper.AssertUIThread();

                this.timeRangeChanged -= value;
            }
        }

        private void LoadEvents()
        {
            if (this.events == null)
            {
                KStudioSeekableEventStream seekableStream = this.stream as KStudioSeekableEventStream;
                if (seekableStream != null)
                {
                    IReadOnlyList<KStudioEventHeader> eventHeaders = seekableStream.EventHeaders;

                    this.events = new EventData[eventHeaders.Count];

                    bool doFrameNumber = false;

                    if (seekableStream.TagSize >= sizeof(uint))
                    {
                        // At this time, assume if there is at least 4 bytes of tag data that it is a frame number
                        doFrameNumber = true;                        
                    }

                    int count = eventHeaders.Count;
                    if (count > 0)
                    {
                        ulong tick;
                        uint eventIndex;

                        {
                            KStudioEventHeader eventHeader = eventHeaders[0];
                            tick = (ulong)eventHeader.RelativeTime.Ticks * EventStreamState.cTimeSpanTicksToTimelineTicks;
                            eventIndex = eventHeader.EventIndex;
                        }

                        int lastIndex = count - 1;

                        for (int i = 0; i < lastIndex; ++i)
                        {
                            KStudioEventHeader eventHeader = eventHeaders[i + 1];
                            ulong nextTick = (ulong)eventHeader.RelativeTime.Ticks * EventStreamState.cTimeSpanTicksToTimelineTicks;
                            uint? frameNumber = null;

                            if (doFrameNumber)
                            {
                                uint bufferSize;
                                IntPtr bufferPtr;
                                eventHeader.AccessUnderlyingTagDataBuffer(out bufferSize, out bufferPtr);

                                Debug.Assert(bufferSize >= sizeof(uint));
                                unsafe
                                {
                                    frameNumber = *((uint*)bufferPtr.ToPointer());
                                }
                            }

                            EventData eventDataNode = new EventData((int)eventIndex, frameNumber, tick, nextTick - tick);
                            tick = nextTick;
                            eventIndex = eventHeader.EventIndex;

                            this.events[i] = eventDataNode;
                        }

                        {
                            KStudioEventHeader eventHeader = eventHeaders[lastIndex];
                            uint? frameNumber = null;

                            if (doFrameNumber)
                            {
                                uint bufferSize;
                                IntPtr bufferPtr;
                                eventHeader.AccessUnderlyingTagDataBuffer(out bufferSize, out bufferPtr);

                                Debug.Assert(bufferSize >= sizeof(uint));
                                unsafe
                                {
                                    frameNumber = *((uint*)bufferPtr.ToPointer());
                                }
                            }

                            ulong lastDuration;

                            if (this.duration == 0)
                            {
                                lastDuration = EventStreamState.cLastEventDuration;
                            }
                            else
                            {
                                lastDuration = this.duration - tick;
                            }

                            EventData eventDataNode = new EventData((int)eventIndex, frameNumber, tick, lastDuration);

                            this.events[lastIndex] = eventDataNode;
                        }

#if TODODEB
                        EventHandler handler = this.TimeRangeChanged;
                        if (handler != null)
                        {
                            handler(this, EventArgs.Empty);
                        }
                        handler = this.RenderInvalidated;
                        if (handler != null)
                        {
                            handler(this, EventArgs.Empty);
                        }
#endif
                    }
                }

                this.readOnlyEvents = Array.AsReadOnly(this.events);
            }
        }

        private bool UpdateSelection(EventData newSelectedNode)
        {
            DebugHelper.AssertUIThread();

            bool changed = false;

            if (newSelectedNode != this.selectedNode)
            {
                changed = true;

                if (this.selectedNode != null)
                {
                    this.selectedNode.IsSelected = false;
                }

                this.selectedNode = newSelectedNode;

                if (this.selectedNode != null)
                {
                    this.selectedNode.IsSelected = true;
                }

                EventHandler handler = this.selectedNodesChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }

                this.RaisePropertyChanged("SelectedNode");
            }

            return changed;
        }

        private KStudioEventStream stream;
        private IKStudioService kstudioService;
        private ulong duration = 0;
        private bool obscureStream = false;
        private bool targetRecordingEnabled = true;
        private bool targetRecordingSelected = false;
        private bool targetRecordingVisible = true;
        private bool targetPlaybackEnabled = true;
        private bool targetPlaybackSelected = false;
        private bool playbackVisible = true;
        private bool targetMonitorEnabled = true;
        private bool targetMonitorSelected = false;
        private bool targetMonitorVisible = true;
        private MetadataInfo metadata = null;
        private ListCollectionView livePlaybackStreams = null;
        private KStudioEventStream selectedLivePlaybackStream = null;
        private KStudioEventStream selectedFilePlaybackStream = null;
        private EventData[] events = null;
        private IReadOnlyList<IEventLaneNode> readOnlyEvents = null;
        private EventData selectedNode = null;
        private EventHandler selectedNodesChanged = null;
        private EventHandler timeRangeChanged = null;

        private static ulong cTimeSpanTicksToTimelineTicks = 100;
        private static readonly ulong cLastEventDuration = ((ulong)TimeSpan.FromSeconds(1.0 / 30.0).Ticks) * cTimeSpanTicksToTimelineTicks;

        private static string BuildRequirementsToolTip(KStudioEventStream stream, IEnumerable streams, KStudioEventStreamSelectorRequirementFlags process)
        {
            string value = null;

            process = process &= KStudioEventStreamSelectorRequirementFlags.ProcessMask;

            if ((stream != null) && (stream.EventStreamSelectorRequirements != null))
            {
                EventStreamState state = (EventStreamState)stream.UserState;

                StringBuilder sb = new StringBuilder();

                KStudioEventStreamIdentifier emptyIdentifier = new KStudioEventStreamIdentifier();

                foreach (KStudioEventStreamSelectorRequirement requirement in stream.EventStreamSelectorRequirements)
                {
                    if ((requirement.Flags & KStudioEventStreamSelectorRequirementFlags.ProcessMask) == process)
                    {
                        switch (requirement.Flags & KStudioEventStreamSelectorRequirementFlags.OperationMask)
                        {
                            case KStudioEventStreamSelectorRequirementFlags.OperationAll:
                                {
                                    string temp = EventStreamState.GetStreamNames((KStudioEventStreamIdentifier)requirement.Identifier0, emptyIdentifier, streams);
                                    if (!String.IsNullOrWhiteSpace(temp))
                                    {
                                        if (sb.Length > 0)
                                        {
                                            sb.AppendLine();
                                        }

                                        sb.AppendFormat(Strings.StreamRequirement_All_Format, temp);
                                    }
                                }
                                {
                                    string temp = EventStreamState.GetStreamNames((KStudioEventStreamIdentifier)requirement.Identifier1, emptyIdentifier, streams);
                                    if (!String.IsNullOrWhiteSpace(temp))
                                    {
                                        if (sb.Length > 0)
                                        {
                                            sb.AppendLine();
                                        }

                                        sb.AppendFormat(Strings.StreamRequirement_All_Format, temp);
                                    }
                                }
                                break;

                            case KStudioEventStreamSelectorRequirementFlags.OperationNone:
                                {
                                    string temp = EventStreamState.GetStreamNames((KStudioEventStreamIdentifier)requirement.Identifier0, (KStudioEventStreamIdentifier)requirement.Identifier1, streams);
                                    if (!String.IsNullOrWhiteSpace(temp))
                                    {
                                        if (sb.Length > 0)
                                        {
                                            sb.AppendLine();
                                        }

                                        sb.AppendFormat(Strings.StreamRequirement_None_Format, temp);
                                    }
                                }
                                break;

                            case KStudioEventStreamSelectorRequirementFlags.OperationOr:
                                {
                                    string temp = EventStreamState.GetStreamNames((KStudioEventStreamIdentifier)requirement.Identifier0, (KStudioEventStreamIdentifier)requirement.Identifier1, streams);
                                    if (!String.IsNullOrWhiteSpace(temp))
                                    {
                                        if (sb.Length > 0)
                                        {
                                            sb.AppendLine();
                                        }

                                        sb.AppendFormat(Strings.StreamRequirement_Or_Format, temp);
                                    }
                                }
                                break;

                            case KStudioEventStreamSelectorRequirementFlags.OperationXor:
                                {
                                    string temp = EventStreamState.GetStreamNames((KStudioEventStreamIdentifier)requirement.Identifier0, (KStudioEventStreamIdentifier)requirement.Identifier1, streams);
                                    if (!String.IsNullOrWhiteSpace(temp))
                                    {
                                        if (sb.Length > 0)
                                        {
                                            sb.AppendLine();
                                        }

                                        sb.AppendFormat(Strings.StreamRequirement_Xor_Format, temp);
                                    }
                                }
                                break;
                        }
                    }
                }

                value = sb.ToString();
            }

            return value;
        }

        private static string GetStreamNames(KStudioEventStreamIdentifier id0, KStudioEventStreamIdentifier id1, IEnumerable streams)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 2; ++i)
            {
                KStudioEventStreamIdentifier id = i == 0 ? id0 : id1;

                if (id.DataTypeId != Guid.Empty)
                {
                    if (streams == null)
                    {
                        if (sb.Length > 0)
                        {
                            sb.AppendLine();
                        }

                        if (id.SemanticId == Guid.Empty)
                        {
                            sb.AppendFormat(CultureInfo.CurrentCulture, Strings.StreamRequirement_Unavailable_Type_Format, id.DataTypeId);
                        }
                        else
                        {
                            sb.AppendFormat(CultureInfo.CurrentCulture, Strings.StreamRequirement_Unavailable_Identifier_Format, id.DataTypeId, id.SemanticId);
                        }
                    }
                    else
                    {
                        foreach (KStudioEventStream stream in streams)
                        {
                            if ((stream.DataTypeId == id.DataTypeId) && (id.SemanticId == Guid.Empty) ||
                                (stream.DataTypeId == id.DataTypeId) && (stream.SemanticId == id.SemanticId))
                            {
                                if (sb.Length > 0)
                                {
                                    sb.AppendLine();
                                }

                                StringBuilder sbName = new StringBuilder();
                                if (!String.IsNullOrWhiteSpace(stream.DataTypeName))
                                {
                                    sbName.Append(stream.DataTypeName);
                                    sbName.Append(" - ");
                                }
                                if (!String.IsNullOrWhiteSpace(stream.Name))
                                {
                                    sbName.Append(stream.Name);
                                    sbName.Append(" - ");
                                }
                                sbName.Append(stream.DataTypeId);
                                sbName.Append(":");
                                sbName.Append(stream.SemanticId);

                                sb.AppendFormat(CultureInfo.CurrentCulture, Strings.StreamRequirement_Unavailable_Name_Format, sbName.ToString());
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private class CompareEventTime : IComparer
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
            public int Compare(object x, object y)
            {
                Debug.Assert(x is IEventLaneNode);
                Debug.Assert(y is ulong);

                return ((IEventLaneNode)x).StartTime.CompareTo((ulong)y);
            }
        }

        private class EventData : IEventLaneNode
        {
            public EventData(int index, uint? frameNumber, ulong startTimeTicks, ulong durationTicks)
            {
                this.index = index;
                this.frameNumber = frameNumber;
                this.startTime = startTimeTicks;
                this.duration = durationTicks;
                this.selected = false;
            }

            public IEnumerable<IEventLaneNode> Children
            {
                get
                {
                    return null;
                }
            }

            public uint Color
            {
                get
                {
                    return this.selected ? 0xFF00FF00 : 0xFF008F00;
                }
            }

            public ulong Duration
            {
                get
                {
                    return this.duration;
                }
            }

            public bool HasChildren
            {
                get
                {
                    return false;
                }
            }

            public string Name
            {
                get
                {
                    string value;

                    if (this.frameNumber.HasValue)
                    {
                        value = String.Format(CultureInfo.CurrentCulture, Strings.TimelineEvent_EventName_WithFrameFormat, this.index, this.frameNumber.Value);
                    }
                    else
                    {
                        value = String.Format(CultureInfo.CurrentCulture, Strings.TimelineEvent_EventName_Format, this.index);
                    }

                    return value;
                }
            }

            public IEventLaneNode Parent
            {
                get 
                {
                    return null;
                }
            }

            public ulong StartTime
            {
                get
                {
                    return this.startTime;
                }
            }

            public EventRenderStyle Style
            {
                get
                {
                    return EventRenderStyle.Normal;
                }
            }

            public object ToolTip
            {
                get 
                {
                    TimeSpan startRelativeTime = TimeSpan.FromTicks((long)(this.startTime / EventStreamState.cTimeSpanTicksToTimelineTicks));
                    TimeSpan durationTime = TimeSpan.FromTicks((long)(this.duration / EventStreamState.cTimeSpanTicksToTimelineTicks));
                    return String.Format(CultureInfo.CurrentCulture, Strings.TimelineEvent_EventToolTip_Format,
                        startRelativeTime, durationTime, this.Name);
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public bool IsSelected
            {
                get
                {
                    return this.selected;
                }
                set
                {
                    this.selected = value;
                }
            }

            private int index;
            private uint? frameNumber;
            private ulong startTime;
            private ulong duration;
            private bool selected;
        }
    }
}
