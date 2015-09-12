//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System;
    using System.Windows;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal class BodyPlugin2DViewSettings : BodyPluginViewSettings
    {
        public BodyPlugin2DViewSettings()
        {
        }

        public override DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("BodyPlugin2DViewSettingsEditDataTemplate") as DataTemplate;
            }
        }

        public override IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            lock (BodyPlugin2DViewSettings.lockObj)
            {
                return new BodyPlugin2DViewSettings(this);
            }
        }

        private BodyPlugin2DViewSettings(BodyPlugin2DViewSettings source)
            : base(source)
        {
        }
    }
}
