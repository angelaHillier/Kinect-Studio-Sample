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
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;
    using nui = Microsoft.Xbox.Input.Nui;
    using viz = Microsoft.Xbox.Kinect.Viz;
using System.Windows.Threading;

    public class ColorPlugin : BasePlugin, IEventHandlerPlugin, I2DVisualPlugin, I3DVisualPlugin, IImageVisualPlugin, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "serviceProvider")]
        public ColorPlugin(IServiceProvider serviceProvider)
            : base(Strings.Color_Plugin_Title, new Guid(0x640d29b1, 0x663a, 0x4913, 0x9e, 0xba, 0xbe, 0xc2, 0xb8, 0xaa, 0x81, 0xea))
        {
            if (serviceProvider != null)
            {
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }

            this.monitorData.timer.Interval = TimeSpan.FromMilliseconds(500);
            this.inspectionData.timer.Interval = TimeSpan.FromMilliseconds(500);

            this.monitorData.timer.Tick += (source, e) => this.HandleTimer(this.monitorData);
            this.inspectionData.timer.Tick += (source, e) => this.HandleTimer(this.inspectionData);
        }

        ~ColorPlugin()
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
                        value = this.selectedData.hasValidData;
                    }
                }

                return value;
            }
        }

        public Color Selected2DPixelColor
        {
            get
            {
                Color value = Colors.Black;

                lock (this.lockObj)
                {
                    if (this.selectedData != null)
                    {
                        value = this.selectedData.selected2DPixelColor;
                    }

                    return value;
                }
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
                    ColorPlugin.DisposeData(this.monitorData, false);
                    break;

                case EventType.Inspection:
                    ColorPlugin.DisposeData(this.inspectionData, false);
                    break;
            }
        }

        public bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId)
        {
            return (dataTypeId == KStudioEventStreamDataTypeIds.UncompressedColor) ||
                   (dataTypeId == HackKStudioEventStreamDataTypeIds.UncompressedColorMonitor) ||
                   (dataTypeId == KStudioEventStreamDataTypeIds.CompressedColor) ||
                   (dataTypeId == HackKStudioEventStreamDataTypeIds.CompressedColorMonitor);
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
            DataTemplate dataTemplate = Resources.Get("ColorPluginPropertyViewDataTemplate") as DataTemplate;
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
                this.RaisePropertyChanged("Selected2DPixelColor");
            }
        }

        public void RemoveView(Guid viewId, Panel hostControl, IPluginViewSettings pluginViewSettings)
        {
        }

        public IPluginViewSettings Add2DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new ColorPlugin2DViewSettings();

            return pluginViewSettings;
        }

        public IPluginViewSettings Add3DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new ColorPlugin3DViewSettings();

            return pluginViewSettings;
        }

        public viz.Texture GetTexture(EventType eventType, IPluginViewSettings pluginViewSettings)
        {
            viz.Texture value = null;

            switch (eventType)
            {
                case EventType.Monitor:
                    value = this.GetTexture(this.monitorData);
                    break;

                case EventType.Inspection:
                    value = this.GetTexture(this.inspectionData);
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
                    ColorPlugin.DisposeData(this.monitorData, true);
                    ColorPlugin.DisposeData(this.inspectionData, true);
                }
            }
        }

        // object owning data should be locked
        private static void DisposeData(EventTypePluginData data, bool doAll)
        {
            Debug.Assert(data != null);

            if (doAll)
            {
                if (data.timer != null)
                {
                    data.timer.Stop();
                }

                // don't dispose this because others may be sharing the data
                data.sharedColorFrame = null;

                data.rgbaFrame = null;
                data.decompressMemoryStreamBuffer = null;
                data.convertColorBuffer = null;
            }

            if (data.colorTexture != null)
            {
                data.colorTexture.Dispose();
                data.colorTexture = null;
            }
        }

        private void InitializeRender(viz.Context context, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            lock (this.lockObj)
            {
                if (data.sharedColorFrame != null)
                {

                    data.needsUpdate = true;
                }
            }
        }

        private void ClearEvents(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            bool doEvent = false;

            lock (this.lockObj)
            {
                doEvent = this.selectedData == data;

                data.sharedColorFrame = null;

                if (data.colorTexture != null)
                {
                    data.colorTexture.Dispose();
                    data.colorTexture = null;

                    data.imageWidth = 0;
                    data.imageHeight = 0;
                }

                data.selected2DPixelX = 0;
                data.selected2DPixelY = 0;
                data.selected2DPixelColor = Colors.Black;
            }

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
                this.RaisePropertyChanged("Selected2DPixelX");
                this.RaisePropertyChanged("Selected2DPixelY");
                this.RaisePropertyChanged("Selected2DPixelColor");
            }
        }

        private void HandleTimer(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            bool doEvent = false;

            lock (this.lockObj)
            {
                data.timer.Stop();
                if (data.hasValidData)
                {
                    doEvent = true;
                    data.hasValidData = false;
                }
            }

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void HandleEvent(KStudioEvent eventObj, EventTypePluginData data, EventType eventType)
        {
            Debug.Assert(data != null);

            if (eventObj != null)
            {
                if ((eventObj.EventStreamDataTypeId == KStudioEventStreamDataTypeIds.UncompressedColor) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.UncompressedColorMonitor) ||
                    (eventObj.EventStreamDataTypeId == KStudioEventStreamDataTypeIds.CompressedColor) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.CompressedColorMonitor))
                {
                    lock (this.lockObj)
                    {
                        data.needsUpdate = true;

                        HGlobalBuffer newSharedFrame = eventObj.GetRetainableEventDataBuffer();

                        data.visibleChanged = (newSharedFrame == null) != (data.sharedColorFrame == null);

                        data.sharedColorFrame = newSharedFrame;

                        data.doDecompress =
                            (eventObj.EventStreamDataTypeId == KStudioEventStreamDataTypeIds.CompressedColor) ||
                            (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.CompressedColorMonitor);

                        uint newWidth = nui.Constants.STREAM_COLOR_WIDTH;
                        uint newHeight = nui.Constants.STREAM_COLOR_HEIGHT;

                        if ((newWidth != data.imageWidth) || (newHeight != data.imageHeight))
                        {
                            data.imageWidth = newWidth;
                            data.imageHeight = newHeight;

                            viz.Context context = null;
                            if (this.pluginService != null)
                            {
                                context = this.pluginService.GetContext(eventType);
                            }

                            ColorPlugin.CreateTextures(data, context);
                        }

                        if (!data.timer.IsEnabled)
                        {
                            data.timer.Start();
                        }
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
                uint offset = ((data.imageWidth * data.selected2DPixelY) + data.selected2DPixelX) * 4;

                if (data.rgbaFrame != null)
                {
                    changed = true;

                    byte red = data.rgbaFrame[offset];
                    byte green = data.rgbaFrame[offset + 1];
                    byte blue = data.rgbaFrame[offset + 2];

                    data.selected2DPixelColor = Color.FromRgb(red, green, blue);
                }
            }

            return changed;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private viz.Texture UpdateData(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            viz.Texture value = null;

            bool doDataEvent = false;
            bool doVisibleEvent = false;

            lock (this.lockObj)
            {
                bool updated = false;

                value = data.colorTexture;

                bool isSelectedData = (this.selectedData == data);

                data.visibleChanged = false;

                if (data.needsUpdate && (data.sharedColorFrame != null) && (data.sharedColorFrame.Buffer != IntPtr.Zero) && (data.colorTexture != null))
                {
                    try
                    {
                        if (data.doDecompress)
                        {
                            DateTime now = DateTime.Now;

                            if (now > this.nextDecompress)
                            {
                                data.timer.Stop();

                                doVisibleEvent = isSelectedData && (!data.hasValidData || data.visibleChanged);
                                data.needsUpdate = false;
                                data.hasValidData = true;

                                updated = true;

                                if ((data.decompressMemoryStreamBuffer == null) || (data.decompressMemoryStreamBuffer.Length < data.sharedColorFrame.Size))
                                {
                                    data.decompressMemoryStreamBuffer = new byte[data.sharedColorFrame.Size];
                                }

                                uint convertBufferSize = data.imageWidth * data.imageHeight * 3;

                                if ((data.convertColorBuffer == null) || (data.convertColorBuffer.Length < convertBufferSize))
                                {
                                    data.convertColorBuffer = new byte[convertBufferSize];
                                }

                                byte[] rgbBuffer = data.convertColorBuffer;

                                if (this.averageDecompressCount > 10)
                                {
                                    this.averageDecompressCount = 1;
                                    this.stopwatch.Reset();
                                }
                                else
                                {
                                    this.averageDecompressCount++;
                                }

                                this.stopwatch.Start();

                                Marshal.Copy(data.sharedColorFrame.Buffer, data.decompressMemoryStreamBuffer, 0, (int)data.sharedColorFrame.Size);

                                using (Stream imageStream = new MemoryStream(data.decompressMemoryStreamBuffer))
                                {
                                    // this has to run on the UI thread
                                    JpegBitmapDecoder decoder = new JpegBitmapDecoder(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

                                    BitmapSource bitmapSource = decoder.Frames[0];

                                    bitmapSource.CopyPixels(rgbBuffer, (int)(data.imageWidth * 3), 0);

                                    // expand bgr to rgba
                                    uint imageSize = data.imageWidth * data.imageHeight;

                                    for (uint index = 0; index < imageSize; ++index)
                                    {
                                        uint rgbaIndex = index * 4;
                                        uint bgrIndex = index * 3;
                                        data.rgbaFrame[rgbaIndex] = rgbBuffer[bgrIndex + 2];
                                        data.rgbaFrame[rgbaIndex + 1] = rgbBuffer[bgrIndex + 1];
                                        data.rgbaFrame[rgbaIndex + 2] = rgbBuffer[bgrIndex];
                                        data.rgbaFrame[rgbaIndex + 3] = 0xFF;
                                    }
                                }

                                this.stopwatch.Stop();

                                // color decompression takes a LONG time (sometimes upwards of 1/10 of  second), so throttle it

                                double averageDecompressTime = ((double)this.stopwatch.ElapsedMilliseconds) / this.averageDecompressCount;

                                double sinceLastDecompress = (now - this.lastDecompress).TotalMilliseconds;

                                if (sinceLastDecompress > 1000)
                                {
                                    this.nextDecompress = now + TimeSpan.FromMilliseconds(averageDecompressTime * 2);
                                }
                                else
                                {
                                    double minTime = Math.Max(sinceLastDecompress, averageDecompressTime * 2);

                                    this.nextDecompress = now + TimeSpan.FromMilliseconds(minTime);
                                }

                                this.lastDecompress = now;
                            }
                        }
                        else
                        {
                            data.timer.Stop();

                            doVisibleEvent = isSelectedData && (!data.hasValidData || data.visibleChanged);
                            data.needsUpdate = false;
                            data.hasValidData = true;
                            updated = true;

                            unsafe
                            {
                                fixed (byte* p = data.rgbaFrame)
                                {
                                    GCHandle rgbaFrameHandle = GCHandle.Alloc(data.rgbaFrame, GCHandleType.Pinned);

                                    NativeMethods.ConvertYUYVToRGBA(
                                        data.sharedColorFrame.Buffer,
                                        data.sharedColorFrame.Size,
                                        rgbaFrameHandle.AddrOfPinnedObject(),
                                        (uint)data.rgbaFrame.Length);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // TODO_LOG
                    }
                }

                if (updated)
                {
                    if (ColorPlugin.UpdateSelectedPixelValue(data))
                    {
                        doDataEvent = isSelectedData;
                    }

                    if ((data.colorTexture != null) && (data.rgbaFrame != null))
                    {
                        unsafe
                        {
                            fixed (byte* pFrame = data.rgbaFrame)
                            {
                                data.colorTexture.UpdateData(pFrame, (uint)data.rgbaFrame.Length);
                            }
                        }
                    }
                }
            }

            if (doVisibleEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
            }

            if (doDataEvent)
            {
                this.RaisePropertyChanged("Selected2DPixelColor");
            }

            return value;
        }

        // object owning data should be locked
        private static void CreateTextures(EventTypePluginData data, viz.Context context)
        {
            Debug.Assert(data != null);

            if (data.colorTexture != null)
            {
                data.colorTexture.Dispose();
                data.colorTexture = null;
            }

            data.rgbaFrame = new byte[data.imageWidth * data.imageHeight * sizeof(uint)];

            if (context != null)
            {
                data.colorTexture = new viz.Texture(context, data.imageWidth, data.imageHeight, viz.TextureFormat.R8G8B8A8_UNORM, false);
            }
        }

        private viz.Texture GetTexture(EventTypePluginData data)
        {
            Debug.Assert(data != null);

            viz.Texture value = null;

            value = this.UpdateData(data);

            return value;
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
                data.selected2DPixelColor = Colors.Black;

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

                ColorPlugin.UpdateSelectedPixelValue(data);
            }

            this.RaisePropertyChanged("Selected2DPixelX");
            this.RaisePropertyChanged("Selected2DPixelY");
            this.RaisePropertyChanged("Selected2DPixelColor");

            if (doEvent)
            {
                this.RaisePropertyChanged("HasSelected2DPixelData");
            }
        }

        private static class NativeMethods
        {
            // P/Invoke into kinect20.dll
            [DllImport("kinect20.dll")]
            public static extern void ConvertYUYVToRGBA(IntPtr pYUYV, uint cbYUYV, IntPtr pRGBA, uint cbRGBA);
        }

        private class EventTypePluginData
        {
            public bool needsUpdate = false;
            public bool hasValidData = false;
            public bool visibleChanged = false;
            public HGlobalBuffer sharedColorFrame = null;
            public bool doDecompress = false;
            public byte[] rgbaFrame = null;
            public byte[] decompressMemoryStreamBuffer = null;
            public byte[] convertColorBuffer = null;
            public viz.Texture colorTexture = null;
            public uint imageWidth = 0;
            public uint imageHeight = 0;
            public uint selected2DPixelX = 0;
            public uint selected2DPixelY = 0;
            public Color selected2DPixelColor = Colors.Black;
            public readonly DispatcherTimer timer = new DispatcherTimer();
        }

        private object lockObj = new object();
        private readonly IPluginService pluginService = null;
        private readonly EventTypePluginData monitorData = new EventTypePluginData();
        private readonly EventTypePluginData inspectionData = new EventTypePluginData();
        private readonly Stopwatch stopwatch = new Stopwatch();
        private EventTypePluginData selectedData = null;
        private int averageDecompressCount = 0;
        private DateTime nextDecompress = DateTime.MinValue; 
        private DateTime lastDecompress = DateTime.MinValue; 
    }
}
