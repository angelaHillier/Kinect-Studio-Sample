//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System.Windows;
    using viz = Microsoft.Xbox.Kinect.Viz;

    public interface IImageVisualPlugin : IVisualPlugin
    {
        void InitializeRender(EventType eventType, viz.Context context);
        void UninitializeRender(EventType eventType);

        viz.Texture GetTexture(EventType eventType, IPluginViewSettings pluginViewSettings);
    }
}
