//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System;
    using System.Windows;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal class BodyPlugin3DViewSettings : BodyPluginViewSettings
    {
        public BodyPlugin3DViewSettings()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsSupplyingTexture
        {
            get
            {
                return false;
            }
        }

        public override DataTemplate SettingsEditDataTemplate
        {
            get
            {
                return Resources.Get("BodyPlugin3DViewSettingsEditDataTemplate") as DataTemplate;
            }
        }

        // should be locked
        protected override void OnReadFrom(XElement element)
        {
            if (element != null)
            {
                lock (BodyPluginViewSettings.lockObj)
                {
                    this.jointOrientations = XmlExtensions.GetAttribute(element, "jointOrientations", this.jointOrientations);
                    this.info = XmlExtensions.GetAttribute(element, "info", this.info);
                }
            }
        }

        // should be locked
        protected override void OnWriteTo(XElement element)
        {
            if (element != null)
            {
                lock (BodyPluginViewSettings.lockObj)
                {
                    element.SetAttributeValue("jointOrientations", this.jointOrientations.ToString());
                    element.SetAttributeValue("info", this.info.ToString());
                }
            }
        }

        public override IPluginEditableViewSettings CloneForEdit()
        {
            DebugHelper.AssertUIThread();

            lock (BodyPluginViewSettings.lockObj)
            {
                return new BodyPlugin3DViewSettings(this);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderJointOrientations
        {
            get
            {
                lock (BodyPluginViewSettings.lockObj)
                {
                    return this.jointOrientations;
                }
            }
            set
            {
                bool doEvent = false;

                lock (BodyPluginViewSettings.lockObj)
                {
                    if (this.jointOrientations != value)
                    {
                        doEvent = true;
                        this.jointOrientations = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("RenderJointOrientations");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderInfo
        {
            get
            {
                lock (BodyPluginViewSettings.lockObj)
                {
                    return this.info;
                }
            }
            set
            {
                bool doEvent = false;

                lock (BodyPluginViewSettings.lockObj)
                {
                    if (this.info != value)
                    {
                        doEvent = true;
                        this.info = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("RenderInfo");
                }
            }
        }

        private BodyPlugin3DViewSettings(BodyPlugin3DViewSettings source)
            :base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.jointOrientations = source.jointOrientations;
            this.info = source.info;
        }

        private bool jointOrientations = false;
        private bool info = true;
    }
}
