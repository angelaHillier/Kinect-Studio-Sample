//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using viz = Microsoft.Xbox.Kinect.Viz;

    public interface I2DVisualPlugin : IVisualPlugin
    {
        IPluginViewSettings Add2DView(EventType eventType, Panel hostControl);
        IPluginViewSettings Add2DPropertyView(ContentControl hostControl);
        void Render2D(EventType eventType, IPluginViewSettings pluginViewSettings, viz.Context context, viz.Texture texture, float left, float top, float width, float height);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        void UpdatePropertyView(EventType eventType, double x, double y, uint width, uint height);
        void ClearPropertyView();
    }
} 
