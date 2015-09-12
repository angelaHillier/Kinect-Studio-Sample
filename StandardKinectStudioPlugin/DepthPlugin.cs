//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Kinect.Tools;
    using nui = Microsoft.Xbox.Input.Nui;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    public class DepthPlugin : BasePlugin, IEventHandlerPlugin, I2DVisualPlugin, I3DVisualPlugin, IImageVisualPlugin, IDisposable
    {
        public DepthPlugin(IServiceProvider serviceProvider)
            : base(Strings.Depth_Plugin_Title, new Guid(0x4fc932f6, 0x77a4, 0x4a22, 0xbe, 0x1f, 0x93, 0x42, 0x4d, 0x8e, 0xbb, 0x1a))
        {
            if (serviceProvider != null)
            {
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }
        }

        ~DepthPlugin()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        public bool HasSelected2DPixelData
        {
            get
            {
                bool value = false;

                lock (this.lockObj)
                {
                    if (this.selectedData != null)
                    {
                        value = this.selectedData.sharedDepthFrame != null;
                    }
                }

                return value;
            }
        }

        public ushort Selected2DPixelDepth
        {
            get
            {
                ushort value = 0;

                lock (this.lockObj)
                {
                    if (this.selectedData != null)
                    {
                        value = this.selectedData.selected2DPixelDepth;
                    }
                }

                return value;
            }
        }

        public uint Selected2DPixelX
        {
            get
            {
                uint value = 0;

                lock (this.lockObj)
                {
                    if (this.selectedData != null)
                    {
                        value = this.selectedData.selected2DPixelX;
                    }
                }

                return value;
            }
        }

        public uint Selected2DPixelY
        {
            get
            {
                uint value = 0;

                lock (this.lockObj)
                {
                    if (this.selectedData != null)
                    {
                        value = this.selectedData.selected2DPixelY;
                    }
                }

                return value;
            }
        }

        public void InitializeRender(EventType eventType, viz.Context context)
        {
            switch (eventType)
            {
                case EventType.Monitor:
                    Debug.Assert(this.monitorData != null);
                    if ((this.monitorData.imageWidth != 0) && (this.monitorData.imageHeight != 0) && (this.monitorData.sharedDepthFrame != null))
                    {
                        DepthPlugin.CreateTextures(this.monitorData, context);
                        if (this.monitorData.depthMap != null)
                        {
                            unsafe
                            {
                                this.monitorData.depthMap.UpdateData((ushort*)this.monitorData.sharedDepthFrame.Buffer, this.monitorData.sharedDepthFrame.Size);
                            }
                        }
                    }
                    break;

                case EventType.Inspection:
                    Debug.Assert(this.inspectionData != null);
                    if ((this.inspectionData.imageWidth != 0) && (this.inspectionData.imageHeight != 0) && (this.inspectionData.sharedDepthFrame != null))
                    {
                        DepthPlugin.CreateTextures(this.inspectionData, context);
                        if (this.inspectionData.depthMap != null)
                        {
                            unsafe
                            {
                                this.inspectionData.depthMap.UpdateData((ushort*)this.inspectionData.sharedDepthFrame.Buffer, this.inspectionData.sharedDepthFrame.Size);
                            }
                        }
                    }
                    break;
            }
        }

        public void UninitializeRender(EventType eventType)
        {
            lock (this.lockObj)
            {
                switch (eventType)
                {
                    case EventType.Monitor:
                        Debug.Assert(this.monitorData != null);
                        DepthPlugin.DisposeData(this.monitorData, false);
                        break;

                    case EventType.Inspection:
                        Debug.Assert(this.inspectionData != null);
                        DepthPlugin.DisposeData(this.inspectionData, false);
                        break;
                }
            }
        }

        public bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId)
        {
            return (dataTypeId == KStudioEventStreamDataTypeIds.Depth) || (dataTypeId == HackKStudioEventStreamDataTypeIds.DepthMonitor) ||
                   (dataTypeId == HackKStudioEventStreamDataTypeIds.Calibration) || (dataTypeId == HackKStudioEventStreamDataTypeIds.CalibrationMonitor) ||
                   (dataTypeId == HackKStudioEventStreamDataTypeIds.SystemInfo);
        }

        public void ClearEvents(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Monitor:
                    this.ClearEvents(this.monitorData);
                    break;

                case EventType.Inspection:
                    this.ClearEvents(this.inspectionData);
                    break;
            }
        }

        public void HandleEvent(EventType eventType, KStudioEvent eventObj)
        {
            switch (eventType)
            {
                case EventType.Monitor:
                    this.HandleEvent(eventType, eventObj, this.monitorData);
                    break;

                case EventType.Inspection:
                    this.HandleEvent(eventType, eventObj, this.inspectionData);
                    break;
            }
        }

        public IPluginViewSettings Add2DPropertyView(ContentControl hostControl)
        {
            DataTemplate dataTemplate = Resources.Get("DepthPluginPropertyViewDataTemplate") as DataTemplate;
            if ((dataTemplate != null) && (hostControl != null))
            {
                hostControl.ContentTemplate = dataTemplate;
                hostControl.Content = this;
            }

            return null;
        }

        public void UpdatePropertyView(EventType eventType, double x, double y, uint width, uint height)
        {
            switch (eventType)
            {
                case EventType.Monitor:
                    this.UpdatePropertyView(x, y, width, height, this.monitorData);
                    break;

                case EventType.Inspection:
                    this.UpdatePropertyView(x, y, width, height, this.inspectionData);
                    break;
            }
        }

        public void ClearPropertyView()
        {
            bool doEvent = false;

            lock (this.lockObj)
            {
                if (this.selectedData != null)
                {
                    doEvent = true;
                    this.selectedData = null;
                }
            }

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
                this.RaisePropertyChanged("Selected2DPixelX");
                this.RaisePropertyChanged("Selected2DPixelY");
                this.RaisePropertyChanged("Selected2DPixelDepth");
            }
        }

        public void RemoveView(Guid viewId, Panel hostControl, IPluginViewSettings pluginViewSettings)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Windows.Controls.TextBlock.set_Text(System.String)")]
        public IPluginViewSettings Add2DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new DepthPlugin2DViewSettings();

