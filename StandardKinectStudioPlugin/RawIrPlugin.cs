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
    using KStudioBridge;
    using KinectStudioUtility;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ir"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ir")]
    public class RawIrPlugin : BasePlugin, IEventHandlerPlugin, I2DVisualPlugin, I3DVisualPlugin, IImageVisualPlugin, IDisposable
    {
        public RawIrPlugin(IServiceProvider serviceProvider)
            : base(Strings.RawIr_Plugin_Title, new Guid(0xedb90d43, 0xb874, 0x40c2, 0x9a, 0xd1, 0x73, 0xe7, 0x67, 0xb1, 0xf7, 0xd2))
        {
            if (serviceProvider != null)
            {
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }
        }

        ~RawIrPlugin()
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
                        value = this.selectedData.sharedRawFrame != null;
                    }
                }

                return value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ir"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ir")]
        public ushort Selected2DPixelIrIntensity
        {
            get
            {
                ushort value = 0;

                lock (this.lockObj)
                {
                    if (this.selectedData != null)
                    {
                        value = this.selectedData.selected2DPixelIntensity;
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
                    break;

                case EventType.Inspection:
                    Debug.Assert(this.inspectionData != null);

                    if (this.inspectionData.sharedRawFrame != null)
                    {
                        EventTypePluginData data = this.inspectionData; 

                        RawIrPlugin.CreateTextures(this.inspectionData, context);
                        DepthIrEngine depthIrEngine = this.pluginService.DepthIrEngine;

                        if ((depthIrEngine != null) && (data.depthFrame != null) && (data.irFrame != null))
                        {
                            unsafe
                            {
                                fixed (ushort* pDepthFrame = &data.depthFrame[0], pIrFrame = &data.irFrame[0])
                                {
                                    depthIrEngine.HandleEvent(data.sharedRawFrame.Buffer, data.sharedRawFrame.Size, pDepthFrame, pIrFrame, data.imageWidth, data.imageHeight);

                                    uint imageDataSize = data.imageWidth * data.imageHeight * sizeof(ushort);
                                    if (data.rawIrTexture != null)
                                    {
                                        data.rawIrTexture.UpdateData((byte*)pIrFrame, imageDataSize);
                                    }

                                    if (data.depthMap != null)
                                    {
                                        data.depthMap.UpdateData(pDepthFrame, imageDataSize);
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public void UninitializeRender(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Monitor:
                    Debug.Assert(this.monitorData != null);
                    RawIrPlugin.DisposeData(this.monitorData, false);
                    break;

                case EventType.Inspection:
                    Debug.Assert(this.inspectionData != null);
                    RawIrPlugin.DisposeData(this.inspectionData, false);
                    break;
            }
        }

        public bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId)
        {
            return (dataTypeId == KStudioEventStreamDataTypeIds.RawIr) ||
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
            DataTemplate dataTemplate = Resources.Get("RawIrPluginPropertyViewDataTemplate") as DataTemplate;
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
                this.RaisePropertyChanged("Selected2DPixelIrIntensity");
                this.RaisePropertyChanged("Selected2DPixelDepth");
            }
        }

        public void RemoveView(Guid viewId, Panel hostControl, IPluginViewSettings pluginViewSettings)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Windows.Controls.TextBlock.set_Text(System.String)")]
        public IPluginViewSettings Add2DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = null;

            switch (eventType)
            {
                case EventType.Monitor:
                    // no raw IR monitoring
                    break;

                case EventType.Inspection:
                    pluginViewSettings = new RawIrPlugin2DViewSettings(this.pluginService, eventType);
                    break;
            }

            return pluginViewSettings;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Windows.Controls.TextBlock.set_Text(System.String)")]
        public IPluginViewSettings Add3DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = null;

            switch (eventType)
            {
                case EventType.Monitor:
                    // no raw monitoring
                    // pluginViewSettings = new RawIrPlugin3DViewSettings();
                    break;

                case EventType.Inspection:
                    pluginViewSettings = new RawIrPlugin3DViewSettings(this.pluginService, eventType);
                    break;
            }

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
                nui.Registration registration = null;
                if (this.pluginService != null)
                {
                    registration = pluginService.GetRegistration(eventType);
                }
                switch (eventType)
                {
                    case EventType.Monitor:
                        RawIrPlugin.Render3D(pluginViewSettings, texture, this.monitorData, registration);
                        break;

                    case EventType.Inspection:
                        RawIrPlugin.Render3D(pluginViewSettings, texture, this.inspectionData, registration);
                        break;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.lockObj)
                {
                    RawIrPlugin.DisposeData(this.monitorData, true);
                    RawIrPlugin.DisposeData(this.inspectionData, true);
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
                data.sharedRawFrame = null;
            }

            if (data.rawIrTexture != null)
            {
                data.rawIrTexture.Dispose();
                data.rawIrTexture = null;
            }

            if (data.irTexture != null)
            {
                data.irTexture.Dispose();
                data.irTexture = null;
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
                data.sharedRawFrame = null;

                data.imageWidth = 0;
                data.imageHeight = 0;

                data.depthFrame = null;
                data.irFrame = null;

                if (data.rawIrTexture != null)
                {
                    data.rawIrTexture.Dispose();
                    data.rawIrTexture = null;
                }

                if (data.irTexture != null)
                {
                    data.irTexture.Dispose();
                    data.irTexture = null;
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

                data.uvTable = null;
            }

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
                this.RaisePropertyChanged("Selected2DPixelX");
                this.RaisePropertyChanged("Selected2DPixelY");
                this.RaisePropertyChanged("Selected2DPixelIrIntensity");
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

                if (eventObj.EventStreamDataTypeId == KStudioEventStreamDataTypeIds.RawIr)
                {
                    lock (this.lockObj)
                    {
                        bool isSelectedData = (this.selectedData == data);

                        HGlobalBuffer newSharedFrame = eventObj.GetRetainableEventDataBuffer();
                        doVisibleEvent = isSelectedData && (newSharedFrame == null) != (data.sharedRawFrame == null);
                        data.sharedRawFrame = newSharedFrame;

                        uint newWidth = nui.Constants.STREAM_IR_WIDTH;
                        uint newHeight = nui.Constants.STREAM_IR_HEIGHT;

                        if ((data.rawIrTexture == null) || (newWidth != data.imageWidth) || (newHeight != data.imageHeight))
                        {
                            data.imageWidth = newWidth;
                            data.imageHeight = newHeight;

                            viz.Context context = null;

                            if (this.pluginService != null)
                            {
                                context = this.pluginService.GetContext(eventType);
                            }

                            RawIrPlugin.CreateTextures(data, context);
                        }

                        DepthIrEngine depthIrEngine = this.pluginService.DepthIrEngine;

                        if ((depthIrEngine != null) && (data.depthFrame != null) && (data.irFrame != null))
                        {
                            unsafe
                            {
                                fixed (ushort* pDepthFrame = &data.depthFrame[0], pIrFrame = &data.irFrame[0])
                                {
                                    depthIrEngine.HandleEvent(data.sharedRawFrame.Buffer, data.sharedRawFrame.Size, pDepthFrame, pIrFrame, data.imageWidth, data.imageHeight);

                                    uint imageDataSize = data.imageWidth * data.imageHeight * sizeof(ushort);
                                    if (data.rawIrTexture != null)
                                    {
                                        data.rawIrTexture.UpdateData((byte*)pIrFrame, imageDataSize);
                                    }

                                    if (data.depthMap != null)
                                    {
                                        data.depthMap.UpdateData(pDepthFrame, imageDataSize);
                                    }
                                }
                            }
                        }

                        if (RawIrPlugin.UpdateSelectedPixelValue(data))
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
                        this.RaisePropertyChanged("Selected2DPixelIrIntensity");
                        this.RaisePropertyChanged("Selected2DPixelDepth");
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

                if (data.irFrame != null)
                {
                    ushort temp = data.irFrame[offset];
                    if (temp != data.selected2DPixelIntensity)
                    {
                        changed = true;
                        data.selected2DPixelIntensity = temp;
                    }
                }

                if (data.depthFrame != null)
                {
                    ushort temp = data.depthFrame[offset];
                    if (temp != data.selected2DPixelDepth)
                    {
                        changed = true;
                        data.selected2DPixelDepth = temp;
                    }
                }
            }

            return changed;
        }

        // object owning should be locked 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "texture")]
        private static void UpdateData(RawIrPlugin3DViewSettings pluginViewSettings, viz.Texture texture, EventTypePluginData data)
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
                        case RawIrPlugin3DViewSettings.RawIr3DViewType.DepthColor:
                            data.depthMap.SetMode(viz.DepthVertexMode.Point, viz.DepthRampMode.Color);
                            break;

                        case RawIrPlugin3DViewSettings.RawIr3DViewType.DepthGrey:
                            data.depthMap.SetMode(viz.DepthVertexMode.Point, viz.DepthRampMode.Grey);
                            break;

                        case RawIrPlugin3DViewSettings.RawIr3DViewType.SurfaceNormal:
                            data.depthMap.SetMode(viz.DepthVertexMode.SurfaceWithNormal, viz.DepthRampMode.None);
                            break;

                        case RawIrPlugin3DViewSettings.RawIr3DViewType.Ir:
                            data.depthMap.SetMode(viz.DepthVertexMode.Surface, viz.DepthRampMode.None);
                            data.irTexture.RampConversion(data.rawIrTexture, pluginViewSettings.RampTexture);
                            break;

                        default:
                            Debug.Assert(false);
                            break;
                    }
                }
            }
        }

        // object owning data should be locked 
        private static void UpdateData(RawIrPlugin2DViewSettings pluginViewSettings, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            if ((pluginViewSettings != null) && (data.depthMap != null) && (data.depthTexture != null) && (data.irTexture != null))
            {
                switch (pluginViewSettings.ViewType)
                {
                    case RawIrPlugin2DViewSettings.RawIr2DViewType.DepthColor:
                        data.depthMap.RampConversion(data.depthTexture, viz.DepthRampMode.Color);
                        break;

                    case RawIrPlugin2DViewSettings.RawIr2DViewType.DepthGrey:
                        data.depthMap.RampConversion(data.depthTexture, viz.DepthRampMode.Grey);
                        break;

                    case RawIrPlugin2DViewSettings.RawIr2DViewType.Ir:
                        data.irTexture.RampConversion(data.rawIrTexture, pluginViewSettings.RampTexture);
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

            if (data.rawIrTexture != null)
            {
                data.rawIrTexture.Dispose();
                data.rawIrTexture = null;
            }

            if (data.irTexture != null)
            {
                data.irTexture.Dispose();
                data.irTexture = null;
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

            data.uvTable = new HGlobalBuffer(data.imageWidth * data.imageHeight * 2 * sizeof(float));

            if (context != null)
            {
                data.rawIrTexture = new viz.Texture(context, data.imageWidth, data.imageHeight, viz.TextureFormat.R16_UNORM, false);
                data.irTexture = new viz.Texture(context, data.imageWidth, data.imageHeight, viz.TextureFormat.R8G8B8A8_UNORM, true);

                data.depthTexture = new viz.Texture(context, data.imageWidth, data.imageHeight, viz.TextureFormat.R8G8B8A8_UNORM, true); // need to be created as render target for GPU conversion
                data.depthMap = new viz.DepthMap(context, data.imageWidth, data.imageHeight, RawIrPlugin.cFovX, RawIrPlugin.cFovY, RawIrPlugin.cMinZmm, RawIrPlugin.cMaxZmm);
            }

            data.depthFrame = new ushort[data.imageWidth * data.imageHeight];
            data.irFrame = new ushort[data.imageWidth * data.imageHeight];
        }

        private viz.Texture GetTexture(IPluginViewSettings pluginViewSettings, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            viz.Texture value = null;

            lock (this.lockObj)
            {
                RawIrPlugin2DViewSettings raw2DSettingsViewSettings = pluginViewSettings as RawIrPlugin2DViewSettings;
                if (raw2DSettingsViewSettings != null)
                {
                    RawIrPlugin.UpdateData(raw2DSettingsViewSettings, data);

                    if (raw2DSettingsViewSettings.ViewType == RawIrPlugin2DViewSettings.RawIr2DViewType.Ir)
                    {
                        value = data.irTexture;
                    }
                    else
                    {
                        value = data.depthTexture;
                    }
                }
                else
                {
                    RawIrPlugin3DViewSettings raw3DSettingsViewSettings = pluginViewSettings as RawIrPlugin3DViewSettings;
                    if (raw3DSettingsViewSettings != null)
                    {
                        if (raw3DSettingsViewSettings.ViewType == RawIrPlugin3DViewSettings.RawIr3DViewType.Ir)
                        {
                            value = data.irTexture;
                        }
                    }
                }
            }

            return value;
        }

        // object owning data should be locked
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "context")]
        private static void Render3D(IPluginViewSettings pluginViewSettings, viz.Texture texture, EventTypePluginData data, nui.Registration registration)
        {
            Debug.Assert(data != null);

            RawIrPlugin3DViewSettings rawPlugin3DViewSettings = pluginViewSettings as RawIrPlugin3DViewSettings;
            if (rawPlugin3DViewSettings != null)
            {
                RawIrPlugin.UpdateData(rawPlugin3DViewSettings, texture, data);

                bool doColor = false;
                bool doBodyIndex = false;

                if (texture != null)
                {
                    uint textureWidth = texture.GetWidth();
                    uint textureHeight = texture.GetHeight();

                    doColor = (textureWidth == nui.Constants.STREAM_COLOR_WIDTH) &&
                                (textureHeight == nui.Constants.STREAM_COLOR_HEIGHT);

                    doBodyIndex = (texture.GetTextureFormat() == viz.TextureFormat.B8G8R8A8_UNORM) &&
                                    (textureWidth == nui.Constants.STREAM_IR_WIDTH) &&
                                    (textureHeight == nui.Constants.STREAM_IR_HEIGHT);
                }

                if ((!rawPlugin3DViewSettings.IsSupplyingSurface && (rawPlugin3DViewSettings.ViewType == RawIrPlugin3DViewSettings.RawIr3DViewType.Ir)) ||
                    (rawPlugin3DViewSettings.IsSupplyingSurface && doBodyIndex) ||
                    (!rawPlugin3DViewSettings.IsSupplyingSurface && rawPlugin3DViewSettings.ViewType == RawIrPlugin3DViewSettings.RawIr3DViewType.SurfaceNormal))
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

                        if ((rawPlugin3DViewSettings.IsSupplyingSurface && doBodyIndex))
                        {
                            if (data.depthMap != null)
                            {
                                data.depthMap.SetMode(viz.DepthVertexMode.SurfaceWithNormal, viz.DepthRampMode.None);
                                effect.EnableTexture = true;
                            }
                        }

                        if (!rawPlugin3DViewSettings.IsSupplyingSurface && (rawPlugin3DViewSettings.ViewType != RawIrPlugin3DViewSettings.RawIr3DViewType.Ir))
                        {
                            texture = null;
                        }

                        data.depthMap.Render(effect, texture);
                    }
                }
                else
                {
                    if (rawPlugin3DViewSettings.IsSupplyingSurface && doColor)
                    {
                        // special case for color
                        if ((registration != null) && (data.depthMap != null) && (data.uvTable != null) && (data.sharedRawFrame != null))
                        {
                            data.depthMap.SetMode(viz.DepthVertexMode.SurfaceWithUV, viz.DepthRampMode.None);

                            IntPtr ptr = data.sharedRawFrame.Buffer;

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
                data.selected2DPixelIntensity = 0;
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

                RawIrPlugin.UpdateSelectedPixelValue(data);
            }

            this.RaisePropertyChanged("Selected2DPixelX");
            this.RaisePropertyChanged("Selected2DPixelY");
            this.RaisePropertyChanged("Selected2DPixelIrIntensity");
            this.RaisePropertyChanged("Selected2DPixelDepth");

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
            }
        }

        private class EventTypePluginData
        {
            public HGlobalBuffer sharedRawFrame = null;
            public HGlobalBuffer uvTable = null;
            public viz.Texture irTexture = null;
            public viz.Texture rawIrTexture = null;
            public viz.DepthMap depthMap = null;
            public viz.Texture depthTexture = null;
            public ushort[] depthFrame = null;
            public ushort[] irFrame = null;
            public uint imageWidth = 0;
            public uint imageHeight = 0;
            public uint selected2DPixelX = 0;
            public uint selected2DPixelY = 0;
            public ushort selected2DPixelIntensity = 0;
            public ushort selected2DPixelDepth = 0;
            public nui.Registration lastRegistration = null;
        }

        private const float cFovX = (float)Math.PI * 70 / 180;
        private const float cFovY = (float)Math.PI * 60 / 180;
        private const float cMinZmm = 500;
        private const float cMaxZmm = 8000;

        private object lockObj = new object();
        private IPluginService pluginService = null;
        private readonly EventTypePluginData monitorData = new EventTypePluginData();
        private readonly EventTypePluginData inspectionData = new EventTypePluginData();
        private EventTypePluginData selectedData = null;
    }
}
