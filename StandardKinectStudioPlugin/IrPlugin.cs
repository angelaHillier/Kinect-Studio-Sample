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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ir"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ir")]
    public class IrPlugin : BasePlugin, IEventHandlerPlugin, I2DVisualPlugin, I3DVisualPlugin, IImageVisualPlugin, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "serviceProvider")]
        public IrPlugin(IServiceProvider serviceProvider)
            : base(Strings.Ir_Plugin_Title, new Guid(0x5ef876c2, 0xcec2, 0x4b2d, 0x88, 0xa8, 0x1d, 0xe8, 0xe6, 0xdf, 0x4e, 0x63))
        {
            if (serviceProvider != null)
            {
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }

            this.selectedData = null;
        }

        ~IrPlugin()
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
                        value = this.selectedData.sharedIrFrame != null;
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
                        value = this.selectedData.selected2DPixelIrIntensity;
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
                    IrPlugin.CreateTextures(this.monitorData, context);
                    break;

                case EventType.Inspection:
                    Debug.Assert(this.inspectionData != null);
                    IrPlugin.CreateTextures(this.inspectionData, context);
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
                        IrPlugin.DisposeData(this.monitorData, false);
                        break;

                    case EventType.Inspection:
                        Debug.Assert(this.inspectionData != null);
                        IrPlugin.DisposeData(this.inspectionData, false);
                        break;
                }
            }
        }

        public bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId)
        {
            return (dataTypeId == KStudioEventStreamDataTypeIds.Ir) || (dataTypeId == HackKStudioEventStreamDataTypeIds.IrMonitor);
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
            DataTemplate dataTemplate = Resources.Get("IrPluginPropertyViewDataTemplate") as DataTemplate;
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
            }
        }

        public void RemoveView(Guid viewId, Panel hostControl, IPluginViewSettings pluginViewSettings)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public IPluginViewSettings Add2DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = null;

            switch (eventType)
            {
                case EventType.Monitor:
                    pluginViewSettings = new IrPlugin2DViewSettings(this.pluginService, eventType);
                    break;

                case EventType.Inspection:
                    pluginViewSettings = new IrPlugin2DViewSettings(this.pluginService, eventType);
                    break;
            }

            return pluginViewSettings;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public IPluginViewSettings Add3DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = null;

            switch (eventType)
            {
                case EventType.Monitor:
                case EventType.Inspection:
                    pluginViewSettings = new IrPlugin3DViewSettings(this.pluginService, eventType);
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
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.lockObj)
                {
                    IrPlugin.DisposeData(this.monitorData, true);
                    IrPlugin.DisposeData(this.inspectionData, true);
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
                data.sharedIrFrame = null;
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
        }

        private void ClearEvents(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            bool doEvent = false;

            lock (this.lockObj)
            {
                doEvent = this.selectedData == data;

                data.sharedIrFrame = null;

                if (data.irTexture != null)
                {
                    data.irTexture.Dispose();
                    data.irTexture = null;

                    data.imageWidth = 0;
                    data.imageHeight = 0;
                }

                data.selected2DPixelX = 0;
                data.selected2DPixelY = 0;
                data.selected2DPixelIrIntensity = 0;
            }

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
                this.RaisePropertyChanged("Selected2DPixelX");
                this.RaisePropertyChanged("Selected2DPixelY");
                this.RaisePropertyChanged("Selected2DPixelIrIntensity");
            }
        }

        private void HandleEvent(KStudioEvent eventObj, EventTypePluginData data, EventType eventType)
        {
            Debug.Assert(data != null);

            if (eventObj != null)
            {
                if ((eventObj.EventStreamDataTypeId == KStudioEventStreamDataTypeIds.Ir) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.IrMonitor))
                {
                    bool doDataEvent = false;
                    bool doVisibleEvent = false;

                    lock (this.lockObj)
                    {
                        bool isSelectedData = (this.selectedData == data);

                        HGlobalBuffer newSharedFrame = eventObj.GetRetainableEventDataBuffer();
                        doVisibleEvent = isSelectedData && (newSharedFrame == null) != (data.sharedIrFrame == null);
                        data.sharedIrFrame = newSharedFrame;

                        uint newWidth = nui.Constants.STREAM_IR_WIDTH;
                        uint newHeight = nui.Constants.STREAM_IR_HEIGHT;

                        if ((newWidth != data.imageWidth) || (newHeight != data.imageHeight))
                        {
                            viz.Context context = null;
                            if (this.pluginService != null)
                            {
                                context = this.pluginService.GetContext(eventType);
                            }

                            data.imageWidth = newWidth;
                            data.imageHeight = newHeight;

                            IrPlugin.CreateTextures(data, context);
                        }

                        if (IrPlugin.UpdateSelectedPixelValue(data))
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
                IrPluginViewSettings irPluginViewSettings = pluginViewSettings as IrPluginViewSettings;
                if (irPluginViewSettings != null)
                {
                    IrPlugin.UpdateData(irPluginViewSettings.RampTexture, data);

                    value = data.irTexture;
                }
            }

            return value;
        }

        // object owning data should be locked
        private static bool UpdateSelectedPixelValue(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            bool changed = false;

            if ((data.selected2DPixelX < data.imageWidth) && (data.selected2DPixelY < data.imageHeight))
            {
                uint offset = ((data.imageWidth * data.selected2DPixelY) + data.selected2DPixelX);

                if (data.sharedIrFrame != null)
                {
                    unsafe
                    {
                        ushort* p = (ushort*)data.sharedIrFrame.Buffer.ToPointer();
                        if (p != null)
                        {
                            ushort temp = p[offset];
                            if (temp != data.selected2DPixelIrIntensity)
                            {
                                changed = true;
                                data.selected2DPixelIrIntensity = temp;
                            }
                        }
                    }
                }
            }

            return changed;
        }

        // object owning data should be locked 
        private static void UpdateData(viz.Texture rampTexture, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            if (rampTexture != null)
            {
                if ((data.sharedIrFrame != null) && (data.rawIrTexture != null))
                {
                    unsafe
                    {
                        data.rawIrTexture.UpdateData((byte*)data.sharedIrFrame.Buffer, data.sharedIrFrame.Size);
                    }
                }

                if (data.irTexture != null)
                {
                    data.irTexture.RampConversion(data.rawIrTexture, rampTexture);
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

            if ((context != null) && (data.imageWidth > 0) && (data.imageHeight > 0))
            {
                data.rawIrTexture = new viz.Texture(context, data.imageWidth, data.imageHeight, viz.TextureFormat.R16_UNORM, false);
                data.irTexture = new viz.Texture(context, data.imageWidth, data.imageHeight, viz.TextureFormat.R8G8B8A8_UNORM, true);
            }
        }

        private void UpdatePropertyView(double x, double y, uint width, uint height, EventTypePluginData data)
        {
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
                data.selected2DPixelIrIntensity = 0;

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

                IrPlugin.UpdateSelectedPixelValue(data);
            }

            this.RaisePropertyChanged("Selected2DPixelX");
            this.RaisePropertyChanged("Selected2DPixelY");
            this.RaisePropertyChanged("Selected2DPixelIrIntensity");

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
            }
        }

        private class EventTypePluginData
        {
            public HGlobalBuffer sharedIrFrame = null;
            public viz.Texture irTexture = null;
            public viz.Texture rawIrTexture = null;
            public uint imageWidth = 0;
            public uint imageHeight = 0;
            public uint selected2DPixelX = 0;
            public uint selected2DPixelY = 0;
            public ushort selected2DPixelIrIntensity = 0;
        }

        private object lockObj = new object();
        private readonly IPluginService pluginService = null;
        private readonly EventTypePluginData monitorData = new EventTypePluginData();
        private readonly EventTypePluginData inspectionData = new EventTypePluginData();
        private EventTypePluginData selectedData = null;
    }
}
