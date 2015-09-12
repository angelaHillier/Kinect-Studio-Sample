//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Xml.Linq;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal class AudioPlugin2DViewSettings : AudioPluginViewSettings
    {
        public AudioPlugin2DViewSettings(AudioPlugin audioPlugin) 
            : base(audioPlugin)
        {
            options = new bool[AudioPlugin2DViewSettings.cAudioTrackCount];

            options[(int)AudioTrack.Output] = true;
        }

        public override DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("AudioPlugin2DViewSettingsEditDataTemplate") as DataTemplate;
            }
        }

        public override IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            lock (AudioPlugin2DViewSettings.lockObj)
            {
                return new AudioPlugin2DViewSettings(this);
            }
        }

        public bool GetTrackOption(AudioTrack option)
        {
            int index = (int)option;
            Debug.Assert((index >= 0) && (index < AudioPlugin2DViewSettings.cAudioTrackCount));

            lock (AudioPlugin2DViewSettings.lockObj)
            {
                return this.options[index];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderOutput
        {
            get
            {
                return GetTrackOption(AudioTrack.Output);
            }
            set
            {
                SetTrackOption(AudioTrack.Output, value, "RenderOutput");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderBeam
        {
            get
            {
                lock (AudioPlugin2DViewSettings.lockObj)
                {
                    return this.beam;
                }
            }
            set
            {
                bool doEvent = false;

                lock (AudioPlugin2DViewSettings.lockObj)
                {
                    if (this.beam != value)
                    {
                        doEvent = true;
                        this.beam = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("RenderBeam");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderMic0
        {
            get
            {
                return GetTrackOption(AudioTrack.Mic0);
            }
            set
            {
                SetTrackOption(AudioTrack.Mic0, value, "RenderMic0");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderMic1
        {
            get
            {
                return GetTrackOption(AudioTrack.Mic1);
            }
            set
            {
                SetTrackOption(AudioTrack.Mic1, value, "RenderMic1");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderMic2
        {
            get
            {
                return GetTrackOption(AudioTrack.Mic2);
            }
            set
            {
                SetTrackOption(AudioTrack.Mic2, value, "RenderMic2");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderMic3
        {
            get
            {
                return GetTrackOption(AudioTrack.Mic3);
            }
            set
            {
                SetTrackOption(AudioTrack.Mic3, value, "RenderMic3");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderSpeakerL
        {
            get
            {
                return GetTrackOption(AudioTrack.SpeakerL);
            }
            set
            {
                SetTrackOption(AudioTrack.SpeakerL, value, "RenderSpeakerL");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderSpeakerR
        {
            get
            {
                return GetTrackOption(AudioTrack.SpeakerR);
            }
            set
            {
                SetTrackOption(AudioTrack.SpeakerR, value, "RenderSpeakerR");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderSpeakerC
        {
            get
            {
                return GetTrackOption(AudioTrack.SpeakerC);
            }
            set
            {
                SetTrackOption(AudioTrack.SpeakerC, value, "RenderSpeakerC");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderSpeakerLFE
        {
            get
            {
                return GetTrackOption(AudioTrack.SpeakerLFE);
            }
            set
            {
                SetTrackOption(AudioTrack.SpeakerLFE, value, "RenderSpeakerLFE");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderSpeakerBL
        {
            get
            {
                return GetTrackOption(AudioTrack.SpeakerBL);
            }
            set
            {
                SetTrackOption(AudioTrack.SpeakerBL, value, "RenderSpeakerBL");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderSpeakerBR
        {
            get
            {
                return GetTrackOption(AudioTrack.SpeakerBR);
            }
            set
            {
                SetTrackOption(AudioTrack.SpeakerBR, value, "RenderSpeakerBR");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderSpeakerSL
        {
            get
            {
                return GetTrackOption(AudioTrack.SpeakerSL);
            }
            set
            {
                SetTrackOption(AudioTrack.SpeakerSL, value, "RenderSpeakerSL");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderSpeakerSR
        {
            get
            {
                return GetTrackOption(AudioTrack.SpeakerSR);
            }
            set
            {
                SetTrackOption(AudioTrack.SpeakerSR, value, "RenderSpeakerSR");
            }
        }

        protected override void OnReadFrom(XElement element)
        {
            if (element != null)
            {
                lock (AudioPlugin2DViewSettings.lockObj)
                {
                    if (this.options != null)
                    {
                        this.beam = XmlExtensions.GetAttribute(element, "beam", this.beam);

                        foreach (AudioTrack option in Enum.GetValues(typeof(AudioTrack)))
                        {
                            int index = (int)option;
                            Debug.Assert((index >= 0) && (index < AudioPlugin2DViewSettings.cAudioTrackCount));

                            this.options[index] = XmlExtensions.GetAttribute(element, "track" + option.ToString(), this.options[index]);
                        }
                    }
                }
            }
        }

        protected override void OnWriteTo(XElement element)
        {
            if (element != null)
            {
                lock (AudioPlugin2DViewSettings.lockObj)
                {
                    if (this.options != null)
                    {
                        foreach (AudioTrack option in Enum.GetValues(typeof(AudioTrack)))
                        {
                            element.SetAttributeValue("beam", this.beam.ToString());

                            int index = (int)option;
                            Debug.Assert((index >= 0) && (index < AudioPlugin2DViewSettings.cAudioTrackCount));

                            element.SetAttributeValue("track" + option.ToString(), this.options[index]);
                        }
                    }
                }
            }
        }

        private AudioPlugin2DViewSettings(AudioPlugin2DViewSettings source)
            : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (source.options != null)
            {
                this.options = source.options.Clone() as bool[];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void SetTrackOption(AudioTrack option, bool value, string propertyName)
        {
            int index = (int)option;
            Debug.Assert((index >= 0) && (index < AudioPlugin2DViewSettings.cAudioTrackCount));
            Debug.Assert(!String.IsNullOrEmpty(propertyName));

            bool doEvent = false;

            lock (AudioPluginViewSettings.lockObj)
            {
                if (this.options[index] != value)
                {
                    doEvent = true;
                    this.options[index] = value;
                }
            }

            if (doEvent)
            {
                RaisePropertyChanged(propertyName);
            }
        }

        private static readonly int cAudioTrackCount = Enum.GetNames(typeof(AudioTrack)).Length;

        private bool[] options = null;
        private bool beam = true;
    }
}
