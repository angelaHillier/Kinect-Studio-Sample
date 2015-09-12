//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using KinectStudioPlugin;

    internal class BodyIndexPlugin2DViewSettings
        : BodyIndexPluginViewSettings
    {
        public BodyIndexPlugin2DViewSettings()
            : base()
        {
        }

        public override IPluginEditableViewSettings CloneForEdit()
        {
            return new BodyIndexPlugin2DViewSettings();
        }
    }
}
