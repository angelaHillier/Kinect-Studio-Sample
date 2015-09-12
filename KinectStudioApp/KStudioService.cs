//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Kinect.Tools;
using Microsoft.Xbox.Tools.Shared;
using KinectStudioPlugin;
using KinectStudioUtility;

namespace KinectStudioApp
{
    // This is essentially the ViewModel for the application.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public class KStudioService : KStudioUserState, IKStudioService, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public KStudioService()
        {
            DebugHelper.AssertUIThread();

            KStudio.KStudioObjectInitializer = ObjectInitializer;

            this.settings = new KStudioServiceSettings();
            this.settings.PropertyChanged += (source, e) =>
            {
                if ((e != null) && (e.PropertyName == "AdvancedModeObscureStreams"))
                {
                    this.RaisePropertyChanged("TargetMonitorableStreamsTitle");
                    this.RaisePropertyChanged("TargetMonitorableStreamsToolTip");
                    this.RaisePropertyChanged("TargetRecordableStreamsTitle");
                    this.RaisePropertyChanged("TargetRecordableStreamsToolTip");
                    this.RaisePropertyChanged("PlaybackableStreamsTitle");
                    this.RaisePropertyChanged("PlaybackableStreamsToolTip");
                }
            };

            this.monitorTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(250),
            };
            this.monitorTimer.Tick += MonitorTimer_Tick;

            this.targetAddress = IPAddress.Loopback;
            this.targetAlias = Environment.MachineName;

            this.client = KStudio.CreateClient(KStudioClientFlags.ProcessNotifications);
            this.client.PropertyChanged += this.Client_PropertyChanged;
            this.client.EventDataAvailable += this.Client_EventDataAvailable;

            // recordable streams

            ListCollectionView targetRecordableStreams = new ListCollectionView(client.EventStreams);
            targetRecordableStreams.Filter = (object item) =>
            {
                KStudioEventStream stream = item as KStudioEventStream;
                Debug.Assert(stream != null);

                return stream.IsRecordable && stream.IsFromService;
            };
            targetRecordableStreams.SortDescriptions.Clear();
            targetRecordableStreams.SortDescriptions.Add(new SortDescription("UserState.ShortNameUppercase", ListSortDirection.Ascending));
            targetRecordableStreams.Refresh();

            this.targetRecordableStreams = targetRecordableStreams;

            // playbackable streams

            ListCollectionView targetPlaybackableStreams = new ListCollectionView(client.EventStreams);
            targetPlaybackableStreams.Filter = (object item) =>
            {
                KStudioEventStream stream = item as KStudioEventStream;
                Debug.Assert(stream != null);

                return stream.IsPlaybackable && stream.IsFromService;
            };
            targetPlaybackableStreams.SortDescriptions.Clear();
            targetPlaybackableStreams.SortDescriptions.Add(new SortDescription("UserState.ShortNameUppercase", ListSortDirection.Ascending));
            targetPlaybackableStreams.Refresh();

            this.targetPlaybackableStreams = targetPlaybackableStreams;

            // monitorable streams

            ListCollectionView targetMonitorableStreams = new ListCollectionView(client.EventStreams);
            targetMonitorableStreams.Filter = (object item) =>
            {
                KStudioEventStream stream = item as KStudioEventStream;
                Debug.Assert(stream != null);

                return stream.IsMonitor && stream.IsFromService;
            };
            targetMonitorableStreams.SortDescriptions.Clear();
            targetMonitorableStreams.SortDescriptions.Add(new SortDescription("UserState.ShortNameUppercase", ListSortDirection.Ascending));
            targetMonitorableStreams.Refresh();

