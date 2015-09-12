//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal class AccessoryPlugin3DViewSettings : KStudioUserState, IPluginEditableViewSettings
    {
        public AccessoryPlugin3DViewSettings()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsSupplyingSurface
        {
            get
            {
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsSupplyingTexture
        {
            get
            {
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsRendingOpaque
        {
            get
            {
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public bool OtherIsRenderingOpaque()
        {
            return true;
        }

        public DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("AccessoryPlugin3DViewSettingsEditDataTemplate") as DataTemplate;
            }
        }

        public string RequirementsToolTip
        {
            get
            {
                return null;
            }
        }

        public bool AreRequirementsSatisfied
        {
            get
            {
                lock (AccessoryPlugin3DViewSettings.lockObj)
                {
                    // always available
                    return true; 
                }
            }
        }

        public void CheckRequirementsSatisfied(HashSet<KStudioEventStreamIdentifier> availableStreamIds)
        {
            // always available
        }

        public void ReadFrom(XElement element)
        {
            if (element != null)
            {
                lock (AccessoryPlugin3DViewSettings.lockObj)
                {
                    this.frustum = XmlExtensions.GetAttribute(element, "frustum", this.frustum);
                    this.orientationCube = XmlExtensions.GetAttribute(element, "orientationCube", this.orientationCube);
                    this.floorPlane = XmlExtensions.GetAttribute(element, "floorPlane", this.floorPlane);
                }
            }
        }

        public void WriteTo(XElement element)
        {
            if (element != null)
            {
                element.RemoveAll();

                lock (AccessoryPlugin3DViewSettings.lockObj)
                {
                    element.SetAttributeValue("frustum", this.frustum.ToString());
                    element.SetAttributeValue("orientationCube", this.orientationCube.ToString());
                    element.SetAttributeValue("floorPlane", this.floorPlane.ToString());
                }
            }
        }

        public IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            lock (AccessoryPlugin3DViewSettings.lockObj)
            {
                return new AccessoryPlugin3DViewSettings(this);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderFrustum
        {
            get
            {
                lock (AccessoryPlugin3DViewSettings.lockObj)
                {
                    return this.frustum;
                }
            }
            set
            {
                bool doEvent = false;

                lock (AccessoryPlugin3DViewSettings.lockObj)
                {
                    if (this.frustum != value)
                    {
                        doEvent = true;
                        this.frustum = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("RenderFrustum");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderOrientationCube
        {
            get
            {
                lock (AccessoryPlugin3DViewSettings.lockObj)
                {
                    return this.orientationCube;
                }
            }
            set
            {
                bool doEvent = false;

                lock (AccessoryPlugin3DViewSettings.lockObj)
                {
                    if (this.orientationCube != value)
                    {
                        doEvent = true;
                        this.orientationCube = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("RenderOrientationCube");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderFloorPlane
        {
            get
            {
                lock (AccessoryPlugin3DViewSettings.lockObj)
                {
                    return this.floorPlane;
                }
            }
            set
            {
                bool doEvent = false;

                lock (AccessoryPlugin3DViewSettings.lockObj)
                {
                    if (this.floorPlane != value)
                    {
                        doEvent = true;
                        this.floorPlane = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("RenderFloorPlane");
                }
            }
        }

        private AccessoryPlugin3DViewSettings(AccessoryPlugin3DViewSettings source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.frustum = source.frustum;
            this.orientationCube = source.orientationCube;
            this.floorPlane = source.floorPlane;
        }

        private bool frustum = true;
        private bool orientationCube = true;
        private bool floorPlane = true;

        private static readonly object lockObj = new object(); // no need for a lock for each instance
    }
}


