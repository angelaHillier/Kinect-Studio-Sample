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
    using viz = Microsoft.Xbox.Kinect.Viz;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal class RawIrPlugin2DViewSettings : IrPluginViewSettings, IPluginEditableViewSettings
    {
        internal enum RawIr2DViewType
        {
            DepthGrey = 0,
            DepthColor = 1,
            Ir = 2,
        }

        public RawIrPlugin2DViewSettings(IPluginService pluginService, EventType eventType) :
            base(pluginService, eventType)
        {
            this.viewType = RawIr2DViewType.DepthColor;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public override bool IsRendingOpaque
        {
            get
            {
                return true;
            }
        }

        public override bool OtherIsRenderingOpaque()
        {
            DebugHelper.AssertUIThread();

            return false;
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

        public override DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("RawIrPlugin2DViewSettingsEditDataTemplate") as DataTemplate;
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
                lock (RawIrPlugin2DViewSettings.lockObj)
                {
                    string str = XmlExtensions.GetAttribute(element, "viewType", String.Empty);
                    RawIr2DViewType temp;
                    if (RawIr2DViewType.TryParse(str, out temp))
                    {
                        this.viewType = temp;
                    }
                }
            }
        }

        public override void WriteTo(XElement element)
        {
            base.WriteTo(element);

            if (element != null)
            {
                element.RemoveAll();

                lock (RawIrPlugin2DViewSettings.lockObj)
                {
                    element.SetAttributeValue("viewType", this.viewType.ToString());
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public override IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            lock (RawIrPlugin2DViewSettings.lockObj)
            {
                return new RawIrPlugin2DViewSettings(this);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public RawIr2DViewType ViewType
        {
            get
            {
                lock (RawIrPlugin2DViewSettings.lockObj)
                {
                    return this.viewType;
                }
            }
            set
            {
                bool doEvent = false;

                lock (RawIrPlugin2DViewSettings.lockObj)
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

        private RawIrPlugin2DViewSettings(RawIrPlugin2DViewSettings source)
            :base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.viewType = source.viewType;
        }

        private RawIr2DViewType viewType;
        private readonly string viewTypeGroupName = Guid.NewGuid().ToString();

        private static readonly object lockObj = new object(); // no need for a lock for each instance
    }
}
