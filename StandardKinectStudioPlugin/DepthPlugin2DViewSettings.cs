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
    using System.Globalization;
    using System.Windows;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal class DepthPlugin2DViewSettings : KStudioUserState, IPluginEditableViewSettings
    {
        internal enum Depth2DViewType
        {
            Grey = 0,
            Color = 1,
        }

        public DepthPlugin2DViewSettings()
        {
            this.viewType = Depth2DViewType.Color;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public virtual bool IsRendingOpaque
        {
            get
            {
                return true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public virtual bool OtherIsRenderingOpaque()
        {
            return false;
        }

        public DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("DepthPlugin2DViewSettingsEditDataTemplate") as DataTemplate;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string ViewTypeGroupName
        {
            get
            {
                return this.viewTypeGroupName;
            }
        }

        public string RequirementsToolTip
        {
            get
            {
                return Strings.Depth_Requirements_ToolTip;
            }
        }

        public bool AreRequirementsSatisfied
        {
            get
            {
                lock (DepthPlugin2DViewSettings.lockObj)
                {
                    return this.requirementsSatisified;
                }
            }
            private set
            {
                DebugHelper.AssertUIThread();

                bool doEvent = false;

                lock (DepthPlugin2DViewSettings.lockObj)
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
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.Depth));
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.DepthMonitor));
            }

            this.AreRequirementsSatisfied = satisfied;
        }

        public void ReadFrom(XElement element)
        {
            if (element != null)
            {
                lock (DepthPlugin2DViewSettings.lockObj)
                {
                    string str = XmlExtensions.GetAttribute(element, "viewType", String.Empty);
                    Depth2DViewType temp;
                    if (Depth2DViewType.TryParse(str, out temp))
                    {
                        this.viewType = temp;
                    }
                }
            }
        }

        public void WriteTo(XElement element)
        {
            if (element != null)
            {
                element.RemoveAll();

                lock (DepthPlugin2DViewSettings.lockObj)
                {
                    element.SetAttributeValue("viewType", this.viewType.ToString());
                }
            }
        }

        public IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            lock (DepthPlugin2DViewSettings.lockObj)
            {
                return new DepthPlugin2DViewSettings(this);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Depth2DViewType ViewType
        {
            get
            {
                lock (DepthPlugin2DViewSettings.lockObj)
                {
                    return this.viewType;
                }
            }
            set
            {
                bool doEvent = false;

                lock (DepthPlugin2DViewSettings.lockObj)
                {
                    if (this.viewType != value)
                    {
                        doEvent = true;
                        this.viewType = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("ViewType");
                }
            }
        }

        private DepthPlugin2DViewSettings(DepthPlugin2DViewSettings source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.viewType = source.viewType;
        }

        private Depth2DViewType viewType;
        private readonly string viewTypeGroupName = Guid.NewGuid().ToString();

        private static readonly object lockObj = new object(); // no need for a lock for each instance

        private bool requirementsSatisified = true;
    }
}
