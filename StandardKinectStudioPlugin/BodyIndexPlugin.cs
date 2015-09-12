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
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Kinect.Tools;
    using nui = Microsoft.Xbox.Input.Nui;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    public class BodyIndexPlugin : BasePlugin, IEventHandlerPlugin, I2DVisualPlugin, I3DVisualPlugin, IImageVisualPlugin, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "serviceProvider")]
        public BodyIndexPlugin(IServiceProvider serviceProvider)
            : base(Strings.BodyIndex_Plugin_Title, new Guid(0x7887714d, 0x5331, 0x406a, 0xa2, 0x90, 0x9d, 0x29, 0x81, 0x21, 0x46, 0x3))
        {
            if (serviceProvider != null)
            {
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }
        }

        ~BodyIndexPlugin()
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
                        value = this.selectedData.sharedBodyIndexFrame != null;
                    }
                }

                return value;
            }
        }

        public ushort Selected2DPixelBodyIndex
        {
            get
            {
                ushort value = 0xff;

                lock (this.lockObj)
                {
                    if (this.selectedData != null)
                    {
                        value = this.selectedData.selected2DPixelBodyIndex;
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
                lock (this.lockObj)
                {
                    uint value = 0;

                    if (this.selectedData != null)
                    {
                        value = this.selectedData.selected2DPixelY;
                    }

                    return value;
                }
            }
        }

        public void InitializeRender(EventType eventType, viz.Context context)
        {
            switch (eventType)
            {
                case EventType.Monitor:
                    this.InitializeRender(context, this.monitorData);
                    break;

                case EventType.Inspection:
                    this.InitializeRender(context, this.inspectionData);
                    break;
            }
        }

        public void UninitializeRender(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Monitor:
                    BodyIndexPlugin.DisposeData(this.monitorData, false);
                    break;

                case EventType.Inspection:
                    BodyIndexPlugin.DisposeData(this.inspectionData, false);
                    break;
            }
        }

        public bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId)
        {
            return (dataTypeId == KStudioEventStreamDataTypeIds.BodyIndex) || (dataTypeId == HackKStudioEventStreamDataTypeIds.BodyIndexMonitor);
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
                    this.HandleEvent(eventObj, this.monitorData, eventType);
                    break;

                case EventType.Inspection:
                    this.HandleEvent(eventObj, this.inspectionData, eventType);
                    break;
            }
        }

        public IPluginViewSettings Add2DPropertyView(ContentControl hostControl)
        {
            DataTemplate dataTemplate = Resources.Get("BodyIndexPluginPropertyViewDataTemplate") as DataTemplate;
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
                this.RaisePropertyChanged("Selected2DPixelBodyIndex");
            }
        }

        public void RemoveView(Guid viewId, Panel hostControl, IPluginViewSettings pluginViewSettings)
        {
        }

        public IPluginViewSettings Add2DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new BodyIndexPlugin2DViewSettings();

            return pluginViewSettings;
        }

        public IPluginViewSettings Add3DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new BodyIndexPlugin3DViewSettings();

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
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.lockObj)
                {
                    BodyIndexPlugin.DisposeData(this.monitorData, true);
                    BodyIndexPlugin.DisposeData(this.inspectionData, true);
                }
            }
        }

        private void InitializeRender(viz.Context context, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            lock (this.lockObj)
            {
                Debug.Assert(data.rampTexture3d == null);
                Debug.Assert(data.rampTexture2d == null);

                if (context != null)
                {
                    BodyIndexPlugin.CreateTextures(data, context);

                    // 2D ramp texture is different from 3D ramp texture for visualization with skeleton
                    uint[] rampData = new uint[BodyIndexPlugin.cRampTextureLength];
                    rampData[0] = 0xFFFF0000;
                    rampData[1] = 0xFF00FF00;
                    rampData[2] = 0xFF40FFFF;
                    rampData[3] = 0xFFFFFF40;
                    rampData[4] = 0xFFFF40FF;
                    rampData[5] = 0xFF8080FF;
                    for (int i = 6; i < rampData.Length; ++i)
                    {
                        rampData[i] = 0xFFFFFFFF;
                    }

                    data.rampTexture3d = new viz.Texture(context, (uint)rampData.Length, 1, viz.TextureFormat.B8G8R8A8_UNORM, false);

                    unsafe
                    {
                        fixed (uint* pRampData = rampData)
                        {
                            data.rampTexture3d.UpdateData((byte*)pRampData, (uint)rampData.Length * sizeof(uint));
                        }
                    }

                    rampData[0] = 0xC0FF0000;
                    rampData[1] = 0xC000FF00;
                    rampData[2] = 0xC040FFFF;
                    rampData[3] = 0xC0FFFF40;
                    rampData[4] = 0xC0FF40FF;
                    rampData[5] = 0xC08080FF;
                    for (int i = 6; i < rampData.Length; i++)
                    {
                        rampData[i] = 0xFF000000;
                    }

                    data.rampTexture2d = new viz.Texture(context, (uint)rampData.Length, 1, viz.TextureFormat.B8G8R8A8_UNORM, false);

                    unsafe
                    {
                        fixed (uint* pRampData = rampData)
                        {
                            data.rampTexture2d.UpdateData((byte*)pRampData, (uint)rampData.Length * sizeof(uint));
                        }
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
                BodyIndexPlugin.UpdateData(data);

                if (pluginViewSettings is BodyIndexPlugin2DViewSettings)
                {
                    value = data.bodyIndexTexture2d;
                }
                else if (pluginViewSettings is BodyIndexPlugin3DViewSettings)
                {
                    value = data.bodyIndexTexture3d;
                }
            }

            return value;
        }

        private void ClearEvents(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            bool doEvent = false;

            lock (this.lockObj)
            {
                doEvent = this.selectedData == data;

                data.sharedBodyIndexFrame = null;

                if (data.bodyIndexTexture2d != null)
                {
                    data.bodyIndexTexture2d.Dispose();
                    data.bodyIndexTexture2d = null;
                }

                if (data.bodyIndexTexture3d != null)
                {
                    data.bodyIndexTexture3d.Dispose();
                    data.bodyIndexTexture3d = null;
                }

                data.imageWidth = 0;
                data.imageHeight = 0;

                data.selected2DPixelX = 0;
                data.selected2DPixelY = 0;
                data.selected2DPixelBodyIndex = 0xff;
            }

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
                this.RaisePropertyChanged("Selected2DPixelX");
                this.RaisePropertyChanged("Selected2DPixelY");
                this.RaisePropertyChanged("Selected2DPixeBodyIndex");
            }
        }

        private void HandleEvent(KStudioEvent eventObj, EventTypePluginData data, EventType eventType)
        {
            if ((eventObj != null) && (data != null))
            {
                if ((eventObj.EventStreamDataTypeId == KStudioEventStreamDataTypeIds.BodyIndex) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.BodyIndexMonitor))
                {
                    bool doDataEvent = false;
                    bool doVisibleEvent = false;

                    lock (this.lockObj)
                    {
                        bool isSelectedData = (this.selectedData == data);

                        HGlobalBuffer newSharedFrame = eventObj.GetRetainableEventDataBuffer();
                        doVisibleEvent =  isSelectedData && (newSharedFrame == null) != (data.sharedBodyIndexFrame == null);
                        data.sharedBodyIndexFrame = newSharedFrame;

                        uint newWidth = nui.Constants.STREAM_BODY_INDEX_WIDTH;
                        uint newHeight = nui.Constants.STREAM_BODY_INDEX_HEIGHT;

                        if ((data.rawBodyIndexTexture == null) || (newWidth != data.imageWidth) || (newHeight != data.imageHeight))
                        {
                            data.imageWidth = newWidth;
                            data.imageHeight = newHeight;

                            viz.Context context = null;
                            if (this.pluginService != null)
                            {
                                context = this.pluginService.GetContext(eventType);
                            }

                            if (context != null)
                            {
                                BodyIndexPlugin.CreateTextures(data, context);
                            }
                        }

                        if (BodyIndexPlugin.UpdateSelectedPixelValue(data))
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
                        this.RaisePropertyChanged("Selected2DPixelBodyIndex");
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
                data.selected2DPixelBodyIndex = 0;

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

                BodyIndexPlugin.UpdateSelectedPixelValue(data);
            }

            this.RaisePropertyChanged("Selected2DPixelX");
            this.RaisePropertyChanged("Selected2DPixelY");
            this.RaisePropertyChanged("Selected2DPixelBodyIndex");

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
            }
        }

        // this.IsPlaybackFileOnTarge data should be locked
        private static bool UpdateSelectedPixelValue(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            bool changed = false;

            if ((data.selected2DPixelX < data.imageWidth) && (data.selected2DPixelY < data.imageHeight))
            {
                uint offset = ((data.imageWidth * data.selected2DPixelY) + data.selected2DPixelX);

                if (data.sharedBodyIndexFrame != null)
                {
                    unsafe
                    {
                        byte* p = (byte*)data.sharedBodyIndexFrame.Buffer.ToPointer();
                        if (p != null)
                        {
                            byte temp = p[offset];
                            if (temp != data.selected2DPixelBodyIndex)
                            {
                                changed = true;
                                data.selected2DPixelBodyIndex = temp;
                            }
                        }
                    }
                }
            }

            return changed;
        }

        // object owning data should be locked 
        private static void UpdateData(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            if ((data.sharedBodyIndexFrame != null))
            {
                if (data.rawBodyIndexTexture != null)
                {
                    unsafe
                    {
                        data.rawBodyIndexTexture.UpdateData((byte*)data.sharedBodyIndexFrame.Buffer, data.sharedBodyIndexFrame.Size);
                    }
                }
            }

            if (data.rawBodyIndexTexture != null)
            {
                if (data.bodyIndexTexture3d != null)
                {
                    data.bodyIndexTexture3d.RampConversion(data.rawBodyIndexTexture, data.rampTexture3d);
                }

                if (data.bodyIndexTexture2d != null)
                {
                    data.bodyIndexTexture2d.RampConversion(data.rawBodyIndexTexture, data.rampTexture2d);
                }
            }
        }

        // instance of data should be locked
        private static void DisposeData(EventTypePluginData data, bool doAll)
        {
            Debug.Assert(data != null);

            if (doAll)
            {
                // don't dispose this because others may be sharing the data
                data.sharedBodyIndexFrame = null;
            }

            if (data.rawBodyIndexTexture != null)
            {
                data.rawBodyIndexTexture.Dispose();
                data.rawBodyIndexTexture = null;
            }

            if (data.bodyIndexTexture3d != null)
            {
                data.bodyIndexTexture3d.Dispose();
                data.bodyIndexTexture3d = null;
            }

            if (data.bodyIndexTexture2d != null)
            {
                data.bodyIndexTexture2d.Dispose();
                data.bodyIndexTexture2d = null;
            }

            if (data.rampTexture3d != null)
            {
                data.rampTexture3d.Dispose();
                data.rampTexture3d = null;
            }

            if (data.rampTexture2d != null)
            {
                data.rampTexture2d.Dispose();
                data.rampTexture2d = null;
            }
        }

        // object owning data should be locked
        private static void CreateTextures(EventTypePluginData data, viz.Context context)
        {
            Debug.Assert(data != null);

            if (data.rawBodyIndexTexture != null)
            {
                data.rawBodyIndexTexture.Dispose();
                data.rawBodyIndexTexture = null;
            }

            if (data.bodyIndexTexture3d != null)
            {
                data.bodyIndexTexture3d.Dispose();
                data.bodyIndexTexture3d = null;
            }

            if (data.bodyIndexTexture2d != null)
            {
                data.bodyIndexTexture2d.Dispose();
                data.bodyIndexTexture2d = null;
            }

            if ((context != null) && (data.imageWidth > 0) && (data.imageHeight > 0))
            {
                data.bodyIndexTexture3d = new viz.Texture(context, data.imageWidth, data.imageHeight, viz.TextureFormat.B8G8R8A8_UNORM, true);
                data.bodyIndexTexture2d = new viz.Texture(context, data.imageWidth, data.imageHeight, viz.TextureFormat.B8G8R8A8_UNORM, true);
                data.rawBodyIndexTexture = new viz.Texture(context, data.imageWidth, data.imageHeight, viz.TextureFormat.R8_UNORM, false);
            }
        }

        private class EventTypePluginData
        {
            public HGlobalBuffer sharedBodyIndexFrame = null;
            public viz.Texture rampTexture3d = null;
            public viz.Texture rampTexture2d = null;
            public viz.Texture bodyIndexTexture3d = null;
            public viz.Texture bodyIndexTexture2d = null;
            public viz.Texture rawBodyIndexTexture = null;
            public uint imageWidth = 0;
            public uint imageHeight = 0;
            public uint selected2DPixelX = 0;
            public uint selected2DPixelY = 0;
            public byte selected2DPixelBodyIndex = 0xff;
        };

        private const uint cRampTextureLength = 256;

        private object lockObj = new object();
        private readonly IPluginService pluginService = null;
        private readonly EventTypePluginData monitorData = new EventTypePluginData();
        private readonly EventTypePluginData inspectionData = new EventTypePluginData();
        private EventTypePluginData selectedData = null;
    }
}

