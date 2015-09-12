//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Net;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;

    public class BusyEventArgs : EventArgs
    {
        public BusyEventArgs(bool busy)
        {
            this.IsBusy = busy;
        }

        public bool IsBusy { get; private set; }
    }
 
    public interface IKStudioService : IAvailableStreams
    {
        void Initialize(IServiceProvider serviceProvider);
        void Shutdown();

        KStudioServiceSettings Settings { get; }

        KStudioClient Client { get; }
        bool IsTargetConnected { get; }
        bool ConnectToTarget(IPAddress targetAddress, string targetAlias);
        bool ConnectToTarget(string targetAlias);
        void DisconnectFromTarget();
        string TargetFilePath { get; }
        IPAddress TargetAddress { get; }
        string TargetAlias { get; }

        bool CanRecord { get; }
        bool HasRecording { get; }
        KStudioRecordingState RecordingState { get; }
        TimeSpan RecordingTime { get; }
        UInt32 RecordingBufferSizeMegabytes { get; }
        UInt32 RecordingBufferInUseMegabytes { get; }
        UInt64 RecordingFileSizeBytes { get; }
        string TargetRecordableStreamsTitle { get; }
        string TargetRecordableStreamsToolTip { get; }
        ICollectionView TargetRecordableStreams { get; }
        MetadataInfo RecordingMetadata { get; }
        string RecordingFilePath { get; }
        void StartRecording();
        void StopRecording();
        void CloseRecording(bool allowAutoPlayback);
        bool DoTargetRecordingDance(KStudioEventStream stream, bool newValue);
        void UpdateTargetRecordingVisibility(KStudioEventStream stream);
        bool AreAllStreamsVisibleForRecording { get; set; }

        bool CanPlayback { get; }
        bool HasPlaybackFile { get; }
        bool IsPlaybackFileReadOnly { get; }
        bool IsPlaybackFileOnTarget { get; }
        bool HasPlayback { get; }
        KStudioPlaybackState PlaybackState { get; }
        TimeSpan PlaybackTime { get; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Playbackable")]
        string PlaybackableStreamsTitle { get; }
        string PlaybackableStreamsToolTip { get; }
        IEnumerable PlaybackableFileStreams { get; }
        MetadataInfo PlaybackMetadata { get; }
        string PlaybackFilePath { get; }
        void OpenTargetPlayback(string filePath, bool asReadOnly);
        void OpenLocalPlayback(string filePath, bool asReadOnly);
        void PlayPlayback();
        void PausePlayback();
        void SeekPlayback(TimeSpan time);
        void StopPlayback();
        void StepPlayback();
        void ClosePlayback();
        bool DoTargetPlaybackDance(KStudioEventStream stream, bool newValue);
        void UpdatePlaybackVisibility(KStudioEventStream stream);
        TimelineMarkersCollection PlaybackFileMarkers { get; }
        TimelinePausePointsCollection PlaybackPausePoints { get; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "OutPoints")]
        TimelineInOutPointsCollection PlaybackInOutPoints { get; }
        uint PlaybackLoopCount { get; set; }
        uint PlaybackLoopIteration { get; set; }
        bool AreAllStreamsVisibleForPlayback { get; set; }

        bool CanMonitor { get; }
        bool HasMonitor { get; }
        KStudioMonitorState MonitorState { get; }
        TimeSpan MonitorTime { get; }
        string TargetMonitorableStreamsTitle { get; }
        string TargetMonitorableStreamsToolTip { get; }
        ICollectionView TargetMonitorableStreams { get; }
        void StartMonitor();
        void StopMonitor();
        bool DoTargetMonitorDance(KStudioEventStream stream, bool newValue);
        void UpdateTargetMonitorVisibility(KStudioEventStream stream);
        bool AreAllStreamsVisibleForMonitor { get; set; }

        event EventHandler PlaybackOpened;
        event EventHandler<BusyEventArgs> Busy;
    }
}
