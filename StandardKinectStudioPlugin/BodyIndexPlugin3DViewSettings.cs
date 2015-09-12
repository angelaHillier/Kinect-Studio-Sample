//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal class BodyIndexPlugin3DViewSettings : BodyIndexPluginViewSettings, IPlugin3DViewSettings
    {
        public BodyIndexPlugin3DViewSettings()
        {
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

        public override IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            return new BodyIndexPlugin3DViewSettings();
        }
    }
}