#if TODO_PLAYING_AROUND
            if (hostControl != null)
            {
                Border border = new Border()
                    {
                        BorderBrush = System.Windows.Media.Brushes.Yellow,
                        BorderThickness = new System.Windows.Thickness(2),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
                    };
                border.SetValue(Canvas.LeftProperty, 256.0);
                border.SetValue(Canvas.TopProperty, 200.0);
                TextBlock text = new TextBlock()
                    {
                        Text = "TEST",
                        Foreground = System.Windows.Media.Brushes.Blue,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                    };
                border.Child = text;
                hostControl.Children.Add(border);
            }
#endif // TODO_PLAYING_AROUND

            return pluginViewSettings;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Windows.Controls.TextBlock.set_Text(System.String)")]
        public IPluginViewSettings Add3DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new DepthPlugin3DViewSettings();

#if TODO_PLAYING_AROUND
            if (hostControl != null)
            {
                Border border = new Border()
                    {
                        BorderBrush = System.Windows.Media.Brushes.Yellow,
                        BorderThickness = new System.Windows.Thickness(2),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = System.Windows.VerticalAlignment.Top,
                    };
                TextBlock text = new TextBlock()
                    {
                        Text = "TEST",
                        Foreground = System.Windows.Media.Brushes.Blue,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    };
                border.Child = text;
                hostControl.Children.Add(border);
            }
