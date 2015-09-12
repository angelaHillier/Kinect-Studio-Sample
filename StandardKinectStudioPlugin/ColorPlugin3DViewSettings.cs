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

    internal class ColorPlugin3DViewSettings : ColorPluginViewSettings, IPlugin3DViewSettings
    {
        public ColorPlugin3DViewSettings()
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

            return new ColorPlugin3DViewSettings();
        }
    }
}
