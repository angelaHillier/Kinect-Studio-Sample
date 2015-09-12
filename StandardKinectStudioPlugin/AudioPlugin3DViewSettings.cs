//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System.Windows;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal class AudioPlugin3DViewSettings : AudioPluginViewSettings
    {
        public AudioPlugin3DViewSettings(AudioPlugin audioPlugin)
            : base(audioPlugin)
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
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void OtherIsSupplyingSurface()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void OtherIsSupplyingTexture()
        {
        }

        public override DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("AudioPlugin3DViewSettingsEditDataTemplate") as DataTemplate;
            }
        }

        public override IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            return new AudioPlugin3DViewSettings(this);
        }

        private AudioPlugin3DViewSettings(AudioPlugin3DViewSettings source)
            : base(source)
        {
        }
    }
}
