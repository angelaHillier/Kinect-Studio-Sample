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

    internal class IrPlugin2DViewSettings 
        : IrPluginViewSettings
    {
        public IrPlugin2DViewSettings(IPluginService pluginService, EventType eventType)  :
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
            return new IrPlugin2DViewSettings(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public override bool IsRendingOpaque
        {
            get
            {
                return true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public override bool OtherIsRenderingOpaque()
        {
            return false;
        }

        private IrPlugin2DViewSettings(IrPlugin2DViewSettings source)
            : base(source)
        {
        }
    }
}