#endif // TODO_PLAYING_AROUND

            return pluginViewSettings;
        }

        public viz.Texture GetTexture(EventType eventType, IPluginViewSettings pluginViewSettings)
        {
            viz.Texture value = null;

            switch (eventType)
            {
                case EventType.Monitor:
                    value = this.GetTexture(pluginViewSettings, this.monitorData);
                    break;

                case EventType.Inspection:
                    value = this.GetTexture(pluginViewSettings, this.inspectionData);
                    break;
            }

            return value;
        }

        public void Render2D(EventType eventType, IPluginViewSettings pluginViewSettings, viz.Context context, viz.Texture texture, float left, float top, float width, float height)
        {
        }

        public void Render3D(EventType eventType, IPluginViewSettings pluginViewSettings, viz.Context context, viz.Texture texture)
        {
            lock (this.lockObj)
            {
                EventTypePluginData data = null;
                nui.Registration registration = null;

                switch (eventType)
                {
                    case EventType.Monitor:
                        data = this.monitorData;
                        break;

                    case EventType.Inspection:
                        data = this.inspectionData;
                        break;
                }

                if (data != null)
                {
                    if (this.pluginService != null)
                    {
                        registration = this.pluginService.GetRegistration(eventType);

                        if (data.lastRegistration != registration)
                        {
                            data.lastRegistration = this.pluginService.GetRegistration(eventType);

                            if ((data.lastRegistration != null) && (data.depthMap != null))
                            {
                                uint xyTableSize;
                                IntPtr xyTable = data.lastRegistration.GetXYTable(out xyTableSize);

                                data.depthMap.UpdateXYTable(xyTable, xyTableSize);
                            }
                        }
                    }

                    DepthPlugin.Render3D(pluginViewSettings, texture, data, registration);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.lockObj)
                {
                    DepthPlugin.DisposeData(this.monitorData, true);
                    DepthPlugin.DisposeData(this.inspectionData, true);
                }
            }
        }

        // object owning data should be locked
        private static void DisposeData(EventTypePluginData data, bool doAll)
        {
            Debug.Assert(data != null);

            if (doAll)
            {
                // don't dispose this because others may be sharing the data
                data.sharedDepthFrame = null;
            }

            if (data.depthTexture != null)
            {
                data.depthTexture.Dispose();
                data.depthTexture = null;
            }

            if (data.depthMap != null)
            {
                data.depthMap.Dispose();
                data.depthMap = null;
            }

            if (data.uvTable != null)
            {
                data.uvTable.Dispose();
                data.uvTable = null;
            }
        }

        private void ClearEvents(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            bool doEvent = false;

            lock (this.lockObj)
            {
                doEvent = this.selectedData == data;

                data.sharedDepthFrame = null;

                if (data.depthTexture != null)
                {
                    data.depthTexture.Dispose();
                    data.depthTexture = null;

                    data.imageWidth = 0;
                    data.imageHeight = 0;
                }

                if (data.depthMap != null)
                {
                    data.depthMap.Dispose();
                    data.depthMap = null;
                }

                if (data.uvTable != null)
                {
                    data.uvTable.Dispose();
                    data.uvTable = null;
                }

                data.uvTable = null;
            }

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
                this.RaisePropertyChanged("Selected2DPixelX");
                this.RaisePropertyChanged("Selected2DPixelY");
                this.RaisePropertyChanged("Selected2DPixelDepth");
            }
        }

        private void HandleEvent(EventType eventType, KStudioEvent eventObj, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            if (eventObj != null)
            {
                bool doDataEvent = false;
                bool doVisibleEvent = false;

                if ((eventObj.EventStreamDataTypeId == KStudioEventStreamDataTypeIds.Depth) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.DepthMonitor))
                {
                    lock (this.lockObj)
                    {
                        bool isSelectedData = (this.selectedData == data);

                        HGlobalBuffer newSharedFrame = eventObj.GetRetainableEventDataBuffer();
                        doVisibleEvent = isSelectedData && (newSharedFrame == null) != (data.sharedDepthFrame == null);
                        data.sharedDepthFrame = newSharedFrame;

                        uint newWidth = nui.Constants.STREAM_DEPTH_WIDTH;
                        uint newHeight = nui.Constants.STREAM_DEPTH_HEIGHT;

                        if ((data.depthMap == null) || (newWidth != data.imageWidth) || (newHeight != data.imageHeight))
                        {
                            viz.Context context = null;
                            if (this.pluginService != null)
                            {
                                context = this.pluginService.GetContext(eventType);
                            }

                            data.imageWidth = newWidth;
                            data.imageHeight = newHeight;

                            DepthPlugin.CreateTextures(data, context);
                        }

                        if (data.sharedDepthFrame != null)
                        {
                            unsafe
                            {
                                data.depthMap.UpdateData((ushort*)data.sharedDepthFrame.Buffer, data.sharedDepthFrame.Size);
                            }
                        }

                        if (DepthPlugin.UpdateSelectedPixelValue(data))
                        {
                            doDataEvent = isSelectedData;
                        }
                    }

                    if (doVisibleEvent)
                    {
                        this.RaisePropertyChanged("HasSelected2DPixelData");
                    }

                    if (doDataEvent)
                    {
                        this.RaisePropertyChanged("Selected2DPixelDepth");
                    }
                }
            }
        }

        private viz.Texture GetTexture(IPluginViewSettings pluginViewSettings, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            viz.Texture value = null;

            lock (this.lockObj)
            {
                if (data.sharedDepthFrame != null)
                {
                    DepthPlugin2DViewSettings depth2DSettingsViewSettings = pluginViewSettings as DepthPlugin2DViewSettings;
                    if (depth2DSettingsViewSettings != null)
                    {
                        DepthPlugin.UpdateData(depth2DSettingsViewSettings, data);

                        value = data.depthTexture;
                    }
                }
            }

            return value;
        }

        // object owning data should be locked
        private static void Render3D(IPluginViewSettings pluginViewSettings, viz.Texture texture, EventTypePluginData data, nui.Registration registration)
        {
            Debug.Assert(data != null);

            DepthPlugin3DViewSettings depthPlugin3DViewSettings = pluginViewSettings as DepthPlugin3DViewSettings;
            if ((registration != null) && (depthPlugin3DViewSettings != null))
            {
                DepthPlugin.UpdateData(depthPlugin3DViewSettings, texture, data);

                bool doColor = false;
                bool doBodyIndex = false;

                if (texture != null)
                {
                    uint textureWidth = texture.GetWidth();
                    uint textureHeight = texture.GetHeight();

                    doColor = (textureWidth == nui.Constants.STREAM_COLOR_WIDTH) &&
                                (textureHeight == nui.Constants.STREAM_COLOR_HEIGHT);

                    doBodyIndex = (texture.GetTextureFormat() == viz.TextureFormat.B8G8R8A8_UNORM) &&
                                    (textureWidth == nui.Constants.STREAM_DEPTH_WIDTH) &&
                                    (textureHeight == nui.Constants.STREAM_DEPTH_HEIGHT);
                }

                if ((depthPlugin3DViewSettings.IsSupplyingSurface && doBodyIndex) ||
                    (!depthPlugin3DViewSettings.IsSupplyingSurface && depthPlugin3DViewSettings.ViewType == DepthPlugin3DViewSettings.Depth3DViewType.SurfaceNormal))
                {
                    if (data.depthMap != null)
                    {
                        // special case for body index
                        viz.Effect effect = new viz.Effect()
                            {
                                Direction = new viz.Vector(0.5f, 0.3f, 1.5f, 0),
                                Ambient = new viz.Vector(0.0f, 0.0f, 0.0f, 1.0f),
                                Diffuse = new viz.Vector(0.5f, 0.5f, 0.5f, 1.0f),
                                Specular = new viz.Vector(1.0f, 1.0f, 1.0f, 1.0f),
                                Power = 25.0f,
                                EnableLighting = true,
                                EnableTexture = false,
                            };

                        if ((depthPlugin3DViewSettings.IsSupplyingSurface && doBodyIndex))
                        {
                            if (data.depthMap != null)
                            {
                                data.depthMap.SetMode(viz.DepthVertexMode.SurfaceWithNormal, viz.DepthRampMode.None);
                                effect.EnableTexture = true;
                            }
                        }

                        if (!depthPlugin3DViewSettings.IsSupplyingSurface)
                        {
                            texture = null;
                        }

                        data.depthMap.Render(effect, texture);
                    }
                }
                else
                {
                    if (depthPlugin3DViewSettings.IsSupplyingSurface && doColor)
                    {
                        // special case for color
                        if ((registration != null) && (data.depthMap != null) && (data.uvTable != null) && (data.sharedDepthFrame != null))
                        {
                            data.depthMap.SetMode(viz.DepthVertexMode.SurfaceWithUV, viz.DepthRampMode.None);

                            IntPtr ptr = data.sharedDepthFrame.Buffer;

                            if (ptr != IntPtr.Zero)
                            {
                                registration.Process(ptr, data.uvTable.Buffer);
                            }

                            data.depthMap.UpdateUVTable(data.uvTable.Buffer, data.uvTable.Size);
                        }
                    }

                    if (data.depthMap != null)
                    {
                        viz.Effect effect = new viz.Effect()
                        {
                            EnableTexture = texture != null,
                        };

                        data.depthMap.Render(effect, texture);
                    }
                }
            }
        }

        // object owning data should be locked
        private static bool UpdateSelectedPixelValue(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            bool changed = false;

            if ((data.selected2DPixelX < data.imageWidth) && (data.selected2DPixelY < data.imageHeight))
            {
                uint offset = ((data.imageWidth * data.selected2DPixelY) + data.selected2DPixelX);

                if (data.sharedDepthFrame != null)
                {
                    unsafe
                    {
                        ushort* p = (ushort*)data.sharedDepthFrame.Buffer.ToPointer();
                        if (p != null)
                        {
                            ushort temp = p[offset];
                            if (temp != data.selected2DPixelDepth)
                            {
                                changed = true;
                                data.selected2DPixelDepth = temp;
                            }
                        }
                    }
                }
            }

            return changed;
        }

        // object owning data should be locked 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "texture")]
        private static void UpdateData(DepthPlugin3DViewSettings pluginViewSettings, viz.Texture texture, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            if ((pluginViewSettings != null) && (data.depthMap != null) && (data.depthTexture != null))
            {
                if (pluginViewSettings.IsSupplyingSurface)
                {
                    data.depthMap.SetMode(viz.DepthVertexMode.Surface, viz.DepthRampMode.None);
                }
                else
                {
                    switch (pluginViewSettings.ViewType)
                    {
                        case DepthPlugin3DViewSettings.Depth3DViewType.Color:
                            data.depthMap.SetMode(viz.DepthVertexMode.Point, viz.DepthRampMode.Color);
                            break;

                        case DepthPlugin3DViewSettings.Depth3DViewType.Grey:
                            data.depthMap.SetMode(viz.DepthVertexMode.Point, viz.DepthRampMode.Grey);
                            break;

                        case DepthPlugin3DViewSettings.Depth3DViewType.SurfaceNormal:
                            data.depthMap.SetMode(viz.DepthVertexMode.SurfaceWithNormal, viz.DepthRampMode.None);
                            break;

                        default:
                            Debug.Assert(false);
                            break;
                    }
                }
            }
        }

        // object owning data should be locked 
        private static void UpdateData(DepthPlugin2DViewSettings pluginViewSettings, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            if ((pluginViewSettings != null) && (data.depthMap != null) && (data.depthTexture != null))
            {
                switch (pluginViewSettings.ViewType)
                {
                    case DepthPlugin2DViewSettings.Depth2DViewType.Color:
                        data.depthMap.RampConversion(data.depthTexture, viz.DepthRampMode.Color);
                        break;

                    case DepthPlugin2DViewSettings.Depth2DViewType.Grey:
                        data.depthMap.RampConversion(data.depthTexture, viz.DepthRampMode.Grey);
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }
        }

        // object owning data should be locked
        private static void CreateTextures(EventTypePluginData data, viz.Context context)
        {
            Debug.Assert(data != null);

            if (data.depthTexture != null)
            {
                data.depthTexture.Dispose();
                data.depthTexture = null;
            }

            if (data.depthMap != null)
            {
                data.depthMap.Dispose();
                data.depthMap = null;
            }

            if (data.uvTable != null)
            {
                data.uvTable.Dispose();
                data.uvTable = null;
            }

            data.uvTable = new HGlobalBuffer(data.imageWidth * data.imageHeight * 2 * sizeof(float));

            if (context != null)
            {
                data.depthTexture = new viz.Texture(context, data.imageWidth, data.imageHeight, viz.TextureFormat.R8G8B8A8_UNORM, true); // need to be created as render target for GPU conversion
                data.depthMap = new viz.DepthMap(context, data.imageWidth, data.imageHeight, DepthPlugin.cFovX, DepthPlugin.cFovY, DepthPlugin.cMinZmm, DepthPlugin.cMaxZmm);
            }
        }

        private void UpdatePropertyView(double x, double y, uint width, uint height, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            bool doEvent = false;

            lock (this.lockObj)
            {
                if (this.selectedData != data)
                {
                    doEvent = true;
                    this.selectedData = data;
                }

                data.selected2DPixelX = 0;
                data.selected2DPixelY = 0;
                data.selected2DPixelDepth = 0;

                if (width == data.imageWidth)
                {
                    data.selected2DPixelX = (uint)x;
                }
                else
                {
                    data.selected2DPixelX = (uint)((x / width) * data.imageWidth);
                }

                if (height == data.imageHeight)
                {
                    data.selected2DPixelY = (uint)y;
                }
                else
                {
                    data.selected2DPixelY = (uint)((y / height) * data.imageHeight);
                }

                DepthPlugin.UpdateSelectedPixelValue(data);
            }

            this.RaisePropertyChanged("Selected2DPixelX");
            this.RaisePropertyChanged("Selected2DPixelY");
            this.RaisePropertyChanged("Selected2DPixelDepth");

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
            }
        }

        private class EventTypePluginData
        {
            public HGlobalBuffer sharedDepthFrame = null;
            public HGlobalBuffer uvTable = null;
            public viz.DepthMap depthMap = null;
            public viz.Texture depthTexture = null;
            public nui.Registration lastRegistration = null;
            public uint imageWidth = 0;
            public uint imageHeight = 0;
            public uint selected2DPixelX = 0;
            public uint selected2DPixelY = 0;
            public ushort selected2DPixelDepth = 0;
        }

        private const float cFovX = (float)Math.PI * 70 / 180;
        private const float cFovY = (float)Math.PI * 60 / 180;
        private const float cMinZmm = 500;
        private const float cMaxZmm = 8000;

        private object lockObj = new object();
        private readonly IPluginService pluginService = null;
        private readonly EventTypePluginData monitorData = new EventTypePluginData();
        private readonly EventTypePluginData inspectionData = new EventTypePluginData();
        private EventTypePluginData selectedData = null;
    }
}
