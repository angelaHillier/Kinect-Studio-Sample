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
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Kinect.Tools;
    using nui = Microsoft.Xbox.Input.Nui;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    public class BodyPlugin : BasePlugin, IEventHandlerPlugin, I2DVisualPlugin, I3DVisualPlugin, IImageVisualPlugin, IDisposable
    {
        public BodyPlugin(IServiceProvider serviceProvider)
            : base(Strings.Body_Plugin_Title, new Guid(0x85a371bc, 0x7bb2, 0x4534, 0x86, 0x5d, 0xb7, 0x2, 0x67, 0x54, 0xe8, 0x76))
        {
            if (serviceProvider != null)
            {
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }
        }

        ~BodyPlugin()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
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

        public bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId)
        {
            return (dataTypeId == KStudioEventStreamDataTypeIds.Body) || (dataTypeId == HackKStudioEventStreamDataTypeIds.BodyMonitor) ||
                (dataTypeId == HackKStudioEventStreamDataTypeIds.Calibration) || (dataTypeId == HackKStudioEventStreamDataTypeIds.CalibrationMonitor) ||
                (dataTypeId == HackKStudioEventStreamDataTypeIds.SystemInfo);
        }

        public void ClearEvents(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Monitor:
                    Debug.Assert(this.monitorData != null);
                    this.monitorData.sharedBodyFrame = null;
                    this.monitorData.bodiesValid = false;
                    break;

                case EventType.Inspection:
                    Debug.Assert(this.inspectionData != null);
                    this.inspectionData.sharedBodyFrame = null;
                    this.inspectionData.bodiesValid = false;
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
            return null;
        }

        public void UpdatePropertyView(EventType eventType, double x, double y, uint width, uint height)
        {
        }

        public void ClearPropertyView()
        {
        }

        public void RemoveView(Guid viewId, Panel hostControl, IPluginViewSettings pluginViewSettings)
        {
        }

        public IPluginViewSettings Add2DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new BodyPlugin2DViewSettings();

            return pluginViewSettings;
        }

        public IPluginViewSettings Add3DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new BodyPlugin3DViewSettings();

            return pluginViewSettings;
        }

        public viz.Texture GetTexture(EventType eventType, IPluginViewSettings pluginViewSettings)
        {
            return null;
        }

        public void Render2D(EventType eventType, IPluginViewSettings pluginViewSettings, viz.Context context, viz.Texture texture, float left, float top, float width, float height)
        {
            lock (this.lockObj)
            {
                nui.Registration registration = null;

                if (this.pluginService != null)
                {
                    registration = this.pluginService.GetRegistration(eventType);
                }

                switch (eventType)
                {
                    case EventType.Monitor:
                        BodyPlugin.Render2D(pluginViewSettings, texture, left, top, width, height, this.monitorData, registration);
                        break;

                    case EventType.Inspection:
                        BodyPlugin.Render2D(pluginViewSettings, texture, left, top, width, height, this.inspectionData, registration);
                        break;
                }
            }
        }

        public void Render3D(EventType eventType, IPluginViewSettings pluginViewSettings, viz.Context context, viz.Texture texture)
        {
            lock (this.lockObj)
            {
                nui.Registration registration = null;

                if (this.pluginService != null)
                {
                    registration = this.pluginService.GetRegistration(eventType);
                }

                switch (eventType)
                {
                    case EventType.Monitor:
                        BodyPlugin.Render3D(pluginViewSettings, this.monitorData, registration);
                        break;

                    case EventType.Inspection:
                        BodyPlugin.Render3D(pluginViewSettings, this.inspectionData, registration);
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
                    BodyPlugin.DisposeData(this.monitorData, true);
                    BodyPlugin.DisposeData(this.inspectionData, true);
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
                data.sharedBodyFrame = null;
            }

            if (data.body != null)
            {
                data.body.Dispose();
                data.body = null;
            }

            if (data.font != null)
            {
                data.font.Dispose();
                data.font = null;
            }
        }

        private void InitializeRender(viz.Context context, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            lock (this.lockObj)
            {
                Debug.Assert(data.body == null);
                Debug.Assert(data.font == null);

                if (context != null)
                {
                    data.body = new viz.Body(context);
                    data.font = new viz.Font(context);

                    if (data.bodiesValid && (data.sharedBodyFrame != null))
                    {
                        if (Marshal.SizeOf(typeof(nui.BODY_FRAME)) == data.sharedBodyFrame.Size)
                        {
                            data.body.UpdateData(data.sharedBodyFrame.Buffer);
                        }
                    }
                }
            }
        }

        public void UninitializeRender(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Monitor:
                    BodyPlugin.DisposeData(this.monitorData, false);
                    break;

                case EventType.Inspection:
                    BodyPlugin.DisposeData(this.inspectionData, false);
                    break;
            }
        }

        private void HandleEvent(EventType eventType, KStudioEvent eventObj, EventTypePluginData data)
        {
            Debug.Assert(data != null);

            if (eventObj != null)
            {
                if ((eventObj.EventStreamDataTypeId == KStudioEventStreamDataTypeIds.Body) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.BodyMonitor))
                {
                    lock (this.lockObj)
                    {
                        data.sharedBodyFrame = eventObj.GetRetainableEventDataBuffer();

                        uint bufferSize;
                        IntPtr bufferPtr;

                        eventObj.AccessUnderlyingEventDataBuffer(out bufferSize, out bufferPtr);

                        if (Marshal.SizeOf(typeof(nui.BODY_FRAME)) == bufferSize)
                        {
                            data.body.UpdateData(bufferPtr); 
                            
                            data.bodiesValid = true;
                        }
                    }
                }
            }
        }

        // object owning data should be locked
        private static void Render2D(IPluginViewSettings pluginViewSettings, viz.Texture texture, float left, float top, float width, float height, EventTypePluginData data, nui.Registration registration)
        {
            Debug.Assert(data != null);

            BodyPlugin2DViewSettings bodyPluginViewSettings = pluginViewSettings as BodyPlugin2DViewSettings;

            if ((bodyPluginViewSettings != null) && (registration != null) && (data.body != null) && data.bodiesValid)
            {
                if (bodyPluginViewSettings.RenderBodies || bodyPluginViewSettings.RenderHands)
                {
                    viz.Body2DMode bodyMode = viz.Body2DMode.DepthIR;
                    if ((texture != null) && (texture.GetWidth() == nui.Constants.STREAM_COLOR_WIDTH) && (texture.GetHeight() == nui.Constants.STREAM_COLOR_HEIGHT))
                    {
                        bodyMode = viz.Body2DMode.Color;
                    }

                    data.body.Begin2D(left, top, width, height, bodyMode, registration.GetCalibrationData());

                    for (uint i = 0; i < BodyPlugin.bodyOptions.Length; ++i)
                    {
                        if (bodyPluginViewSettings.RenderBodies)
                        {
                            viz.Vector color = BodyPlugin.bodyOptions[i].ColorVector;
                            color.A = 0.7f;
                            data.body.RenderBones2D(i, color);
                        }

                        if (bodyPluginViewSettings.RenderHands)
                        {
                            data.body.RenderHandStates2D(i);
                        }
                    }

                    data.body.End2D();
                }
            }
        }

        // object owning data should be locked
        private static void Render3D(IPluginViewSettings pluginViewSettings, EventTypePluginData data, nui.Registration registration)
        {
            Debug.Assert(data != null);

            BodyPlugin3DViewSettings bodyPluginViewSettings = pluginViewSettings as BodyPlugin3DViewSettings;

            if ((bodyPluginViewSettings != null) && (registration != null) && (data.body != null) && data.bodiesValid)
            {
                if (bodyPluginViewSettings.RenderBodies || bodyPluginViewSettings.RenderHands)
                {
                    data.body.Begin();

                    for (uint i = 0; i < BodyPlugin.bodyOptions.Length; ++i)
                    {
                        if (bodyPluginViewSettings.RenderBodies)
                        {
                            BodyOptions bodyOption = BodyPlugin.bodyOptions[i];

                            data.body.RenderBones(i, bodyOption.BoneEffect);

                            if (bodyPluginViewSettings.RenderJointOrientations)
                            {
                                data.body.RenderJointOrientations(i, bodyOption.JointEffect);
                            }

                            data.body.RenderJoints(i, bodyOption.JointEffect);

                            if (bodyPluginViewSettings.RenderInfo && (data.font != null))
                            {
                                // PlayderIndex (sic)
                                data.body.RenderInfo(i, viz.BodyInfoFlag.PlayderIndex, data.font, bodyOption.ColorVector);
                            }
                        }

                        if (bodyPluginViewSettings.RenderHands)
                        {
                            data.body.RenderHandStates(i);
                        }
                    }

                    data.body.End(0.0f);
                }
            }
        }

        private class EventTypePluginData
        {
            public HGlobalBuffer sharedBodyFrame = null;
            public viz.Body body = null;
            public viz.Font font = null;
            public bool bodiesValid = false;
        }

        private object lockObj = new object();
        private IPluginService pluginService = null;
        private readonly EventTypePluginData monitorData = new EventTypePluginData();
        private readonly EventTypePluginData inspectionData = new EventTypePluginData();

        private struct BodyOptions
        {
            public BodyOptions(float r, float g, float b)
            {
                BoneEffect = new viz.Effect
                    {
                        EnableLighting = true,
                        Ambient = new viz.Vector(1.0f, 0.0f, 0.0f, 1.0f) { R = r, G = g, B = b },
                        Specular = new viz.Vector(1.0f, 1.0f, 1.0f, 1.0f),
                        Power = 22.0f
                    };
                JointEffect = new viz.Effect { Diffuse = new viz.Vector(0.0f, 1.0f, 0.0f, 1.0f) }; ;
                ColorVector = new viz.Vector(1, 1, 1, 1) { R = r, G = g, B = b };
            }

            public readonly viz.Effect BoneEffect;
            public readonly viz.Effect JointEffect;
            public readonly viz.Vector ColorVector; // for use for  the body text.
        }

        private static readonly BodyOptions[] bodyOptions = new BodyOptions[]
            {
                new BodyOptions(1, 0, 0),
                new BodyOptions(0, 1, 0),
                new BodyOptions(0.25f, 1, 1),
                new BodyOptions(1, 1, 0.25f),
                new BodyOptions(1, 0.25f, 1),
                new BodyOptions(0.5f, 0.5f, 1),
            };
    }
}
