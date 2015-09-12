//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal abstract class AudioPluginViewSettings : KStudioUserState, IPluginEditableViewSettings
    {
        protected AudioPluginViewSettings(AudioPlugin audioPlugin)
        {
            if (audioPlugin == null)
            {
                throw new ArgumentNullException("audioPlugin");
            }

            this.audioPlugin = audioPlugin;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsRendingOpaque
        {
            get
            {
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public bool OtherIsRenderingOpaque()
        {
            return true;
        }

        public abstract DataTemplate SettingsEditDataTemplate { get; }

        public string RequirementsToolTip
        {
            get
            {
                return Strings.Audio_Requirements_ToolTip;
            }
        }

        public bool AreRequirementsSatisfied
        {
            get
            {
                lock (AudioPluginViewSettings.lockObj)
                {
                    return this.requirementsSatisified;
                }
            }
            private set
            {
                DebugHelper.AssertUIThread();

                bool doEvent = false;

                lock (AudioPluginViewSettings.lockObj)
                {
                    if (this.requirementsSatisified != value)
                    {
                        this.requirementsSatisified = value;
                        doEvent = true;
                    }
                }

                if (doEvent)
                {
                    this.RaisePropertyChanged("AreRequirementsSatisfied");
                }
            }
        }

        public void CheckRequirementsSatisfied(HashSet<KStudioEventStreamIdentifier> availableStreamIds)
        {
            bool satisfied = false;

            if (availableStreamIds == null)
            {
                satisfied = true;
            }
            else
            {
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.TitleAudio));
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.TitleAudioMonitor));
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.SystemAudio));
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.SystemAudioMonitor));
            }

            this.AreRequirementsSatisfied = satisfied;
        }

        public void ReadFrom(XElement element)
        {
            if (element != null)
            {
                lock (AudioPluginViewSettings.lockObj)
                {
                    OnReadFrom(element);
                }
            }
        }

        public void WriteTo(XElement element)
        {
            if (element != null)
            {
                element.RemoveAll();

                lock (AudioPluginViewSettings.lockObj)
                {
                    OnWriteTo(element);
                }
            }
        }

        public abstract IPluginEditableViewSettings CloneForEdit();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public AudioPlugin Plugin
        {
            get
            {
                lock (AudioPluginViewSettings.lockObj)
                {
                    return this.audioPlugin;
                }
            }
        }

        protected virtual void OnReadFrom(XElement element) { }

        protected virtual void OnWriteTo(XElement element) { }

        protected AudioPluginViewSettings(AudioPluginViewSettings source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.audioPlugin = source.audioPlugin;
        }

        protected static readonly object lockObj = new object(); // no need for a lock for each instance

        private readonly AudioPlugin audioPlugin;
        private bool requirementsSatisified = true;
    }
}
