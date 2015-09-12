//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using KinectStudioPlugin;

    internal class ColorPlugin2DViewSettings
        : ColorPluginViewSettings
    {
        public ColorPlugin2DViewSettings()
            : base()
        {
        }

        public override IPluginEditableViewSettings CloneForEdit()
        {
            return new ColorPlugin2DViewSettings();
        }
    }
}
