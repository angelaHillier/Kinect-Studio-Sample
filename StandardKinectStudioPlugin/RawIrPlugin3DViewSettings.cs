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
    using viz = Microsoft.Xbox.Kinect.Viz;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal class RawIrPlugin3DViewSettings : IrPluginViewSettings, IPlugin3DViewSettings
    {
        internal enum RawIr3DViewType
        {
            DepthGrey = 0,
            DepthColor = 1,
            SurfaceNormal = 2,
            Ir = 3,
        }

        public RawIrPlugin3DViewSettings(IPluginService pluginService, EventType eventType) :
            base(pluginService, eventType)
        {
            this.viewType = RawIr3DViewType.DepthColor;
        }

        public override string RequirementsToolTip
        {
            get
            {
                return Strings.RawIr_Requirements_ToolTip;
            }
        }

        public override void CheckRequirementsSatisfied(HashSet<KStudioEventStreamIdentifier> availableStreamIds)
        {
            bool satisfied = false;

            if (availableStreamIds == null)
            {
                satisfied = true;
            }
            else
            {
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.RawIr));
            }

            this.AreRequirementsSatisfied = satisfied;
        }

        public bool IsSupplyingSurface
        {
            get
            {
                lock (RawIrPlugin3DViewSettings.lockObj)
                {
                    return this.surface;
                }
            }
            set
            {
                bool doEvent = false;

                lock (RawIrPlugin3DViewSettings.lockObj)
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
                lock (RawIrPlugin3DViewSettings.lockObj)
                {
                    return !this.surface;
                }
            }
        }

        public override bool IsRendingOpaque
        {
            get
            {
                lock (RawIrPlugin3DViewSettings.lockObj)
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

        public override bool OtherIsRenderingOpaque()
        {
            DebugHelper.AssertUIThread();

            return false;
        }

        public override DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("RawIrPlugin3DViewSettingsEditDataTemplate") as DataTemplate;
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

        public override void ReadFrom(XElement element)
        {
            base.ReadFrom(element);

            if (element != null)
            {
                lock (RawIrPlugin3DViewSettings.lockObj)
                {
                    string str = XmlExtensions.GetAttribute(element, "viewType", String.Empty);
                    RawIr3DViewType temp;
                    if (RawIr3DViewType.TryParse(str, out temp))
                    {
                        this.viewType = temp;
                    }

                    this.surface = XmlExtensions.GetAttribute(element, "surface", false);
                }
            }
        }

        public override void WriteTo(XElement element)
        {
            base.WriteTo(element);

            if (element != null)
            {
                element.RemoveAll();

                lock (RawIrPlugin3DViewSettings.lockObj)
                {
                    element.SetAttributeValue("viewType", this.viewType.ToString());
                    element.SetAttributeValue("surface", this.surface.ToString());
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public override IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            lock (RawIrPlugin3DViewSettings.lockObj)
            {
                return new RawIrPlugin3DViewSettings(this);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public RawIr3DViewType ViewType
        {
            get
            {
                lock (RawIrPlugin3DViewSettings.lockObj)
                {
                    return this.viewType;
                }
            }
            set
            {
                bool doEvent = false;

                lock (RawIrPlugin3DViewSettings.lockObj)
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

        private RawIrPlugin3DViewSettings(RawIrPlugin3DViewSettings source)
            : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.surface = source.surface;
            this.viewType = source.viewType;
        }

        private bool surface = false;
        private RawIr3DViewType viewType;
        private readonly string viewTypeGroupName = Guid.NewGuid().ToString();
        private uint checkReentrant = 0;

        private static readonly object lockObj = new object(); // no need for a lock for each instance
    }
}
