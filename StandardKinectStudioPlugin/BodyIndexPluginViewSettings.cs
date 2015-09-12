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

    internal abstract class BodyIndexPluginViewSettings : KStudioUserState, IPluginEditableViewSettings
    {
        protected BodyIndexPluginViewSettings()
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
                return Resources.Get("BodyIndexPluginViewSettingsEditDataTemplate") as DataTemplate;
            }
        }

        public abstract IPluginEditableViewSettings CloneForEdit();

        public string RequirementsToolTip
        {
            get
            {
                return Strings.BodyIndex_Requirements_ToolTip;
            }
        }

        public bool AreRequirementsSatisfied
        {
            get
            {
                lock (BodyIndexPluginViewSettings.lockObj)
                {
                    return this.requirementsSatisified;
                }
            }
            protected set
            {
                DebugHelper.AssertUIThread();

                bool doEvent = false;

                lock (BodyIndexPluginViewSettings.lockObj)
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
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.BodyIndex));
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.BodyIndexMonitor));
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

        protected static readonly object lockObj = new object(); // no need for a lock for each instance

        private bool requirementsSatisified = true;
    }
}
