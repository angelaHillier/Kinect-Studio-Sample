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
    using System.Linq;
    using System.IO;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    public class KStudioServiceSettings : KStudioUserState
    {
        public bool AutoTargetConnectOnStartup
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.autoTargetConnectOnStartUp;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.autoTargetConnectOnStartUp != value)
                {
                    this.autoTargetConnectOnStartUp = value;
                    RaisePropertyChanged("AutoTargetConnectOnStartUp");
                }
            }
        }

        public bool AutoMonitorOnTargetConnect
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.autoMonitorOnTargetConnect;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.autoMonitorOnTargetConnect != value)
                {
                    this.autoMonitorOnTargetConnect = value;
                    RaisePropertyChanged("AutoMonitorOnTargetConnect");
                }
            }
        }

        public bool AutoMonitorOnTargetPlayback
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.autoMonitorOnTargetPlayback;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.autoMonitorOnTargetPlayback != value)
                {
                    this.autoMonitorOnTargetPlayback = value;
                    RaisePropertyChanged("AutoMonitorOnTargetPlayback");
                }
            }
        }

        public bool AutoStopMonitorOnRecord
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.autoStopMonitorOnRecord;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.autoStopMonitorOnRecord != value)
                {
                    this.autoStopMonitorOnRecord = value;
                    RaisePropertyChanged("AutoStopMonitorOnRecord");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        public bool AutoPlaybackOnRecordStop
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.autoPlaybackOnRecordStop;
            }
            set
            {
                DebugHelper.AssertUIThread();

#if false

                if (this.autoPlaybackOnRecordStop != value)
                {
                    this.autoPlaybackOnRecordStop = value;
                    RaisePropertyChanged("AutoPlaybackOnRecordStop");
                }
#endif // false
            }
        }

        public bool AdvancedModeObscureStreams
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.advancedModeObscureStreams;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.advancedModeObscureStreams != value)
                {
                    this.advancedModeObscureStreams = value;
                    RaisePropertyChanged("AdvancedModeObscureStreams");
                }
            }
        }

        public UInt32 RecordingBufferSizeMB
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.recordingBufferSizeMB;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.recordingBufferSizeMB != value)
                {
                    this.recordingBufferSizeMB = value;
                    RaisePropertyChanged("RecordingBufferSizeMB");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public string TargetFilePath
        {
            get
            {
                DebugHelper.AssertUIThread();

                if (String.IsNullOrWhiteSpace(this.targetFilePath) || !Path.IsPathRooted(this.targetFilePath))
                {
                    this.targetFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    this.targetFilePath = Path.Combine(this.targetFilePath, "Kinect Studio\\Repository");

                    // If this path is bad at the time of recording, the user will get an error and can fix the path.
                    // We don't want to use an invalid dir before that time, because the dir could be on a network share that the
                    // user hasn't connected to yet (it would annoy user if they had to keep fixing it over and over).
                }

                return this.targetFilePath;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.targetFilePath != value)
                {
                    this.targetFilePath = value;
                    RaisePropertyChanged("TargetFilePath");
                }
            }
        }

        private bool autoTargetConnectOnStartUp = false;
        private bool autoMonitorOnTargetConnect = true;
        private bool autoMonitorOnTargetPlayback = true;
        private bool autoStopMonitorOnRecord = false;
        private bool autoPlaybackOnRecordStop = true;
        private bool advancedModeObscureStreams = false;
        private UInt32 recordingBufferSizeMB = 1024; // 1 gigabyte 
        private string targetFilePath = null;
    }
}
