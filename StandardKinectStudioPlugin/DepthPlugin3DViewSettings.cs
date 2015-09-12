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

    internal class DepthPlugin3DViewSettings : KStudioUserState, IPlugin3DViewSettings
    {
        internal enum Depth3DViewType
        {
            Grey = 0,
            Color = 1,
            SurfaceNormal = 2,
        }

        public DepthPlugin3DViewSettings()
        {
        }

        public bool IsSupplyingSurface
        {
            get
            {
                lock (DepthPlugin3DViewSettings.lockObj)
                {
                    return this.surface;
                }
            }
            set
            {
                bool doEvent = false;

                lock (DepthPlugin3DViewSettings.lockObj)
                {
                    if (this.surface != value)
                    {
                        doEvent = true;
                        this.surface = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("IsSupplyingSurface");
                    RaisePropertyChanged("IsSupplyingTexture");
                    RaisePropertyChanged("IsRendingOpaque");
                }
            }
        }

        public bool IsSupplyingTexture
        {
            get
            {
                lock (DepthPlugin3DViewSettings.lockObj)
                {
                    return !this.surface;
                }
            }
        }

        public bool IsRendingOpaque
        {
            get
            {
                lock (DepthPlugin3DViewSettings.lockObj)
                {
                    return !this.surface;
                }
            }
        }

        public bool OtherIsSupplyingSurface()
        {
            DebugHelper.AssertUIThread();

            if (this.checkReentrant > 0)
            {
                return false;
            }

            this.checkReentrant++;

            this.IsSupplyingSurface = false;

            this.checkReentrant--;

            return true;
        }

        public bool OtherIsSupplyingTexture()
        {
            DebugHelper.AssertUIThread();

            if (this.checkReentrant > 0)
            {
                return false;
            }

            bool value = false;

            this.checkReentrant++;

            if (this.surface)
            {
                value = true;
            }

            this.checkReentrant--;

            return value;
        }

        public bool OtherIsRenderingOpaque()
        {
            DebugHelper.AssertUIThread();

            if (this.checkReentrant > 0)
            {
                return false;
            }

            bool value = false;

            this.checkReentrant++;

            if (this.surface)
            {
                value = true;
            }
            else
            {
                this.IsSupplyingSurface = true;
            }

            this.checkReentrant--;

            return value;
        }

        public DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("DepthPlugin3DViewSettingsEditDataTemplate") as DataTemplate;
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
                lock (DepthPlugin3DViewSettings.lockObj)
                {
                    return this.requirementsSatisified;
                }
            }
            private set
            {
                DebugHelper.AssertUIThread();

                bool doEvent = false;

                lock (DepthPlugin3DViewSettings.lockObj)
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
                lock (DepthPlugin3DViewSettings.lockObj)
                {
                    string str = XmlExtensions.GetAttribute(element, "viewType", String.Empty);
                    Depth3DViewType temp;
                    if (Depth3DViewType.TryParse(str, out temp))
                    {
                        this.viewType = temp;
                    }

                    this.surface = XmlExtensions.GetAttribute(element, "surface", false);
                }
            }
        }

        public void WriteTo(XElement element)
        {
            if (element != null)
            {
                element.RemoveAll();

                lock (DepthPlugin3DViewSettings.lockObj)
                {
                    element.SetAttributeValue("viewType", this.viewType.ToString());
                    element.SetAttributeValue("surface", this.surface.ToString());
                }
            }
        }

        public IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            lock (DepthPlugin3DViewSettings.lockObj)
            {
                return new DepthPlugin3DViewSettings(this);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Depth3DViewType ViewType
        {
            get
            {
                lock (DepthPlugin3DViewSettings.lockObj)
                {
                    return this.viewType;
                }
            }
            set
            {
                bool doEvent = false;

                lock (DepthPlugin3DViewSettings.lockObj)
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

        private DepthPlugin3DViewSettings(DepthPlugin3DViewSettings source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.surface = source.surface;
            this.viewType = source.viewType;
        }

        private bool surface = false;
        private Depth3DViewType viewType = Depth3DViewType.Grey;
        private readonly string viewTypeGroupName = Guid.NewGuid().ToString();
        private uint checkReentrant = 0;

        private static readonly object lockObj = new object(); // no need for a lock for each instance

        private bool requirementsSatisified = true;
    }
}
