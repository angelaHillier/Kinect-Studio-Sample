//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal abstract class ColorPluginViewSettings : KStudioUserState, IPluginEditableViewSettings
    {
        protected ColorPluginViewSettings()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsRendingOpaque
        {
            get
            {
                return true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public bool OtherIsRenderingOpaque()
        {
            return false;
        }

        public DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("ColorPluginViewSettingsEditDataTemplate") as DataTemplate;
            }
        }

        public string RequirementsToolTip
        {
            get
            {
                return Strings.Color_Requirements_ToolTip;
            }
        }

        public bool AreRequirementsSatisfied
        {
            get
            {
                lock (ColorPluginViewSettings.lockObj)
                {
                    return this.requirementsSatisified;
                }
            }
            private set
            {
                DebugHelper.AssertUIThread();

                bool doEvent = false;

                lock (ColorPluginViewSettings.lockObj)
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
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.CompressedColor));
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.CompressedColorMonitor));
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.UncompressedColor));
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.UncompressedColorMonitor));
            }

            this.AreRequirementsSatisfied = satisfied;
        }

        public void ReadFrom(XElement element)
        {
            // nothing persisted
        }

        public void WriteTo(XElement element)
        {
            // nothing persisted
        }

        public abstract IPluginEditableViewSettings CloneForEdit();

        private bool requirementsSatisified = true;

        protected static readonly object lockObj = new object(); // no need for a lock for each instance
    }
}
