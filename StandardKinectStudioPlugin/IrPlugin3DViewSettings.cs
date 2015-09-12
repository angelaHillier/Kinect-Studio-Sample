//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System.Windows;
    using System.Collections.Generic;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal class IrPlugin3DViewSettings : IrPluginViewSettings, IPlugin3DViewSettings
    {
        public IrPlugin3DViewSettings(IPluginService pluginService, EventType eventType) :
            base(pluginService, eventType)
        {
        }

        public override string RequirementsToolTip
        {
            get
            {
                return Strings.Ir_Requirements_ToolTip;
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
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.Ir));
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.IrMonitor));
            }

            this.AreRequirementsSatisfied = satisfied;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsSupplyingSurface
        {
            get
            {
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsSupplyingTexture
        {
            get
            {
                return true;
            }
        }

        public bool OtherIsSupplyingSurface()
        {
            DebugHelper.AssertUIThread();

            return true;
        }

        public bool OtherIsSupplyingTexture()
        {
            DebugHelper.AssertUIThread();

            return false;
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

        public override DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("IrPluginViewSettingsEditDataTemplate") as DataTemplate;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public override IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            return new IrPlugin3DViewSettings(this);
        }

        private IrPlugin3DViewSettings(IrPlugin3DViewSettings source) 
            : base(source)
        {
        }
    }
}