            this.targetMonitorableStreams = targetMonitorableStreams;
        }

        ~KStudioService()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            if (serviceProvider != null)
            {
                ISessionStateService sessionStateService = serviceProvider.GetService(typeof(ISessionStateService)) as ISessionStateService;
                if (sessionStateService != null)
                {
                    sessionStateService.DeclareSessionStateVariable("KStudioServiceSettings", this.settings);
                }

                this.lastRecordedStreams = new LastSelectedStreams(serviceProvider, "RecordedStreams");

                this.lastMonitoredStreams = new LastSelectedStreams(serviceProvider, "MonitoredStreams");

                this.lastHiddenRecordableStreams = new LastSelectedStreams(serviceProvider, "HiddenRecordableStreams");

                this.lastHiddenMonitorableStreams = new LastSelectedStreams(serviceProvider, "HiddenMonitorableStreams");

                this.lastHiddenPlaybackableStreams = new LastSelectedStreams(serviceProvider, "HiddenPlaybackableStreams");

                this.loggingService = serviceProvider.GetService(typeof(ILoggingService)) as ILoggingService;

                this.notificationService = serviceProvider.GetService(typeof(IUserNotificationService)) as IUserNotificationService;

                this.mruService = serviceProvider.GetService(typeof(IMostRecentlyUsedService)) as IMostRecentlyUsedService;

                this.fileSettingsService = serviceProvider.GetService(typeof(IFileSettingsService)) as IFileSettingsService;

                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;

                this.metadataViewService = serviceProvider.GetService(typeof(IMetadataViewService)) as IMetadataViewService;
            }

            this.threadStartEvent = new AutoResetEvent(false);
            this.threadDoneEvent = new ManualResetEvent(false);

            this.thread = new Thread(new ThreadStart(this.RunThread))
            {
                Name = "KStudioService",
            };
            this.thread.Start();

            this.threadDoneEvent.WaitOne();


            if (this.settings.AutoTargetConnectOnStartup && (this.targetAddress != null))
            {
                this.ConnectToTarget(this.targetAddress, this.targetAlias);
            }
        }

        public event EventHandler PlaybackOpened;
        public event EventHandler<BusyEventArgs> Busy;

        private void Client_EventDataAvailable(object sender, KStudioEventDataEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.pluginService != null))
            {
                if (e.IsFromService)
                {
                    this.pluginService.HandleEvent(EventType.Monitor, e.EventData);

                    if (this.playback != null)
                    {
                        this.pluginService.HandleEvent(EventType.Inspection, e.EventData);

                        this.UpdatePlaybackStreamsTimeline(e.EventData.RelativeTime, false, false);
                    }
                }
                else
                {
                    this.pluginService.HandleEvent(EventType.Inspection, e.EventData);

                    if (this.playbackFile != null)
                    {
                        foreach (KStudioEventStream stream in this.playbackFile.EventStreams)
                        {
                            if ((stream.DataTypeId == e.EventData.EventStreamDataTypeId) && (stream.SemanticId == e.EventData.EventStreamSemanticId))
                            {
                                uint eventIndex;

                                EventStreamState ess = (EventStreamState)stream.UserState;

                                if (ess != null)
                                {
                                    ess.UpdateTime(e.EventData.RelativeTime, out eventIndex);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void Shutdown()
        {
            DebugHelper.AssertUIThread();

            this.DisconnectFromTarget();

            if (this.client != null)
            {
                this.client.PropertyChanged -= this.Client_PropertyChanged;
                this.client.EventDataAvailable -= this.Client_EventDataAvailable;
                this.client.Dispose();
                this.client = null;
            }

            if (this.thread != null)
            {
                try
                {
                    if (this.threadDoneEvent != null)
                    {
                        this.threadDoneEvent.WaitOne();
                    }

                    this.threadAction = null;

                    if (this.threadStartEvent != null)
                    {
                        this.threadStartEvent.Set();
                    }

                    if (this.threadDoneEvent != null)
                    {
                        this.threadDoneEvent.WaitOne();
                    }

                    if (this.threadStartEvent != null)
                    {
                        this.threadStartEvent.Dispose();
                        this.threadStartEvent = null;
                    }

                    if (this.threadDoneEvent != null)
                    {
                        this.threadDoneEvent.Dispose();
                        this.threadDoneEvent = null;
                    }

                    this.thread = null;
                }
                catch (Exception ex)
                {
                    if (this.loggingService != null)
                    {
                        this.loggingService.LogException(ex);
                    }
                }
            }
        }

        public string TargetFilePath
        {
            get
            {
                return settings.TargetFilePath;
            }
        }

        public IPAddress TargetAddress
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.targetAddress;
            }
            private set
            {
                DebugHelper.AssertUIThread();

                if (this.targetAddress != value)
                {
                    if ((this.client != null) && this.client.IsServiceConnected)
                    {
                        throw new InvalidOperationException("cannot change target address while target is connected");
                    }

                    this.targetAddress = value;
                    RaisePropertyChanged("TargetAddress");
                }
            }
        }

        public string TargetAlias
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.targetAlias;
            }
            private set
            {
                DebugHelper.AssertUIThread();

                if (this.targetAlias != value)
                {
                    if ((this.client != null) && this.client.IsServiceConnected)
                    {
                        throw new InvalidOperationException("cannot change target alias while target is connected");
                    }

                    this.targetAlias = value;
                    RaisePropertyChanged("TargetAlias");
                }
            }
        }

        public KStudioServiceSettings Settings
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.settings;
            }

        }

        public KStudioClient Client
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.client;
            }
        }

        public bool IsTargetConnected
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.connectedState;
            }
            private set
            {
                DebugHelper.AssertUIThread();

                if (this.connectedState != value)
                {
                    this.connectedState = value;
                    RaisePropertyChanged("IsTargetConnected");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "targetAddress"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "targetAlias")]
        public bool ConnectToTarget(IPAddress targetAddress, string targetAlias)
        {
            DebugHelper.AssertUIThread();

            bool result = false;

            if (targetAddress == null)
            {
                throw new ArgumentNullException("targetAddress");
            }

            if (targetAlias == null)
            {
                targetAlias = String.Format(CultureInfo.CurrentCulture, Strings.TargetAlias_Unknown, targetAddress.ToString());
            }

            if (this.client != null)
            {
                if (this.client.IsServiceConnected && (targetAddress.Equals(this.targetAddress)) && (targetAlias == this.targetAlias))
                {
                    return true;
                }

                this.DisconnectFromTarget();

                this.TargetAlias = targetAlias;
                this.TargetAddress = targetAddress;

                try
                {
                    this.client.ConnectToService();
                    result = true;
                }
                catch (Exception ex)
                {
                    result = false;

                    this.HandleConnectError(ex, this.targetAddress);
                }
            }

            if (result)
            {
                HashSet<KStudioEventStreamIdentifier> lastRecorded = this.lastRecordedStreams.HashSet;
                if (lastRecorded.Count == 0)
                {
                    lastRecorded = this.defaultRecordingStreams;
                }

                HashSet<KStudioEventStreamIdentifier> lastHiddenRecordable = this.lastHiddenRecordableStreams.HashSet;
                if (lastHiddenRecordable.Count == 0)
                {
                    lastHiddenRecordable = this.defaultHiddenRecordableStreams;
                }

                foreach (KStudioEventStream stream in this.targetRecordableStreams)
                {
                    KStudioEventStreamIdentifier identifier = new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId);

                    EventStreamState eventStreamState = stream.UserState as EventStreamState;
                    if (eventStreamState != null)
                    {
                        if (lastRecorded.Contains(identifier))
                        {
                            eventStreamState.IsSelectedForTargetRecording = this.DoTargetRecordingDance(stream, true);
                        }

                        eventStreamState.IsVisibleForTargetRecording = !lastHiddenRecordable.Contains(identifier);
                    }
                }

                HashSet<KStudioEventStreamIdentifier> lastMonitored = this.lastMonitoredStreams.HashSet;
                if (lastMonitored.Count == 0)
                {
                    lastMonitored = this.defaultMonitorStreams;
                }

                HashSet<KStudioEventStreamIdentifier> lastHiddenMonitorable = this.lastHiddenMonitorableStreams.HashSet;
                if (lastHiddenMonitorable.Count == 0)
                {
                    lastHiddenMonitorable = this.defaultHiddenMonitorableStreams;
                }

                foreach (KStudioEventStream stream in this.targetMonitorableStreams)
                {
                    KStudioEventStreamIdentifier identifier = new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId);

                    EventStreamState eventStreamState = stream.UserState as EventStreamState;
                    if (eventStreamState != null)
                    {
                        if (lastMonitored.Contains(identifier))
                        {
                            eventStreamState.IsSelectedForTargetMonitor = this.DoTargetMonitorDance(stream, true);
                        }

                        eventStreamState.IsVisibleForTargetMonitor = !lastHiddenMonitorable.Contains(identifier);
                    }
                }

                if ((this.settings != null) && this.settings.AutoMonitorOnTargetConnect)
                {
                    this.StartMonitor();
                }
            }

            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "targetAlias")]
        public bool ConnectToTarget(string targetAlias)
        {
            DebugHelper.AssertUIThread();

            if (String.IsNullOrWhiteSpace(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            bool result = false;

            IPAddress address = null;
            address = IPAddress.Loopback;

            if (address != null)
            {
                result = this.ConnectToTarget(address, targetAlias);
            }

            return result;
        }

        public void DisconnectFromTarget()
        {
            DebugHelper.AssertUIThread();

            this.CloseRecording(false);
            if (this.HasPlaybackFile && this.IsPlaybackFileOnTarget)
            {
                this.ClosePlayback();
            }
            this.StopMonitor();

            if (this.client != null)
            {
                this.client.DisconnectFromService();
            }
        }

        public bool CanRecord
        {
            get
            {
                DebugHelper.AssertUIThread();
                return this.connectedState && (this.recording == null) && (this.playback == null) && this.selectedTargetStreamsRecordable;
            }
        }

        public bool HasRecording
        {
            get
            {
                DebugHelper.AssertUIThread();
                return this.recording != null;
            }
        }

        public KStudioRecordingState RecordingState
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.recordingState;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.recordingState != value)
                {
                    this.recordingState = value;
                    RaisePropertyChanged("CanRecord");
                    RaisePropertyChanged("HasRecording");
                    RaisePropertyChanged("RecordingState");
                }
            }
        }

        public TimeSpan RecordingTime
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.recordingTime;
            }
            private set
            {
                if (this.recordingTime != value)
                {
                    this.recordingTime = value;
                    RaisePropertyChanged("RecordingTime");
                }
            }
        }

        public UInt64 RecordingFileSizeBytes
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.recordingFileSize;
            }
            private set
            {
                if (this.recordingFileSize != value)
                {
                    this.recordingFileSize = value;
                    RaisePropertyChanged("RecordingFileSizeBytes");
                }
            }
        }

        public UInt32 RecordingBufferSizeMegabytes
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.recordingBufferSize;
            }
            private set
            {
                if (this.recordingBufferSize != value)
                {
                    this.recordingBufferSize = value;
                    RaisePropertyChanged("RecordingBufferSizeMegabytes");
                }
            }
        }

        public UInt32 RecordingBufferInUseMegabytes
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.recordingInUseSize;
            }
            private set
            {
                if (this.recordingInUseSize != value)
                {
                    this.recordingInUseSize = value;
                    RaisePropertyChanged("RecordingBufferInUseMegabytes");
                }
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void StartRecording()
        {
            DebugHelper.AssertUIThread();

            this.ClosePlayback();

            if ((this.client != null) && (this.recording == null) && (this.playback == null))
            {
                bool rawRecording = false;

                KStudioEventStreamSelectorCollection streamSelectorCollection = new KStudioEventStreamSelectorCollection();
                foreach (KStudioEventStream s in this.targetRecordableStreams.OfType<KStudioEventStream>().Where(s => ((EventStreamState)s.UserState).IsSelectedForTargetRecording))
                {
                    if (s.DataTypeId == KStudioEventStreamDataTypeIds.RawIr)
                    {
                        rawRecording = true;
                    }

                    streamSelectorCollection.Add(s.DataTypeId, s.SemanticId);
                }

                string filePath = null;

                try
                {
                    if ((this.settings != null) && this.settings.AutoStopMonitorOnRecord)
                    {
                        this.StopMonitor();
                    }

                    this.RecordingTime = TimeSpan.Zero;

                    KStudioRecordingFlags flags = KStudioRecordingFlags.GenerateFileName | KStudioRecordingFlags.IgnoreOptionalStreams;
                    if (rawRecording)
                    {
                        flags |= KStudioRecordingFlags.XrfFileName;
                    }

                    this.recording = this.client.CreateRecording(this.TargetFilePath, streamSelectorCollection, this.settings.RecordingBufferSizeMB, flags);

                    filePath = recording.FilePath;

                    this.RecordingState = this.recording.State;
                    this.RecordingFilePath = filePath;
                    this.RecordingFileSizeBytes = this.recording.FileSizeBytes;
                    this.RecordingBufferSizeMegabytes = this.recording.BufferSizeMegabytes;
                    this.RecordingBufferInUseMegabytes = this.recording.BufferInUseSizeMegabytes;

                    this.RecordingMetadata = new MetadataInfo(false, Strings.RecordingMetadata_Title,
                        this.recordingFilePath,
                        new WritableMetadataProxy(null, this.recording.GetMetadata(KStudioMetadataType.Public)),
                        new WritableMetadataProxy(null, this.recording.GetMetadata(KStudioMetadataType.PersonallyIdentifiableInformation)));

                    foreach (KStudioEventStream s in this.targetRecordableStreams.OfType<KStudioEventStream>())
                    {
                        EventStreamState streamState = s.UserState as EventStreamState;
                        if (streamState != null)
                        {
                            if (streamState.IsSelectedForTargetRecording)
                            {
                                streamState.Metadata = new MetadataInfo(false, streamState.ShortName,
                                    this.recordingFilePath + "\n" + streamState.LongName,
                                    new WritableMetadataProxy(null, this.recording.GetMetadataForStream(KStudioMetadataType.Public, s.DataTypeId, s.SemanticId)),
                                    new WritableMetadataProxy(null, this.recording.GetMetadataForStream(KStudioMetadataType.PersonallyIdentifiableInformation, s.DataTypeId, s.SemanticId)));

                                this.lastRecordedStreams.HashSet.Add(new KStudioEventStreamIdentifier(s.DataTypeId, s.SemanticId));
                            }
                            else
                            {
                                this.lastRecordedStreams.HashSet.Remove(new KStudioEventStreamIdentifier(s.DataTypeId, s.SemanticId));
                            }
                        }
                    }

                    this.recording.PropertyChanged += this.Recording_PropertyChanged;

                    this.SetDefaultMetadataViews(this.recordingMetadata);

                    KStudioRecording temp = this.recording;
                    this.RunAsync(() => temp.Start());
                }
                catch (Exception ex)
                {
                    this.CloseRecording(false);

                    this.HandleFileCreateError(ex, filePath);
                }
            }
        }

        public void StopRecording()
        {
            DebugHelper.AssertUIThread();

            this.CloseRecording(true);
        }

        public void CloseRecording(bool allowAutoPlayback)
        {
            DebugHelper.AssertUIThread();

            if (this.recording != null)
            {
                string filePath = this.recording.FilePath;

                if (!Path.IsPathRooted(filePath))
                {
                    filePath = Path.Combine(this.TargetFilePath, filePath);
                }

                HashSet<MetadataInfo> metadataViewsToClose = new HashSet<MetadataInfo>();
                if (this.recordingMetadata != null)
                {
                    metadataViewsToClose.Add(this.recordingMetadata);
                }

                foreach (KStudioEventStream s in this.targetRecordableStreams.OfType<KStudioEventStream>())
                {
                    EventStreamState streamState = s.UserState as EventStreamState;
                    if ((streamState != null) & (streamState.Metadata != null))
                    {
                        metadataViewsToClose.Add(streamState.Metadata);
                        streamState.Metadata = null;
                    }
                }

                this.CloseMetadataViews(metadataViewsToClose);

                KStudioRecording temp = this.recording;
                this.RunAsync(() => temp.Stop());

                Dispatcher uiDispatcher = Dispatcher.CurrentDispatcher;

                this.RaiseBusy(true);

                // do the Dispose on another thread so that the buffer dump does not block the UI thread
                Thread thread = new Thread(() =>
                    {
                        while (temp.BufferInUseSizeMegabytes > 0)
                        {
                            Thread.Sleep(0);

                            uiDispatcher.Invoke(new Action(() =>
                                {
                                    this.RecordingBufferInUseMegabytes = temp.BufferInUseSizeMegabytes;
                                    this.recordingFileSize = temp.FileSizeBytes;
                                }));
                        }

                        temp.Dispose();
                        temp = null;

                        try
                        {
                            uiDispatcher.Invoke(new Action(() =>
                                {
                                    this.recording.PropertyChanged -= this.Recording_PropertyChanged;
                                    this.recording = null;
                                    this.RecordingMetadata = null;
                                    this.RecordingFilePath = null;
                                    this.RecordingTime = TimeSpan.Zero;
                                    this.RecordingFileSizeBytes = 0;
                                    this.RecordingBufferSizeMegabytes = 0;
                                    this.RecordingBufferInUseMegabytes = 0;
                                    this.RecordingState = KStudioRecordingState.Error;

                                    if (allowAutoPlayback && this.Settings.AutoPlaybackOnRecordStop)
                                    {
                                        DispatcherTimer timer = new DispatcherTimer()
                                            {
                                                Interval = TimeSpan.FromMilliseconds(500),
                                            };

                                        timer.Tick += (s, e) =>
                                            {
                                                DebugHelper.AssertUIThread();

                                                try
                                                {
                                                    timer.Stop();

                                                    CommandManager.InvalidateRequerySuggested();

                                                    if ((this.recording == null) && (this.playbackFile == null))
                                                    {
                                                        this.OpenTargetPlayback(filePath, true);
                                                    }

                                                    CommandManager.InvalidateRequerySuggested();

                                                    timer = null;
                                                }
                                                finally
                                                {
                                                    this.RaiseBusy(false);
                                                }
                                            };

                                        timer.Start();
                                    }
                                    else
                                    {
                                        this.RaiseBusy(false);
                                    }
                                }));
                        }
                        catch (Exception)
                        {
                            // ignore
                        }
                    });
                thread.IsBackground = true;
                thread.Priority = ThreadPriority.Lowest;
                thread.Start();
            }
        }

        public string TargetRecordableStreamsTitle
        {
            get
            {
                DebugHelper.AssertUIThread();

                int countVisible = 0;
                int countHidden = 0;

                bool allVisible = this.areAllStreamsVisibleForRecording;
                bool advanced = this.settings.AdvancedModeObscureStreams;

                foreach (KStudioEventStream stream in this.targetRecordableStreams)
                {
                    EventStreamState state = stream.UserState as EventStreamState;
                    if (state != null)
                    {
                        if (!state.IsObscureStream || advanced)
                        {
                            if (allVisible || state.IsVisibleForTargetRecording)
                            {
                                countVisible++;
                            }
                            else
                            {
                                countHidden++;
                            }
                        }
                    }
                }

                return String.Format(CultureInfo.CurrentCulture, Strings.RecordView_Header_Format, (countVisible + countHidden), countVisible, countHidden);
            }
        }

        public string TargetRecordableStreamsToolTip
        {
            get
            {
                DebugHelper.AssertUIThread();

                int countObscure = 0;

                if (this.settings.AdvancedModeObscureStreams)
                {
                    foreach (KStudioEventStream stream in this.targetRecordableStreams)
                    {
                        EventStreamState state = stream.UserState as EventStreamState;
                        if (state != null)
                        {
                            if (state.IsObscureStream)
                            {
                                countObscure++;
                            }
                        }
                    }
                }

                return (countObscure == 0) ? null : String.Format(CultureInfo.CurrentCulture, Strings.RecordView_ToolTip_Format, countObscure);
            }
        }

        public ICollectionView TargetRecordableStreams
        {
            get
            {
                return this.targetRecordableStreams;
            }
        }

        public MetadataInfo RecordingMetadata
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.recordingMetadata;
            }

            private set
            {
                DebugHelper.AssertUIThread();

                if (this.recordingMetadata != value)
                {
                    this.recordingMetadata = value;
                    RaisePropertyChanged("RecordingMetadata");
                }
            }
        }

        public string RecordingFilePath
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.recordingFilePath;
            }

            private set
            {
                DebugHelper.AssertUIThread();

                if (this.recordingFilePath != value)
                {
                    this.recordingFilePath = value;
                    RaisePropertyChanged("RecordingFilePath");
                }
            }
        }

        public bool CanPlayback
        {
            get
            {
                DebugHelper.AssertUIThread();
                return (this.recording == null) && (this.playback == null) && (this.playbackFile != null) &&
                    (this.playbackFileLocal || (this.connectedState && this.selectedTargetStreamsPlaybackable));
            }
        }

        public bool HasPlaybackFile
        {
            get
            {
                DebugHelper.AssertUIThread();
                return this.playbackFile != null;
            }
        }

        public bool IsPlaybackFileReadOnly
        {
            get
            {
                DebugHelper.AssertUIThread();
                return (this.playbackFile != null) && !(this.playbackFile is KStudioWritableEventFile);
            }
        }

        public bool IsPlaybackFileOnTarget
        {
            get
            {
                DebugHelper.AssertUIThread();
                return !this.playbackFileLocal;
            }
        }

        public bool HasPlayback
        {
            get
            {
                DebugHelper.AssertUIThread();
                return this.playback != null;
            }
        }

        public KStudioPlaybackState PlaybackState
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackState;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.playbackState != value)
                {
                    this.playbackState = value;

                    if ((this.playbackState == KStudioPlaybackState.Stopped) && (this.playback != null))
                    {
                        this.StopPlayback();
                    }

                    RaisePropertyChanged("CanPlayback");
                    RaisePropertyChanged("HasPlayback");
                    RaisePropertyChanged("PlaybackState");

                    switch (this.playbackState)
                    {
                        case KStudioPlaybackState.Stopped:
                        case KStudioPlaybackState.Idle:
                        case KStudioPlaybackState.Error:
                            this.PlaybackTime = TimeSpan.Zero;
                            this.PlaybackLoopIteration = 0;
                            break;

                        case KStudioPlaybackState.Paused:
                        case KStudioPlaybackState.Playing:
                            if (this.playback != null)
                            {
                                this.PlaybackTime = this.playback.CurrentRelativeTime;
                            }
                            break;
                    }
                }
            }
        }

        public bool AreAllStreamsVisibleForRecording
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.areAllStreamsVisibleForRecording;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.areAllStreamsVisibleForRecording != value)
                {
                    this.areAllStreamsVisibleForRecording = value;

                    this.RaisePropertyChanged("AreAllStreamsVisibleForRecording");
                    this.RaisePropertyChanged("TargetRecordableStreamsTitle");
                    this.RaisePropertyChanged("TargetRecordableStreamsToolTip");

                }
            }
        }

        public TimeSpan PlaybackTime
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackTime;
            }
            private set
            {
                DebugHelper.AssertUIThread();

                if (this.playbackTime != value)
                {
                    this.playbackTime = value;
                    RaisePropertyChanged("PlaybackTime");
                }
            }
        }

        public string PlaybackableStreamsTitle
        {
            get
            {
                DebugHelper.AssertUIThread();

                int countTotal = 0;
                int countVisible = 0;
                int countHidden = 0;

                bool allVisible = this.areAllStreamsVisibleForPlayback;
                bool advanced = this.settings.AdvancedModeObscureStreams;

                if (this.playbackFile != null)
                {
                    countTotal = this.playbackFile.EventStreams.Count;

                    foreach (KStudioEventStream stream in this.playbackFile.EventStreams)
                    {
                        EventStreamState state = stream.UserState as EventStreamState;
                        if (state != null)
                        {
                            if (!state.IsObscureStream || advanced)
                            {
                                if (allVisible || state.IsVisibleForPlayback)
                                {
                                    countVisible++;
                                }
                                else
                                {
                                    countHidden++;
                                }
                            }
                        }
                    }
                }

                return String.Format(CultureInfo.CurrentCulture, Strings.PlaybackView_Header_Format, countTotal, countVisible, countHidden);
            }
        }

        public string PlaybackableStreamsToolTip
        {
            get
            {
                DebugHelper.AssertUIThread();

                int countObscure = 0;

                if ((this.playbackFile != null) && this.settings.AdvancedModeObscureStreams)
                {
                    foreach (KStudioEventStream stream in this.playbackFile.EventStreams)
                    {
                        EventStreamState state = stream.UserState as EventStreamState;
                        if (state != null)
                        {
                            if (state.IsObscureStream)
                            {
                                countObscure++;
                            }
                        }
                    }
                }

                return (countObscure == 0) ? null : String.Format(CultureInfo.CurrentCulture, Strings.PlaybackView_ToolTip_Format, countObscure);
            }
        }

        public IEnumerable PlaybackableFileStreams
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackableFileStreams;
            }
            private set
            {
                if (this.playbackableFileStreams != value)
                {
                    this.playbackableFileStreams = value;
                    RaisePropertyChanged("PlaybackableFileStreams");
                }
            }
        }

        public MetadataInfo PlaybackMetadata
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackMetadata;
            }

            private set
            {
                DebugHelper.AssertUIThread();

                if (this.playbackMetadata != value)
                {
                    this.playbackMetadata = value;
                    RaisePropertyChanged("PlaybackMetadata");
                }
            }
        }

        public TimelineMarkersCollection PlaybackFileMarkers
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackFileMarkers;
            }

            private set
            {
                DebugHelper.AssertUIThread();

                if (this.playbackFileMarkers != value)
                {
                    this.playbackFileMarkers = value;
                    RaisePropertyChanged("PlaybackFileMarkers");
                }
            }
        }

        public TimelinePausePointsCollection PlaybackPausePoints
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackPausePoints;
            }

            private set
            {
                DebugHelper.AssertUIThread();

                if (this.playbackPausePoints != value)
                {
                    this.playbackPausePoints = value;
                    RaisePropertyChanged("PlaybackPausePoints");
                }
            }
        }

        public TimelineInOutPointsCollection PlaybackInOutPoints
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackInOutPoints;
            }

            private set
            {
                DebugHelper.AssertUIThread();

                if (this.playbackInOutPoints != value)
                {
                    this.playbackInOutPoints = value;
                    RaisePropertyChanged("PlaybackInOutPoints");
                }
            }
        }

        public uint PlaybackLoopCount
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackLoopCount;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.playbackLoopCount != value)
                {
                    this.playbackLoopCount = value;
                    RaisePropertyChanged("PlaybackLoopCount");

                    if ((this.playback != null) && (this.playbackState != KStudioPlaybackState.Playing))
                    {
                        this.playback.LoopCount = value;
                    }
                }
            }
        }

        public uint PlaybackLoopIteration
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackLoopIteration;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.playbackLoopIteration != value)
                {
                    this.playbackLoopIteration = value;
                    RaisePropertyChanged("PlaybackLoopIteration");

                    if ((this.playback != null) && (this.playbackState != KStudioPlaybackState.Playing))
                    {
                        this.playback.CurrentLoopIteration = value;
                    }
                }
            }
        }

        public bool AreAllStreamsVisibleForPlayback
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.areAllStreamsVisibleForPlayback;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.areAllStreamsVisibleForPlayback != value)
                {
                    this.areAllStreamsVisibleForPlayback = value;

                    this.RaisePropertyChanged("AreAllStreamsVisibleForPlayback");
                    this.RaisePropertyChanged("PlaybackableStreamsTitle");
                    this.RaisePropertyChanged("PlaybackableStreamsToolTip");
                }
            }
        }

        public string PlaybackFilePath
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.playbackFilePath;
            }

            private set
            {
                DebugHelper.AssertUIThread();

                if (this.playbackFilePath != value)
                {
                    this.playbackFilePath = value;
                    RaisePropertyChanged("PlaybackFilePath");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public void OpenTargetPlayback(string filePath, bool asReadOnly)
        {
            this.OpenPlayback(filePath, asReadOnly, false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void OpenLocalPlayback(string filePath, bool asReadOnly)
        {
            this.OpenPlayback(filePath, asReadOnly, true);
        }

        public void PlayPlayback()
        {
            DebugHelper.AssertUIThread();

            this.StartPlayback(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                DebugHelper.AssertUIThread();

                this.Shutdown();
            }
        }

        private void RaiseBusy(bool busy)
        {
            DebugHelper.AssertUIThread();

            EventHandler<BusyEventArgs> handler = this.Busy;
            if (handler != null)
            {
                handler(this, new BusyEventArgs(busy));
            }
        }

        public string MonitorViewStateTitle
        {
            get
            {
                DebugHelper.AssertUIThread();

                string value = null;

                if (this.monitor == null)
                {
                    value = Strings.ViewMonitor_None;
                }
                else
                {
                    if (this.playback != null)
                    {
                        value = Strings.ViewMonitor_File;
                    }
                    else
                    {
                        value = Strings.ViewMonitor_Live;
                    }
                }

                return value;
            }
        }

        public string ComboViewStateTitle
        {
            get
            {
                DebugHelper.AssertUIThread();

                string value = null;

                if (this.playbackFile == null)
                {
                    value = Strings.ViewPlayback_None;
                }
                else
                {
                    if ((this.playbackFileLocal) || (this.playback == null))
                    {
                        value = Strings.ViewPlayback_File;
                    }
                    else if (this.monitor != null)
                    {
                        value = Strings.ViewPlayback_Live;
                    }
                    else
                    {
                        value = Strings.ViewPlayback_None;
                    }
                }

                return value;
            }
        }

        public HashSet<KStudioEventStreamIdentifier> GetAvailableComboStreams()
        {
            DebugHelper.AssertUIThread();

            HashSet<KStudioEventStreamIdentifier> value = null;

            if (this.playbackFile != null)
            {
                value = new HashSet<KStudioEventStreamIdentifier>();

                foreach (KStudioEventStream stream in this.playbackFile.EventStreams)
                {
                    value.Add(new KStudioEventStreamIdentifier(stream.DataTypeId));
                    value.Add(new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId));
                }
            }
            else if (this.monitor != null)
            {
                value = new HashSet<KStudioEventStreamIdentifier>();

                foreach (KStudioEventStream stream in this.targetMonitorableStreams)
                {
                    EventStreamState ess = stream.UserState as EventStreamState;
                    if ((ess != null) && ess.IsSelectedForTargetMonitor)
                    {
                        value.Add(new KStudioEventStreamIdentifier(stream.DataTypeId));
                        value.Add(new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId));
                    }
                }
            }

            return value;
        }

        public HashSet<KStudioEventStreamIdentifier> GetAvailableMonitorStreams()
        {
            DebugHelper.AssertUIThread();

            HashSet<KStudioEventStreamIdentifier> value = null;

            if (this.monitor != null)
            {
                value = new HashSet<KStudioEventStreamIdentifier>();

                foreach (KStudioEventStream stream in this.targetMonitorableStreams)
                {
                    EventStreamState ess = stream.UserState as EventStreamState;
                    if ((ess != null) && ess.IsSelectedForTargetMonitor)
                    {
                        value.Add(new KStudioEventStreamIdentifier(stream.DataTypeId));
                        value.Add(new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId));
                    }
                }
            }

            return value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void OpenPlayback(string filePath, bool asReadOnly, bool localPlayback)
        {
            DebugHelper.AssertUIThread();

            if (String.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if ((this.playbackFilePath == filePath) && (this.IsPlaybackFileReadOnly == asReadOnly))
            {
                return;
            }

            this.CloseRecording(false);
            this.ClosePlayback();

            this.playbackFileLocal = localPlayback;
            RaisePropertyChanged("IsPlaybackFileOnTarget");

            if ((this.client != null) && (this.recording == null) && (this.playback == null))
            {
                this.PlaybackTime = TimeSpan.Zero;

                if (!asReadOnly)
                {
                    try
                    {
                        this.playbackFile = this.client.OpenEventFileForEdit(filePath, localPlayback ? KStudioEventFileFlags.None : KStudioEventFileFlags.OnService);
                    }
#if OPEN_FOR_EDIT
                    catch (IOException ex)
                    {
                        if (ex.Message == "access denied")
                        {
                            // read-only, so ignore
                            if (this.loggingService != null)
                            {
                                this.loggingService.LogLine(Strings.FileOpen_Error_CantOpenForEdit_Format, filePath);
                            }
                        }
                        else
                        {
                            this.HandleFileOpenError(ex, filePath);
                            return;
                        }
                    }
#endif // OPEN_FOR_EDIT
                    catch (Exception ex)
                    {
                        this.HandleFileOpenError(ex, filePath);
                        return;
                    }
                }

                try
                {
                    if (this.playbackFile == null)
                    {
                        this.playbackFile = this.client.OpenEventFile(filePath, localPlayback ? KStudioEventFileFlags.None : KStudioEventFileFlags.OnService);
                    }

                    this.PlaybackFilePath = this.playbackFile.FilePath;
                    PlaybackFileSettings playbackFileSettings = new PlaybackFileSettings(this.fileSettingsService, this.targetAlias, this.playbackFile);
                    this.playbackFile.UserState = playbackFileSettings;

                    this.PlaybackLoopCount = playbackFileSettings.LoadSetting("playback", "loopCount", 0);

                    bool actuallyReadOnly = !(this.playbackFile is KStudioWritableEventFile);

                    if (actuallyReadOnly)
                    {
                        this.PlaybackMetadata = new MetadataInfo(true, Strings.PlaybackMetadata_Title,
                            this.playbackFilePath,
                            this.playbackFile.GetMetadata(KStudioMetadataType.Public),
                            this.playbackFile.GetMetadata(KStudioMetadataType.PersonallyIdentifiableInformation));
                    }
                    else
                    {
                        this.PlaybackMetadata = new MetadataInfo(false, Strings.PlaybackMetadata_Title,
                            this.playbackFilePath,
                            new WritableMetadataProxy(this.playbackFile, this.playbackFile.GetMetadata(KStudioMetadataType.Public)),
                            new WritableMetadataProxy(this.playbackFile, this.playbackFile.GetMetadata(KStudioMetadataType.PersonallyIdentifiableInformation)));
                    }

                    this.PlaybackFileMarkers = new TimelineMarkersCollection(this.targetAlias, this.playbackFile);
                    this.PlaybackPausePoints = new TimelinePausePointsCollection(this.targetAlias, this.playbackFile, this.PlaybackFileMarkers);
                    this.PlaybackInOutPoints = new TimelineInOutPointsCollection(this.targetAlias, this.playbackFile);

                    foreach (KStudioEventStream s in this.playbackFile.EventStreams)
                    {
                        EventStreamState streamState = s.UserState as EventStreamState;
                        if (streamState != null)
                        {
                            streamState.SetDuration(this.playbackFile.Duration);

                            if (actuallyReadOnly)
                            {
                                streamState.Metadata = new MetadataInfo(false, streamState.ShortName,
                                    this.playbackFilePath + "\n" + streamState.LongName,
                                    s.GetMetadata(KStudioMetadataType.Public),
                                    s.GetMetadata(KStudioMetadataType.PersonallyIdentifiableInformation));
                            }
                            else
                            {
                                streamState.Metadata = new MetadataInfo(false, streamState.ShortName,
                                    this.playbackFilePath + "\n" + streamState.LongName,
                                    new WritableMetadataProxy(this.playbackFile, s.GetMetadata(KStudioMetadataType.Public)),
                                    new WritableMetadataProxy(this.playbackFile, s.GetMetadata(KStudioMetadataType.PersonallyIdentifiableInformation)));
                            }

                            streamState.SetupForPlayback(this.client.EventStreams);
                        }
                    }

                    if (!localPlayback)
                    {
                        IReadOnlyCollection<Tuple<KStudioEventStreamSelectorItem, bool>> lastStreamSelection = playbackFileSettings.GetLastStreamSelection();

                        if (lastStreamSelection == null)
                        {
                            // match up file streams to live streams if unambiguous

                            Dictionary<Guid, KStudioEventStream> liveEventStreams = new Dictionary<Guid, KStudioEventStream>();
                            foreach (KStudioEventStream liveEventStream in this.targetPlaybackableStreams)
                            {
                                KStudioEventStream multipleExist;
                                if (liveEventStreams.TryGetValue(liveEventStream.DataTypeId, out multipleExist))
                                {
                                    liveEventStreams[liveEventStream.DataTypeId] = null;
                                }
                                else
                                {
                                    liveEventStreams[liveEventStream.DataTypeId] = liveEventStream;
                                }
                            }

                            Dictionary<Guid, bool> fileEventStreams = new Dictionary<Guid, bool>();
                            foreach (KStudioEventStream fileEventStream in this.playbackFile.EventStreams)
                            {
                                bool multipleExist;
                                if (fileEventStreams.TryGetValue(fileEventStream.DataTypeId, out multipleExist))
                                {
                                    fileEventStreams[fileEventStream.DataTypeId] = true;
                                }
                                else
                                {
                                    fileEventStreams[fileEventStream.DataTypeId] = false;
                                }
                            }

                            foreach (KStudioEventStream fileStream in this.playbackFile.EventStreams)
                            {
                                EventStreamState fileStreamState = fileStream.UserState as EventStreamState;
                                if (fileStreamState != null)
                                {
                                    bool multipleFileExist;

                                    if (fileEventStreams.TryGetValue(fileStream.DataTypeId, out multipleFileExist))
                                    {
                                        if (!multipleFileExist)
                                        {
                                            KStudioEventStream multipleLiveExist;

                                            if (liveEventStreams.TryGetValue(fileStream.DataTypeId, out multipleLiveExist))
                                            {
                                                if (multipleLiveExist != null)
                                                {
                                                    fileStreamState.SelectedLivePlaybackStream = multipleLiveExist;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            foreach (KStudioEventStream liveStream in this.targetPlaybackableStreams)
                            {
                                KStudioEventStreamIdentifier identifier = new KStudioEventStreamIdentifier(liveStream.DataTypeId, liveStream.SemanticId);
                                if (this.defaultPlaybackStreams.Contains(identifier))
                                {
                                    EventStreamState eventStreamState = liveStream.UserState as EventStreamState;
                                    if (eventStreamState != null)
                                    {
                                        if (eventStreamState.SelectedFilePlaybackStream != null)
                                        {
                                            eventStreamState.IsSelectedForTargetPlayback = this.DoTargetPlaybackDance(liveStream, true);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Dictionary<KStudioEventStream, bool?> selectedStreams = new Dictionary<KStudioEventStream, bool?>();

                            foreach (var item in lastStreamSelection)
                            {
                                KStudioEventStream fileStream = this.playbackFile.EventStreams.FirstOrDefault(s => (s.DataTypeId == item.Item1.DataTypeId) && (s.SemanticId == item.Item1.SourceSemanticId));

                                if (fileStream != null)
                                {
                                    KStudioEventStream liveStream = null;
                                    foreach (KStudioEventStream s in this.targetPlaybackableStreams)
                                    {
                                        if ((s.DataTypeId == item.Item1.DataTypeId) && (s.SemanticId == item.Item1.DestinationSemanticId))
                                        {
                                            liveStream = s;
                                            break;
                                        }
                                    }

                                    if (liveStream != null)
                                    {
                                        EventStreamState ess = fileStream.UserState as EventStreamState;
                                        if (ess != null)
                                        {
                                            ess.SelectedLivePlaybackStream = liveStream;

                                            if (item.Item2)
                                            {
                                                EventStreamState ess2 = liveStream.UserState as EventStreamState;
                                                if (ess2 != null)
                                                {
                                                    // do NoCheck here so that it doesn't auto turn on the tag-alongs
                                                    ess2.IsSelectedForTargetPlaybackNoCheck = true;

                                                    selectedStreams.Add(liveStream, true);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // now update the CanPlayback

                            if (selectedStreams.Count > 0)
                            {
                                this.selectedTargetStreamsPlaybackable = true;

                                foreach (KStudioEventStream liveStream in selectedStreams.Keys)
                                {
                                    if (!liveStream.HasSelectionRequirements(KStudioEventStreamSelectorRequirementFlags.ProcessPlayback, selectedStreams))
                                    {
                                        this.selectedTargetStreamsPlaybackable = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    this.PlaybackableFileStreams = this.playbackFile.EventStreams;

                    HashSet<KStudioEventStreamIdentifier> lastHiddenPlaybackable = this.lastHiddenPlaybackableStreams.HashSet;
                    if (lastHiddenPlaybackable.Count == 0)
                    {
                        lastHiddenPlaybackable = this.defaultHiddenPlaybackableStreams;
                    }

                    foreach (KStudioEventStream stream in this.playbackFile.EventStreams)
                    {
                        EventStreamState streamState = stream.UserState as EventStreamState;
                        if (streamState != null)
                        {
                            KStudioEventStreamIdentifier identifier = new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId);

                            streamState.IsVisibleForPlayback = !lastHiddenPlaybackable.Contains(identifier);
                        }
                    }

                    foreach (KStudioEventStream liveStream in this.targetPlaybackableStreams)
                    {
                        KStudioEventStreamIdentifier identifier = new KStudioEventStreamIdentifier(liveStream.DataTypeId, liveStream.SemanticId);
                        if (this.defaultPlaybackStreams.Contains(identifier))
                        {
                            EventStreamState eventStreamState = liveStream.UserState as EventStreamState;
                            if (eventStreamState != null)
                            {
                                if (eventStreamState.SelectedFilePlaybackStream != null)
                                {
                                    eventStreamState.IsSelectedForTargetPlayback = this.DoTargetPlaybackDance(liveStream, true);
                                }

                                eventStreamState.IsVisibleForPlayback = !lastHiddenPlaybackable.Contains(identifier);
                            }
                        }
                    }

                    RaisePropertyChanged("HasPlaybackFile");
                    RaisePropertyChanged("IsPlaybackFileReadOnly");
                    RaisePropertyChanged("CanPlayback");
                    RaisePropertyChanged("ComboViewStateTitle");

                    if (this.mruService != null)
                    {
                        this.mruService.AddMostRecentlyUsedLocalFile(filePath);
                    }

                    this.SetDefaultMetadataViews(this.playbackMetadata);

                    EventHandler handler = PlaybackOpened;
                    if (handler != null)
                    {
                        handler(this, null);
                    }

                    TimeSpan firstTime = this.playbackFile.EventStreams.OfType<KStudioSeekableEventStream>().Min(s => s.StartRelativeTime);
                    this.SeekPlayback(firstTime);
                }
                catch (Exception ex)
                {
                    this.ClosePlayback();

                    this.HandleFileOpenError(ex, filePath);
                }
            }
        }

        private void SetDefaultMetadataViews(MetadataInfo metadata)
        {
            DebugHelper.AssertUIThread();

            if ((metadata != null) && (this.metadataViewService != null))
            {
                foreach (Window window in App.Current.Windows)
                {
                    if (window is ToolsUIWindow)
                    {
                        IEnumerable<MetadataView> metadataViews = metadataViewService.GetMetadataViews(window);
                        if (metadataViews.Count() > 0)
                        {
                            bool setIt = true;

                            foreach (MetadataView metadataView in metadataViews)
                            {
                                if (metadataView == null)
                                {
                                    setIt = true;
                                }
                                else if (setIt)
                                {
                                    metadataView.SetMetadata(metadata);
                                    setIt = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "address")]
        private void HandleConnectError(Exception ex, IPAddress address)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(address != null);

            string error = Strings.Connect_Error_CantConnect_SingleBox;

            if (this.notificationService != null)
            {
                this.notificationService.ShowMessageBox(error, MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
            }

            if (this.loggingService != null)
            {
                this.loggingService.LogLine(error);

                if (ex != null)
                {
                    this.loggingService.LogLine(ex.Message);
                }
            }
        }

        private void HandleMonitorError(Exception ex)
        {
            DebugHelper.AssertUIThread();

            if (this.notificationService != null)
            {
                this.notificationService.ShowMessageBox(Strings.Monitor_Error_CantMonitor, MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
            }

            if (this.loggingService != null)
            {
                this.loggingService.LogLine(Strings.Monitor_Error_CantMonitor);

                if (ex != null)
                {
                    this.loggingService.LogLine(ex.Message);
                }
            }
        }

        private void HandleFileOpenError(Exception ex, string filePath)
        {
            DebugHelper.AssertUIThread();

            string error = String.Format(CultureInfo.CurrentCulture, Strings.FileOpen_Error_CantOpen_Format, filePath);

            if (this.notificationService != null)
            {
                this.notificationService.ShowMessageBox(error, MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
            }

            if (this.loggingService != null)
            {
                this.loggingService.LogLine(error);

                if (ex != null)
                {
                    this.loggingService.LogLine(ex.Message);
                }
            }
        }

        private void HandleFileCreateError(Exception ex, string filePath)
        {
            DebugHelper.AssertUIThread();

            string error;
            if (filePath == null)
            {
                error = Strings.FileOpen_Error_CantCreate;
            }
            else
            {
                error = String.Format(CultureInfo.CurrentCulture, Strings.FileOpen_Error_CantCreate_Format, filePath);
            }

            if (this.notificationService != null)
            {
                this.notificationService.ShowMessageBox(error, MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
            }

            if (this.loggingService != null)
            {
                this.loggingService.LogLine(error);

                if (ex != null)
                {
                    this.loggingService.LogLine(ex.Message);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void StartPlayback(bool startPaused)
        {
            DebugHelper.AssertUIThread();

            if ((this.playback == null) && (this.playbackFile != null))
            {
                if (!this.IsPlaybackFileOnTarget)
                {
                    this.StopMonitor();
                }

                KStudioEventStreamSelectorCollection streamSelectorCollection = null;
                KStudioPlaybackFlags flags = KStudioPlaybackFlags.IgnoreOptionalStreams;

                this.playbackStep = null;

                if (this.playbackFileLocal)
                {
                    flags |= KStudioPlaybackFlags.ToClient;

                    streamSelectorCollection = new KStudioEventStreamSelectorCollection();

                    foreach (KStudioEventStream stream in this.playbackFile.EventStreams)
                    {
                        if ((this.pluginService == null) || this.pluginService.IsInterestedInEventsFrom(EventType.Inspection, stream.DataTypeId, stream.SemanticId))
                        {
                            streamSelectorCollection.Add(stream.DataTypeId, stream.SemanticId);

                            // last one always wins
                            if ((stream.DataTypeId == KStudioEventStreamDataTypeIds.LongExposureIr) ||
                                (stream.DataTypeId == KStudioEventStreamDataTypeIds.Ir) ||
                                (stream.DataTypeId == KStudioEventStreamDataTypeIds.Depth) ||
                                (stream.DataTypeId == KStudioEventStreamDataTypeIds.RawIr))
                            {
                                this.playbackStep = new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId);
                            }
                        }
                    }
                }
                else
                {
                    streamSelectorCollection = new KStudioEventStreamSelectorCollection();

                    foreach (KStudioEventStream stream in this.targetPlaybackableStreams.OfType<KStudioEventStream>())
                    {
                        EventStreamState ess = stream.UserState as EventStreamState;
                        if ((ess != null) && ess.IsSelectedForTargetPlayback && (ess.SelectedFilePlaybackStream != null))
                        {
                            streamSelectorCollection.Add(stream.DataTypeId, ess.SelectedFilePlaybackStream.SemanticId, stream.SemanticId);

                            if (stream.SemanticId == KStudioEventStreamSemanticIds.KinectDefaultSensorConsumer)
                            {
                                if ((stream.DataTypeId == KStudioEventStreamDataTypeIds.LongExposureIr))
                                {
                                    // LEIR always wins because of the way mapping order works
                                    this.playbackStep = new KStudioEventStreamIdentifier(stream.DataTypeId, ess.SelectedFilePlaybackStream.SemanticId);
                                }
                                else if ((stream.DataTypeId == KStudioEventStreamDataTypeIds.Ir) ||
                                         (stream.DataTypeId == KStudioEventStreamDataTypeIds.RawIr))
                                {
                                    if (this.playbackStep == null)
                                    {
                                        this.playbackStep = new KStudioEventStreamIdentifier(stream.DataTypeId, ess.SelectedFilePlaybackStream.SemanticId);
                                    }
                                }
                            }
                        }
                    }
                }

                this.playback = this.client.CreatePlayback(playbackFile, streamSelectorCollection, flags);

                this.playback.PropertyChanged += this.Playback_PropertyChanged;

                this.PlaybackState = this.playback.State;

                PlaybackFileSettings playbackFileSettings = this.playbackFile.UserState as PlaybackFileSettings;
                if (playbackFileSettings != null)
                {
                    playbackFileSettings.SaveStreamSelection();
                }

                if (this.playbackPausePoints != null)
                {
                    this.playbackPausePoints.Playback = this.playback;
                }

                if (this.playbackInOutPoints != null)
                {
                    this.playbackInOutPoints.Playback = this.playback;
                }

                this.playback.LoopCount = this.playbackLoopCount;
                this.playback.CurrentLoopIteration = this.playbackLoopIteration;

                KStudioPlayback temp = this.playback;

                if (this.playbackStartTime != TimeSpan.Zero)
                {
                    temp.SeekByRelativeTime(this.playbackStartTime);

                    this.playbackStartTime = TimeSpan.Zero;
                }

                if (this.IsPlaybackFileOnTarget && (this.pluginService != null))
                {
                    this.pluginService.ClearEvents(EventType.Inspection);
                }

                this.RaisePropertyChanged("MonitorViewStateTitle");
                this.RaisePropertyChanged("ComboViewStateTitle");

                if (startPaused)
                {
                    this.RunAsync(() => temp.StartPaused());
                }
                else
                {
                    this.RunAsync(() => temp.Start());
                }

                if (this.IsPlaybackFileOnTarget && (this.settings != null) && this.settings.AutoMonitorOnTargetPlayback)
                {
                    this.StartMonitor();
                }
            }
            else if (this.playback != null)
            {
                KStudioPlayback temp = this.playback;

                if (this.playback.State == KStudioPlaybackState.Paused)
                {
                    this.RunAsync(() => temp.Resume());
                }
                else
                {
                    this.RunAsync(() => temp.Start());
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void PausePlayback()
        {
            DebugHelper.AssertUIThread();

            KStudioPlayback temp = this.playback;

            if (temp != null)
            {
                this.RunAsync(() =>
                {
                    temp.Pause();
                });
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void SeekPlayback(TimeSpan time)
        {
            DebugHelper.AssertUIThread();

            KStudioPlayback temp = this.playback;

            if (temp == null)
            {
                this.playbackStartTime = time;

                this.PlaybackTime = time;

                this.UpdatePlaybackStreamsTimeline(time, true, true);
            }
            else if (this.playbackState != KStudioPlaybackState.Playing)
            {
                this.RunAsync(() => temp.SeekByRelativeTime(time));

                this.PlaybackTime = time;

                this.UpdatePlaybackStreamsTimeline(time, true, true);
            }
        }

        public void StopPlayback()
        {
            DebugHelper.AssertUIThread();

            if (this.playbackPausePoints != null)
            {
                this.playbackPausePoints.Playback = null;
            }

            if (this.playbackInOutPoints != null)
            {
                this.playbackInOutPoints.Playback = null;
            }

            if (this.playback != null)
            {
                this.playback.PropertyChanged -= this.Playback_PropertyChanged;

                KStudioPlayback temp = this.playback;
                this.RunAsync(() => temp.Stop());

                this.playback.Dispose();

                this.playback = null;
                this.PlaybackState = KStudioPlaybackState.Error;
                this.playbackStep = null;

                if (this.pluginService != null)
                {
                    this.pluginService.ClearEvents(EventType.Inspection);

                    if (!this.playbackFileLocal)
                    {
                        this.pluginService.ClearEvents(EventType.Monitor);
                    }

                    TimeSpan firstTime = this.playbackFile.EventStreams.OfType<KStudioSeekableEventStream>().Min(s => s.StartRelativeTime);
                    this.SeekPlayback(firstTime);
                }

                this.RaisePropertyChanged("MonitorViewStateTitle");
                this.RaisePropertyChanged("ComboViewStateTitle");
            }
        }

        public void StepPlayback()
        {
            DebugHelper.AssertUIThread();

            if (this.playbackFile != null)
            {
                if (this.playback == null)
                {
                    this.StartPlayback(true);
                }

                if (this.playback != null)
                {
                    if (this.playback.State == KStudioPlaybackState.Paused)
                    {
                        KStudioPlayback temp = this.playback;
                        if (this.playbackStep.HasValue)
                        {
                            Guid dataTypeId = this.playbackStep.Value.DataTypeId;
                            Guid semanticId = this.playbackStep.Value.SemanticId;

                            this.RunAsync(() => temp.StepOnce(dataTypeId, semanticId));
                        }
                        else
                        {
                            this.RunAsync(() => temp.StepOnce());
                        }
                    }
                }
            }
        }

        public void ClosePlayback()
        {
            DebugHelper.AssertUIThread();

            this.StopPlayback();

            if (this.playbackFile != null)
            {
                PlaybackFileSettings playbackFileSettings = this.playbackFile.UserState as PlaybackFileSettings;

                if (playbackFileSettings != null)
                {
                    playbackFileSettings.SaveSetting("playback", "loopCount", this.playbackLoopCount);

                    playbackFileSettings.Dispose();
                    this.playbackFile.UserState = null;
                }

                HashSet<MetadataInfo> metadataViewsToClose = new HashSet<MetadataInfo>();
                if (this.playbackMetadata != null)
                {
                    metadataViewsToClose.Add(this.playbackMetadata);
                }

                foreach (KStudioEventStream s in this.playbackFile.EventStreams)
                {
                    EventStreamState streamState = s.UserState as EventStreamState;
                    if ((streamState != null) & (streamState.Metadata != null))
                    {
                        metadataViewsToClose.Add(streamState.Metadata);
                        streamState.Metadata = null;
                        streamState.SelectedLivePlaybackStream = null;
                    }
                }

                if (this.playbackFileMarkers != null)
                {
                    foreach (TimelineMarker marker in this.playbackFileMarkers)
                    {
                        if (marker.HasMetadata)
                        {
                            metadataViewsToClose.Add(marker.Metadata);
                        }
                    }
                }

                this.CloseMetadataViews(metadataViewsToClose);

                if (this.pluginService != null)
                {
                    this.pluginService.ClearEvents(EventType.Inspection);
                }

                if (this.playbackFileMarkers != null)
                {
                    this.playbackFileMarkers.Dispose();
                    this.PlaybackFileMarkers = null;
                }

                this.PlaybackLoopCount = 0;
                this.PlaybackLoopIteration = 0;
                this.PlaybackPausePoints = null;
                this.PlaybackInOutPoints = null;
                this.PlaybackMetadata = null;
                this.PlaybackFilePath = null;
                this.PlaybackTime = TimeSpan.Zero;
                this.PlaybackableFileStreams = null;
                this.playbackFile.Dispose();
                this.playbackFile = null;
                this.playbackStartTime = TimeSpan.Zero;
                this.selectedTargetStreamsPlaybackable = false;

                RaisePropertyChanged("HasPlaybackFile");
                RaisePropertyChanged("CanPlayback");
                RaisePropertyChanged("HasPlayback");
                RaisePropertyChanged("PlaybackState");
                RaisePropertyChanged("ComboViewStateTitle");
            }
        }

        public string TargetMonitorableStreamsTitle
        {
            get
            {
                DebugHelper.AssertUIThread();

                int countVisible = 0;
                int countHidden = 0;

                bool allVisible = this.areAllStreamsVisibleForMonitor;
                bool advanced = this.settings.AdvancedModeObscureStreams;

                foreach (KStudioEventStream stream in this.targetMonitorableStreams)
                {
                    EventStreamState state = stream.UserState as EventStreamState;
                    if (state != null)
                    {
                        if (!state.IsObscureStream || advanced)
                        {
                            if (allVisible || state.IsVisibleForTargetMonitor)
                            {
                                countVisible++;
                            }
                            else
                            {
                                countHidden++;
                            }
                        }
                    }
                }

                return String.Format(CultureInfo.CurrentCulture, Strings.MonitorView_Header_Format, (countVisible + countHidden), countVisible, countHidden);
            }
        }

        public string TargetMonitorableStreamsToolTip
        {
            get
            {
                DebugHelper.AssertUIThread();

                int countObscure = 0;

                if (this.settings.AdvancedModeObscureStreams)
                {
                    foreach (KStudioEventStream stream in this.targetMonitorableStreams)
                    {
                        EventStreamState state = stream.UserState as EventStreamState;
                        if (state != null)
                        {
                            if (state.IsObscureStream)
                            {
                                countObscure++;
                            }
                        }
                    }
                }

                return (countObscure == 0) ? null : String.Format(CultureInfo.CurrentCulture, Strings.MonitorView_ToolTip_Format, countObscure);
            }
        }

        public ICollectionView TargetMonitorableStreams
        {
            get
            {
                return this.targetMonitorableStreams;
            }
        }

        public bool DoTargetRecordingDance(KStudioEventStream stream, bool newValue)
        {
            DebugHelper.AssertUIThread();

            bool okay = false;

            if (stream == null)
            {
                Debug.Assert(newValue == false);

                this.selectedTargetStreamsRecordable = false;
                RaisePropertyChanged("CanRecord");
            }
            else
            {
                okay = true;

                // if unselecting the stream, first unselect all hidden streams that require the changing stream 

                if (!newValue)
                {
                    Dictionary<KStudioEventStream, bool?> recordableStreams = new Dictionary<KStudioEventStream, bool?>();
                    foreach (KStudioEventStream recordableStream in this.targetRecordableStreams)
                    {
                        EventStreamState state = (EventStreamState)recordableStream.UserState;
                        recordableStreams.Add(recordableStream, recordableStream == stream ? false : state.IsSelectedForTargetRecording);
                    }

                    foreach (KStudioEventStream recordableStream in this.targetRecordableStreams)
                    {
                        if (recordableStream != stream)
                        {
                            EventStreamState state = (EventStreamState)(recordableStream.UserState);
                            if (state.IsSelectedForTargetRecording && !state.IsVisibleForTargetRecording)
                            {
                                if (!recordableStream.HasSelectionRequirements(KStudioEventStreamSelectorRequirementFlags.ProcessRecord, recordableStreams))
                                {
                                    state.IsSelectedForTargetRecordingNoCheck = false;
                                }
                            }
                        }
                    }
                }

                {
                    Dictionary<KStudioEventStream, bool?> recordableStreams = new Dictionary<KStudioEventStream, bool?>();
                    foreach (KStudioEventStream recordableStream in this.targetRecordableStreams)
                    {
                        EventStreamState state = (EventStreamState)(recordableStream.UserState);
                        recordableStreams.Add(recordableStream, state.IsSelectedForTargetRecording);
                    }

                    bool canRecord = false;
                    okay = stream.DetermineSelectionRequirements(KStudioEventStreamSelectorRequirementFlags.ProcessRecord, recordableStreams, newValue, out canRecord);

                    if (okay)
                    {
                        foreach (KeyValuePair<KStudioEventStream, bool?> kv in recordableStreams)
                        {
                            if (kv.Key != stream)
                            {
                                if (kv.Value.HasValue)
                                {
                                    ((EventStreamState)kv.Key.UserState).IsEnabledForTargetRecording = true;
                                    ((EventStreamState)kv.Key.UserState).IsSelectedForTargetRecordingNoCheck = kv.Value.Value;
                                }
                                else
                                {
                                    ((EventStreamState)kv.Key.UserState).IsEnabledForTargetRecording = false;
                                }
                            }
                        }
                    }

                    this.selectedTargetStreamsRecordable = canRecord;
                    RaisePropertyChanged("CanRecord");
                }
            }

            return okay;
        }

        public void UpdateTargetRecordingVisibility(KStudioEventStream stream)
        {
            DebugHelper.AssertUIThread();

            if (stream != null)
            {
                EventStreamState streamState = stream.UserState as EventStreamState;
                if (streamState != null)
                {
                    if (streamState.IsVisibleForTargetRecording)
                    {
                        this.lastHiddenRecordableStreams.HashSet.Remove(new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId));
                    }
                    else
                    {
                        this.lastHiddenRecordableStreams.HashSet.Add(new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId));
                    }
                }
            }

            this.RaisePropertyChanged("TargetRecordableStreamsTitle");
            this.RaisePropertyChanged("TargetRecordableStreamsToolTip");
        }

        public bool DoTargetPlaybackDance(KStudioEventStream stream, bool newValue)
        {
            DebugHelper.AssertUIThread();

            bool okay = false;

            if (stream == null)
            {
                Debug.Assert(newValue == false);

                this.selectedTargetStreamsPlaybackable = false;
                RaisePropertyChanged("CanPlayback");
            }
            else
            {
                okay = true;

                // if unselecting the stream, first unselect all hidden streams that require the changing stream 

                if (!newValue)
                {
                    Dictionary<KStudioEventStream, bool?> playbackableStreams = new Dictionary<KStudioEventStream, bool?>();
                    foreach (KStudioEventStream playbackableStream in this.targetPlaybackableStreams)
                    {
                        EventStreamState state = (EventStreamState)(playbackableStream.UserState);
                        if (state.SelectedFilePlaybackStream != null)
                        {
                            playbackableStreams.Add(playbackableStream, playbackableStream == stream ? false : state.IsSelectedForTargetPlayback);
                        }
                    }

                    foreach (KStudioEventStream playbackableStream in this.targetPlaybackableStreams)
                    {
                        if (playbackableStream != stream)
                        {
                            EventStreamState state = (EventStreamState)(playbackableStream.UserState);
                            if (state.IsSelectedForTargetPlayback && (state.SelectedFilePlaybackStream != null))
                            {
                                EventStreamState stateFile = (EventStreamState)(state.SelectedFilePlaybackStream.UserState);
                                if (!stateFile.IsVisibleForPlayback)
                                {
                                    if (!playbackableStream.HasSelectionRequirements(KStudioEventStreamSelectorRequirementFlags.ProcessPlayback, playbackableStreams))
                                    {
                                        state.IsSelectedForTargetPlaybackNoCheck = false;
                                    }
                                }
                            }
                        }
                    }
                }

                {
                    Dictionary<KStudioEventStream, bool?> playbackableStreams = new Dictionary<KStudioEventStream, bool?>();
                    foreach (KStudioEventStream playbackableStream in this.targetPlaybackableStreams)
                    {
                        EventStreamState state = (EventStreamState)(playbackableStream.UserState);
                        if (state.SelectedFilePlaybackStream != null)
                        {
                            playbackableStreams.Add(playbackableStream, state.IsSelectedForTargetPlayback);
                        }
                    }

                    bool canPlayback = false;
                    okay = stream.DetermineSelectionRequirements(KStudioEventStreamSelectorRequirementFlags.ProcessPlayback, playbackableStreams, newValue, out canPlayback);

                    if (okay)
                    {
                        foreach (KeyValuePair<KStudioEventStream, bool?> kv in playbackableStreams)
                        {
                            if (kv.Key != stream)
                            {
                                if (kv.Value.HasValue)
                                {
                                    ((EventStreamState)kv.Key.UserState).IsEnabledForTargetPlayback = true;
                                    ((EventStreamState)kv.Key.UserState).IsSelectedForTargetPlaybackNoCheck = kv.Value.Value;
                                }
                                else
                                {
                                    ((EventStreamState)kv.Key.UserState).IsEnabledForTargetPlayback = false;
                                }
                            }
                        }
                    }

                    this.selectedTargetStreamsPlaybackable = canPlayback;
                    RaisePropertyChanged("CanPlayback");
                }
            }

            return okay;
        }

        public void UpdatePlaybackVisibility(KStudioEventStream stream)
        {
            DebugHelper.AssertUIThread();
            if (stream != null)
            {
                EventStreamState streamState = stream.UserState as EventStreamState;
                if (streamState != null)
                {
                    if (streamState.IsVisibleForPlayback)
                    {
                        this.lastHiddenPlaybackableStreams.HashSet.Remove(new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId));
                    }
                    else
                    {
                        this.lastHiddenPlaybackableStreams.HashSet.Add(new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId));
                    }
                }
            }

            this.RaisePropertyChanged("PlaybackableStreamsTitle");
            this.RaisePropertyChanged("PlaybackableStreamsToolTip");
        }

        public bool CanMonitor
        {
            get
            {
                DebugHelper.AssertUIThread();
                return this.connectedState && (this.monitor == null) && this.selectedTargetStreamsMonitorable;
            }
        }

        public bool HasMonitor
        {
            get
            {
                DebugHelper.AssertUIThread();
                return this.monitor != null;
            }
        }

        public KStudioMonitorState MonitorState
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.monitorState;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.monitorState != value)
                {
                    this.monitorState = value;
                    RaisePropertyChanged("CanMonitor");
                    RaisePropertyChanged("HasMonitor");
                    RaisePropertyChanged("MonitorState");
                }
            }
        }

        public TimeSpan MonitorTime
        {
            get
            {
                DebugHelper.AssertUIThread();

                return KStudio.GetCurrentRelativeTime();
            }
            private set
            {
                if (this.monitorTime != value)
                {
                    this.monitorTime = value;
                    RaisePropertyChanged("MonitorTime");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void StartMonitor()
        {
            DebugHelper.AssertUIThread();

            if ((this.client != null) && (this.monitor == null))
            {
                KStudioEventStreamSelectorCollection streamSelectorCollection = new KStudioEventStreamSelectorCollection();
                foreach (KStudioEventStream s in this.targetMonitorableStreams.OfType<KStudioEventStream>())
                {
                    EventStreamState streamState = s.UserState as EventStreamState;
                    if (streamState != null)
                    {
                        if (streamState.IsSelectedForTargetMonitor)
                        {
                            streamSelectorCollection.Add(s.DataTypeId, s.SemanticId);

                            this.lastMonitoredStreams.HashSet.Add(new KStudioEventStreamIdentifier(s.DataTypeId, s.SemanticId));
                        }
                        else
                        {
                            this.lastMonitoredStreams.HashSet.Remove(new KStudioEventStreamIdentifier(s.DataTypeId, s.SemanticId));
                        }
                    }
                }

                if (streamSelectorCollection.Count > 0)
                {
                    try
                    {
                        this.MonitorTime = TimeSpan.Zero;

                        this.monitor = this.client.CreateMonitor(streamSelectorCollection, KStudioMonitorFlags.IgnoreOptionalStreams);

                        this.monitor.PropertyChanged += this.Monitor_PropertyChanged;

                        this.MonitorState = this.monitor.State;

                        KStudioMonitor temp = this.monitor;
                        this.RunAsync(() => temp.Start());

                        if (this.pluginService != null)
                        {
                            this.pluginService.ClearEvents(EventType.Monitor);
                        }

                        this.RaisePropertyChanged("MonitorViewStateTitle");
                        this.RaisePropertyChanged("ComboViewStateTitle");

                        this.monitorTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        this.StopMonitor();

                        this.HandleMonitorError(ex);
                        return;
                    }
                }
            }
        }

        public void StopMonitor()
        {
            DebugHelper.AssertUIThread();

            if (this.monitor != null)
            {
                this.monitorTimer.Stop();

                KStudioMonitor temp = this.monitor;
                this.RunAsync(() => temp.Stop());

                this.monitor.PropertyChanged -= this.Monitor_PropertyChanged;
                this.monitor.Dispose();

                this.monitor = null;
                this.MonitorTime = TimeSpan.Zero;
                this.MonitorState = KStudioMonitorState.Error;

                this.RaisePropertyChanged("MonitorTime");
                this.RaisePropertyChanged("MonitorViewStateTitle");
                this.RaisePropertyChanged("ComboViewStateTitle");

                if (this.pluginService != null)
                {
                    this.pluginService.ClearEvents(EventType.Monitor);
                }
            }
        }

        public bool DoTargetMonitorDance(KStudioEventStream stream, bool newValue)
        {
            DebugHelper.AssertUIThread();

            bool okay = false;

            if (stream == null)
            {
                Debug.Assert(newValue == false);

                this.selectedTargetStreamsMonitorable = false;
                RaisePropertyChanged("CanMonitor");
            }
            else
            {
                okay = true;

                // if unselecting the stream, first unselect all hidden streams that require the changing stream 

                if (!newValue)
                {
                    Dictionary<KStudioEventStream, bool?> monitorableStreams = new Dictionary<KStudioEventStream, bool?>();
                    foreach (KStudioEventStream monitorableStream in this.targetMonitorableStreams)
                    {
                        EventStreamState state = (EventStreamState)monitorableStream.UserState;
                        monitorableStreams.Add(monitorableStream, monitorableStream == stream ? false : state.IsSelectedForTargetMonitor);
                    }

                    foreach (KStudioEventStream monitorableStream in this.targetMonitorableStreams)
                    {
                        if (monitorableStream != stream)
                        {
                            EventStreamState state = (EventStreamState)(monitorableStream.UserState);
                            if (state.IsSelectedForTargetMonitor && !state.IsVisibleForTargetMonitor)
                            {
                                if (!monitorableStream.HasSelectionRequirements(KStudioEventStreamSelectorRequirementFlags.ProcessMonitor, monitorableStreams))
                                {
                                    state.IsSelectedForTargetMonitorNoCheck = false;
                                }
                            }
                        }
                    }
                }

                bool canMonitor = false;
                {
                    Dictionary<KStudioEventStream, bool?> monitorableStreams = new Dictionary<KStudioEventStream, bool?>();
                    foreach (KStudioEventStream monitorableStream in this.targetMonitorableStreams)
                    {
                        EventStreamState state = (EventStreamState)(monitorableStream.UserState);
                        monitorableStreams.Add(monitorableStream, state.IsSelectedForTargetMonitor);
                    }

                    okay = stream.DetermineSelectionRequirements(KStudioEventStreamSelectorRequirementFlags.ProcessMonitor, monitorableStreams, newValue, out canMonitor);

                    if (okay)
                    {
                        foreach (KeyValuePair<KStudioEventStream, bool?> kv in monitorableStreams)
                        {
                            if (kv.Key != stream)
                            {
                                if (kv.Value.HasValue)
                                {
                                    ((EventStreamState)kv.Key.UserState).IsEnabledForTargetMonitor = true;
                                    ((EventStreamState)kv.Key.UserState).IsSelectedForTargetMonitorNoCheck = kv.Value.Value;
                                }
                                else
                                {
                                    ((EventStreamState)kv.Key.UserState).IsEnabledForTargetMonitor = false;
                                }
                            }
                        }
                    }
                }

                this.selectedTargetStreamsMonitorable = canMonitor;
                RaisePropertyChanged("CanMonitor");
            }

            return okay;
        }

        public void UpdateTargetMonitorVisibility(KStudioEventStream stream)
        {
            DebugHelper.AssertUIThread();
            if (stream != null)
            {
                EventStreamState streamState = stream.UserState as EventStreamState;
                if (streamState != null)
                {
                    if (streamState.IsVisibleForTargetMonitor)
                    {
                        this.lastHiddenMonitorableStreams.HashSet.Remove(new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId));
                    }
                    else
                    {
                        this.lastHiddenMonitorableStreams.HashSet.Add(new KStudioEventStreamIdentifier(stream.DataTypeId, stream.SemanticId));
                    }
                }
            }

            this.RaisePropertyChanged("TargetMonitorableStreamsTitle");
            this.RaisePropertyChanged("TargetMonitorableStreamsToolTip");
        }

        public bool AreAllStreamsVisibleForMonitor
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.areAllStreamsVisibleForMonitor;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.areAllStreamsVisibleForMonitor != value)
                {
                    this.areAllStreamsVisibleForMonitor = value;

                    this.RaisePropertyChanged("AreAllStreamsVisibleForMonitor");
                    this.RaisePropertyChanged("TargetMonitorableStreamsTitle");
                    this.RaisePropertyChanged("TargetMonitorableStreamsToolTip");
                }
            }
        }

        private void ObjectInitializer(KStudioObject obj)
        {
            KStudioEventStream eventStream = obj as KStudioEventStream;
            if (eventStream != null)
            {
                KStudioEventStreamIdentifier identifier = new KStudioEventStreamIdentifier(eventStream.DataTypeId, eventStream.SemanticId);

                bool obscureStream =
                    this.obscureStreams.Contains(identifier);

                obj.UserState = new EventStreamState(eventStream, this, obscureStream);
            }
        }

        private bool selectedTargetStreamsRecordable = false;
        private ICollectionView targetRecordableStreams = null;

        private bool selectedTargetStreamsPlaybackable = false;
        private ICollectionView targetPlaybackableStreams = null;

        private bool selectedTargetStreamsMonitorable = false;
        private ICollectionView targetMonitorableStreams = null;

        void MonitorTimer_Tick(object sender, EventArgs e)
        {
            DebugHelper.AssertUIThread();

            RaisePropertyChanged("MonitorTime");
        }

        private void Client_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.client != null))
            {
                switch (e.PropertyName)
                {
                    case "IsServiceConnected":
                        this.IsTargetConnected = this.client.IsServiceConnected;

                        if (this.pluginService != null)
                        {
                            this.pluginService.ClearEvents(EventType.Monitor);
                        }
                        break;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void Recording_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e != null) && (this.recording != null))
            {
                try
                {
                    switch (e.PropertyName)
                    {
                        case "State":
                            this.RecordingState = this.recording.State;
                            break;

                        case "Duration":
                            this.RecordingTime = this.recording.Duration;
                            break;

                        case "FileSizeBytes":
                            this.RecordingFileSizeBytes = this.recording.FileSizeBytes;
                            break;

                        case "BufferSizeMegabytes":
                            this.RecordingBufferSizeMegabytes = this.recording.BufferSizeMegabytes;
                            break;

                        case "BufferInUseSizeMegabytes":
                            this.RecordingBufferInUseMegabytes = this.recording.BufferInUseSizeMegabytes;
                            break;
                    }
                }
                catch (Exception)
                {
                    this.CloseRecording(false);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void Playback_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e != null) && (this.playback != null))
            {
                try
                {
                    switch (e.PropertyName)
                    {
                        case "State":
                            this.PlaybackState = this.playback.State;
                            break;

                        case "CurrentRelativeTime":
                            this.PlaybackTime = this.playback.CurrentRelativeTime;

                            this.UpdatePlaybackStreamsTimeline(this.playbackTime, false, false);
                            break;

                        case "CurrentLoopIteration":
                            this.PlaybackLoopIteration = this.playback.CurrentLoopIteration;
                            break;
                    }
                }
                catch (Exception)
                {
                    this.ClosePlayback();
                }
            }
        }

        private void UpdatePlaybackStreamsTimeline(TimeSpan time, bool clearEvents, bool forceHandleEvents)
        {
            if (this.playbackFile != null)
            {
                if (clearEvents && (this.pluginService != null))
                {
                    this.pluginService.ClearEvents(EventType.Inspection);
                }

                if (this.playbackFileLocal)
                {
                    foreach (KStudioEventStream stream in this.playbackFile.EventStreams)
                    {
                        EventStreamState ess = stream.UserState as EventStreamState;
                        uint eventIndex;
                        if (ess.UpdateTime(time, out eventIndex))
                        {
                            if ((forceHandleEvents || (this.playbackState != KStudioPlaybackState.Playing)) && (this.pluginService != null))
                            {
                                KStudioSeekableEventStream seekableStream = stream as KStudioSeekableEventStream;
                                if (seekableStream != null)
                                {
                                    KStudioEvent eventObj = seekableStream.ReadEvent(eventIndex);
                                    if (eventObj != null)
                                    {
                                        this.pluginService.HandleEvent(EventType.Inspection, eventObj);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (KStudioEventStream stream in this.playbackFile.EventStreams)
                    {
                        EventStreamState ess = stream.UserState as EventStreamState;
                        if (ess != null)
                        {
                            bool handled = false;

                            if (ess.SelectedLivePlaybackStream != null)
                            {
                                EventStreamState liveEss = ess.SelectedLivePlaybackStream.UserState as EventStreamState;

                                if ((liveEss != null) && (liveEss.IsSelectedForTargetPlayback))
                                {
                                    handled = true;

                                    uint eventIndex;
                                    if (ess.UpdateTime(time, out eventIndex))
                                    {
                                        if ((forceHandleEvents || ((this.playback == null) || (this.playbackState == KStudioPlaybackState.Paused))) && (this.pluginService != null))
                                        {
                                            KStudioSeekableEventStream seekableStream = stream as KStudioSeekableEventStream;
                                            if (seekableStream != null)
                                            {
                                                KStudioEvent eventObj = seekableStream.ReadEvent(eventIndex);
                                                if (eventObj != null)
                                                {
                                                    this.pluginService.HandleEvent(EventType.Inspection, eventObj);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (!handled)
                            {
                                ess.ClearTime();
                            }
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void Monitor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e != null) && (this.monitor != null))
            {
                try
                {
                    switch (e.PropertyName)
                    {
                        case "State":
                            this.MonitorState = this.monitor.State;
                            break;
                    }
                }
                catch (Exception)
                {
                    this.StopMonitor();
                }
            }
        }

        private void CloseMetadataViews(ISet<MetadataInfo> metadataViewsToClose)
        {
            DebugHelper.AssertUIThread();

            if (this.metadataViewService != null)
            {
                this.metadataViewService.CloseMetadataViews(metadataViewsToClose);
            }
        }

        private Thread thread = null;
        private AutoResetEvent threadStartEvent = null;
        private ManualResetEvent threadDoneEvent = null;
        private Action threadAction = null;
        private ILoggingService loggingService = null;
        private IUserNotificationService notificationService = null;
        private IMostRecentlyUsedService mruService = null;
        private IFileSettingsService fileSettingsService = null;
        private IPluginService pluginService = null;
        private IMetadataViewService metadataViewService = null;
        private readonly KStudioServiceSettings settings = null;
        private IPAddress targetAddress = null;
        private string targetAlias = null;
        private KStudioClient client = null;
        private bool connectedState = false;

        private KStudioRecording recording = null;
        private MetadataInfo recordingMetadata = null;
        private string recordingFilePath = null;
        private KStudioRecordingState recordingState = KStudioRecordingState.Error;
        private TimeSpan recordingTime = TimeSpan.Zero;
        private UInt64 recordingFileSize = 0;
        private UInt32 recordingBufferSize = 0;
        private UInt32 recordingInUseSize = 0;
        private bool areAllStreamsVisibleForRecording = false;

        private KStudioEventFile playbackFile = null;
        private bool playbackFileLocal = true;
        private KStudioPlayback playback = null;
        private KStudioEventStreamIdentifier? playbackStep = null;
        private IEnumerable playbackableFileStreams = null;
        private MetadataInfo playbackMetadata = null;
        private TimelineMarkersCollection playbackFileMarkers = null;
        private TimelinePausePointsCollection playbackPausePoints = null;
        private TimelineInOutPointsCollection playbackInOutPoints = null;
        private uint playbackLoopCount = 0;
        private uint playbackLoopIteration = 0;
        private TimeSpan playbackStartTime = TimeSpan.Zero;
        private bool areAllStreamsVisibleForPlayback = false;

        private string playbackFilePath = null;
        private KStudioPlaybackState playbackState = KStudioPlaybackState.Error;
        private TimeSpan playbackTime = TimeSpan.Zero;

        private KStudioMonitor monitor = null;
        private KStudioMonitorState monitorState = KStudioMonitorState.Error;
        private TimeSpan monitorTime = TimeSpan.Zero;
        private DispatcherTimer monitorTimer = null;
        private bool areAllStreamsVisibleForMonitor = false;

        private LastSelectedStreams lastRecordedStreams = null;
        private LastSelectedStreams lastMonitoredStreams = null;
        private LastSelectedStreams lastHiddenRecordableStreams = null;
        private LastSelectedStreams lastHiddenMonitorableStreams = null;
        private LastSelectedStreams lastHiddenPlaybackableStreams = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void RunThread()
        {
            ManualResetEvent done = this.threadDoneEvent;
            AutoResetEvent start = this.threadStartEvent;
            ILoggingService logger = this.loggingService;

            if ((done == null) || (start == null))
            {
                return;
            }

            done.Set();

            while (true)
            {
                start.WaitOne();

                Action action = this.threadAction;

                if (action == null)
                {
                    break;
                }

                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    if (logger != null)
                    {
                        App.Current.Dispatcher.BeginInvoke(new Action(() => logger.LogException(ex)));
                    }
                }

                done.Set();
            }
        }

        private void RunAsync(Action action)
        {
            DebugHelper.AssertUIThread();

            ManualResetEvent done = this.threadDoneEvent;
            AutoResetEvent start = this.threadStartEvent;

            if ((done != null) && (start != null) && (action != null))
            {
                done.Reset();

                this.threadAction = action;

                start.Set();

                while (!done.WaitOne(0))
                {
                    KStudio.ProcessNotifications();
                }

                this.threadAction = null;
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private readonly HashSet<KStudioEventStreamIdentifier> defaultRecordingStreams = new HashSet<KStudioEventStreamIdentifier>(new KStudioEventStreamIdentifier[] 
            {
                new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.Body, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.CompressedColor, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.Depth, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
            });

        private readonly HashSet<KStudioEventStreamIdentifier> defaultPlaybackStreams = new HashSet<KStudioEventStreamIdentifier>(new KStudioEventStreamIdentifier[] 
            {
                new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.CompressedColor, KStudioEventStreamSemanticIds.KinectDefaultSensorConsumer),
                new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.Depth, KStudioEventStreamSemanticIds.KinectDefaultSensorConsumer),
            });

        private readonly HashSet<KStudioEventStreamIdentifier> defaultMonitorStreams = new HashSet<KStudioEventStreamIdentifier>(new KStudioEventStreamIdentifier[] 
            {
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.DepthMonitor, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.BodyMonitor, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.BodyIndexMonitor, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.UncompressedColorMonitor, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.CompressedColorMonitor, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.TitleAudioMonitor, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
            });

        private readonly HashSet<KStudioEventStreamIdentifier> defaultHiddenRecordableStreams = new HashSet<KStudioEventStreamIdentifier>(new KStudioEventStreamIdentifier[] 
            {
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.Calibration, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.ColorSettings, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.Opaque, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.LongExposureIr, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.SystemAudio, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
            });

        private readonly HashSet<KStudioEventStreamIdentifier> defaultHiddenMonitorableStreams = new HashSet<KStudioEventStreamIdentifier>(new KStudioEventStreamIdentifier[] 
            {
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.CalibrationMonitor, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.SystemAudioMonitor, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
            });

        private readonly HashSet<KStudioEventStreamIdentifier> defaultHiddenPlaybackableStreams = new HashSet<KStudioEventStreamIdentifier>(new KStudioEventStreamIdentifier[] 
            {
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.Calibration, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.ColorSettings, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.Opaque, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.LongExposureIr, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.SystemAudio, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
            });

        private readonly HashSet<KStudioEventStreamIdentifier> obscureStreams = new HashSet<KStudioEventStreamIdentifier>(new KStudioEventStreamIdentifier[] 
            {
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.CommonModeIr, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.Interaction, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.RawIr, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
                new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.SystemInfo, KStudioEventStreamSemanticIds.KinectDefaultSensorProducer),
            });
    }
}
