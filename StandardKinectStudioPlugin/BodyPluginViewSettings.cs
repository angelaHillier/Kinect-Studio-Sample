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

    internal abstract class BodyPluginViewSettings : KStudioUserState, IPluginEditableViewSettings
    {
        protected BodyPluginViewSettings()
        {
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

        public abstract DataTemplate SettingsEditDataTemplate { get; }

        public string RequirementsToolTip
        {
            get
            {
                return Strings.Body_Requirements_ToolTip;
            }
        }

        public bool AreRequirementsSatisfied
        {
            get
            {
                lock (BodyPluginViewSettings.lockObj)
                {
                    return this.requirementsSatisified;
                }
            }
            private set
            {
                DebugHelper.AssertUIThread();

                bool doEvent = false;

                lock (BodyPluginViewSettings.lockObj)
                {
                    if (this.requirementsSatisified != value)
                    {
                        this.requirementsSatisified = value;
                        doEvent = true;
                    }
                }

                if (doEvent)
                {
                    this.RaisePropertyChanged("AreRequirementsSatisfied");
                }
            }
        }

        public void CheckRequirementsSatisfied(HashSet<KStudioEventStreamIdentifier> availableStreamIds)
        {
            bool satisfied = false;

            if (availableStreamIds == null)
            {
                satisfied = true;
            }
            else
            {
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(KStudioEventStreamDataTypeIds.Body));
                satisfied |= availableStreamIds.Contains(new KStudioEventStreamIdentifier(HackKStudioEventStreamDataTypeIds.BodyMonitor));
            }

            this.AreRequirementsSatisfied = satisfied;
        }

        public void ReadFrom(XElement element)
        {
            if (element != null)
            {
                lock (BodyPluginViewSettings.lockObj)
                {
                    this.bodies = XmlExtensions.GetAttribute(element, "bodies", this.bodies);
                    this.hands = XmlExtensions.GetAttribute(element, "hands", this.hands);

                    OnReadFrom(element);
                }
            }
        }

        public void WriteTo(XElement element)
        {
            if (element != null)
            {
                element.RemoveAll();

                lock (BodyPluginViewSettings.lockObj)
                {
                    element.SetAttributeValue("bodies", this.bodies.ToString());
                    element.SetAttributeValue("hands", this.hands.ToString());

                    OnWriteTo(element);
                }
            }
        }

        public abstract IPluginEditableViewSettings CloneForEdit();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderBodies
        {
            get
            {
                lock (BodyPluginViewSettings.lockObj)
                {
                    return this.bodies;
                }
            }
            set
            {
                bool doEvent = false;

                lock (BodyPluginViewSettings.lockObj)
                {
                    if (this.bodies != value)
                    {
                        doEvent = true;
                        this.bodies = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("RenderBodies");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool RenderHands
        {
            get
            {
                lock (BodyPluginViewSettings.lockObj)
                {
                    return this.hands;
                }
            }
            set
            {
                bool doEvent = false;

                lock (BodyPluginViewSettings.lockObj)
                {
                    if (this.hands != value)
                    {
                        doEvent = true;
                        this.hands = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("RenderHands");
                }
            }
        }

        protected BodyPluginViewSettings(BodyPluginViewSettings source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.bodies = source.bodies;
            this.hands = source.hands;
        }

        protected virtual void OnReadFrom(XElement element)
        {
        }

        protected virtual void OnWriteTo(XElement element)
        {
        }

        private bool bodies = true;
        private bool hands = true;

        protected static readonly object lockObj = new object(); // no need for a lock for each instance
        private bool requirementsSatisified = true;
    }
}
