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

    public class AccessoryPlugin : BasePlugin, IEventHandlerPlugin, I3DVisualPlugin, IImageVisualPlugin, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "serviceProvider")]
        public AccessoryPlugin(IServiceProvider serviceProvider)
            : base(Strings.Accessory_Plugin_Title, new Guid(0xd1ec6fb2, 0xb19d, 0x4285, 0x9b, 0x69, 0xdc, 0x92, 0x1f, 0xeb, 0xf6, 0x9f))
        {
        }

        ~AccessoryPlugin()
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
            if (context != null)
            {
                lock (this.lockObj)
                {
                    switch (eventType)
                    {
                        case EventType.Monitor:
                            this.monitorAccessory = new viz.Accessory(context, 
                                AccessoryPlugin.cKinectSensorFovX, AccessoryPlugin.cKinectSensorFovY,
                                AccessoryPlugin.cKinectSensorMinZmm, AccessoryPlugin.cKinectSensorMaxZmm);
                            break;

                        case EventType.Inspection:
                            this.inspectionAccessory = new viz.Accessory(context,
                                AccessoryPlugin.cKinectSensorFovX, AccessoryPlugin.cKinectSensorFovY,
                                AccessoryPlugin.cKinectSensorMinZmm, AccessoryPlugin.cKinectSensorMaxZmm);
                            break;
                    }
                }
            }
        }

        public void UninitializeRender(EventType eventType)
        {
            lock (this.lockObj)
            {
                switch (eventType)
                {
                    case EventType.Monitor:
                        if (this.monitorAccessory != null)
                        {
                            this.monitorAccessory.Dispose();
                            this.monitorAccessory = null;
                        }
                        break;

                    case EventType.Inspection:
                        if (this.inspectionAccessory != null)
                        {
                            this.inspectionAccessory.Dispose();
                            this.inspectionAccessory = null;
                        }
                        break;
                }
            }
        }

        public bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId)
        {
            return (dataTypeId == KStudioEventStreamDataTypeIds.Body) || (dataTypeId == HackKStudioEventStreamDataTypeIds.BodyMonitor);
        }

        public void ClearEvents(EventType eventType)
        {
            lock (this.lockObj)
            {
                switch (eventType)
                {
                    case EventType.Monitor:
                        if (this.monitorAccessory != null)
                        {
                            this.monitorAccessory.UpdateFloorPlaneAndUpVector(new viz.Vector(0, 0, 0, 0), new viz.Vector(0, 0, 0, 0));
                        }
                        break;

                    case EventType.Inspection:
                        if (this.inspectionAccessory != null)
                        {
                            this.inspectionAccessory.UpdateFloorPlaneAndUpVector(new viz.Vector(0, 0, 0, 0), new viz.Vector(0, 0, 0, 0));
                        }
                        break;
                }
            }
        }

        public void HandleEvent(EventType eventType, KStudioEvent eventObj)
        {
            lock (this.lockObj)
            {
                switch (eventType)
                {
                    case EventType.Monitor:
                        AccessoryPlugin.HandleEvent(eventObj, this.monitorAccessory);
                        break;

                    case EventType.Inspection:
                        AccessoryPlugin.HandleEvent(eventObj, this.inspectionAccessory);
                        break;
                }
            }
        }

        public void RemoveView(Guid viewId, Panel hostControl, IPluginViewSettings pluginViewSettings)
        {
        }

        public IPluginViewSettings Add3DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new AccessoryPlugin3DViewSettings();

            return pluginViewSettings;
        }

        public viz.Texture GetTexture(EventType eventType, IPluginViewSettings pluginViewSettings)
        {
            return null;
        }

        public void Render3D(EventType eventType, IPluginViewSettings pluginViewSettings, viz.Context context, viz.Texture texture)
        {
            lock (this.lockObj)
            {
                switch (eventType)
                {
                    case EventType.Monitor:
                        AccessoryPlugin.Render3D(pluginViewSettings, this.monitorAccessory);
                        break;

                    case EventType.Inspection:
                        AccessoryPlugin.Render3D(pluginViewSettings, this.inspectionAccessory);
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
                    if (this.monitorAccessory != null)
                    {
                        this.monitorAccessory.Dispose();
                        this.monitorAccessory = null;
                    }
                    if (this.inspectionAccessory != null)
                    {
                        this.inspectionAccessory.Dispose();
                        this.inspectionAccessory = null;
                    }
                }
            }
        }

        // instance for accessory should be locked
        private static void HandleEvent(KStudioEvent eventObj, viz.Accessory accessory)
        {
            if (eventObj != null)
            {
                if ((eventObj.EventStreamDataTypeId == KStudioEventStreamDataTypeIds.Body) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.BodyMonitor))
                {
                    if (accessory != null)
                    {
                        uint bufferSize;
                        IntPtr bufferPtr;

                        eventObj.AccessUnderlyingEventDataBuffer(out bufferSize, out bufferPtr);

                        if (bufferSize >= AccessoryPlugin.cMinimumBodyFrameSize)
                        {
                            viz.Vector floorPlane;
                            viz.Vector upVector;

                            unsafe
                            {
                                float* pFloats = (float*)((bufferPtr + AccessoryPlugin.cFloorClipPlaneOffset).ToPointer());

                                if (pFloats[3] == 0.0f)
                                {
                                    // if W is 0, assume no real floor plane
                                    floorPlane = new viz.Vector(0.0f, 0.0f, 0.0f, 0.0f); 
                                }
                                else
                                {
                                    floorPlane = new viz.Vector(pFloats[0], pFloats[1], pFloats[2], pFloats[3]);
                                }

                                pFloats = (float*)((bufferPtr + AccessoryPlugin.cUpVectorOffset).ToPointer());
                                upVector =  new viz.Vector(0.0f, 0.0f, 0.0f, 0.0f); // never use up vector for visualizing the floor

                                accessory.UpdateFloorPlaneAndUpVector(floorPlane, upVector);
                            }
                        }
                    }
                }
            }
        }

        // object owning accessory should be locked
        private static void Render3D(IPluginViewSettings pluginViewSettings, viz.Accessory accessory)
        {
            AccessoryPlugin3DViewSettings accessoryViewSettings = pluginViewSettings as AccessoryPlugin3DViewSettings;
            if ((accessoryViewSettings != null) && (accessory != null))
            {
                if (accessoryViewSettings.RenderOrientationCube)
                {
                    accessory.SetMode(viz.AccessoryMode.RotationCube);
                    accessory.Render();
                }

                if (accessoryViewSettings.RenderFrustum)
                {
                    accessory.SetMode(viz.AccessoryMode.ViewFrustum);
                    accessory.Render();
                }

                if (accessoryViewSettings.RenderFloorPlane)
                {
                    accessory.SetMode(viz.AccessoryMode.FloorPlane);
                    accessory.Render();
                }
            }
        }

        private const float cKinectSensorFovX = (float)Math.PI * 70 / 180;
        private const float cKinectSensorFovY = (float)Math.PI * 60 / 180;
        private const float cKinectSensorMinZmm = 500;
        private const float cKinectSensorMaxZmm = 8000;
        private static readonly int cFloorClipPlaneOffset = Marshal.OffsetOf(typeof(nui.BODY_FRAME), "FloorClipPlane").ToInt32();
        private static readonly int cUpVectorOffset = Marshal.OffsetOf(typeof(nui.BODY_FRAME), "Up").ToInt32();
        private static readonly int cMinimumBodyFrameSize = Math.Max(AccessoryPlugin.cFloorClipPlaneOffset, AccessoryPlugin.cUpVectorOffset) + (sizeof(float) * 4 * 2);

        private object lockObj = new object();
        private viz.Accessory monitorAccessory = null;
        private viz.Accessory inspectionAccessory = null;
    }
}


