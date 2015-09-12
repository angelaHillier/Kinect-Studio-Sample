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
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Xml.Linq;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    internal abstract class IrPluginViewSettings : KStudioUserState, IPluginEditableViewSettings, IDisposable 
    {
        protected IrPluginViewSettings(IPluginService pluginService, EventType eventType)
        {
            this.pluginService = pluginService;
            this.eventType = eventType;
        }

        protected IrPluginViewSettings(IrPluginViewSettings source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            this.pluginService = source.pluginService;
            this.eventType = source.eventType;
            this.lastContext = source.lastContext;
            this.amplification = source.amplification;
            this.gamma = source.gamma;
        }

        ~IrPluginViewSettings()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        public abstract bool IsRendingOpaque { get; }

        public abstract bool OtherIsRenderingOpaque();

        public abstract DataTemplate SettingsEditDataTemplate { get; }

        public abstract IPluginEditableViewSettings CloneForEdit();

        public abstract string RequirementsToolTip { get; }

        public bool AreRequirementsSatisfied
        {
            get
            {
                lock (IrPluginViewSettings.lockObj)
                {
                    return this.requirementsSatisified;
                }
            }
            protected set
            {
                DebugHelper.AssertUIThread();

                bool doEvent = false;

                lock (IrPluginViewSettings.lockObj)
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

        public abstract void CheckRequirementsSatisfied(HashSet<KStudioEventStreamIdentifier> availableStreamIds);

        public virtual void ReadFrom(XElement element)
        {
            if (element != null)
            {
                lock (IrPluginViewSettings.lockObj)
                {
                    this.amplification = XmlExtensions.GetAttribute(element, "amplification", this.amplification);
                    this.gamma = XmlExtensions.GetAttribute(element, "gamma", this.gamma);

                    if (this.rampTexture != null)
                    {
                        this.rampTexture.Dispose();
                        this.rampTexture = null;
                    }
                }
            }
        }

        public virtual void WriteTo(XElement element)
        {
            if (element != null)
            {
                element.RemoveAll();

                lock (IrPluginViewSettings.lockObj)
                {
                    element.SetAttributeValue("amplification", this.amplification.ToString(CultureInfo.InvariantCulture));
                    element.SetAttributeValue("gamma", this.gamma.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        public viz.Texture RampTexture
        {
            get
            {
                viz.Texture value = null;

                viz.Context context = null;
                if (this.pluginService != null)
                {
                    context = this.pluginService.GetContext(this.eventType);
                }
                
                lock (IrPluginViewSettings.lockObj)
                {
                    if (this.lastContext != context)
                    {
                        if (this.rampTexture != null)
                        {
                            this.rampTexture.Dispose();
                            this.rampTexture = null;
                        }

                        this.lastContext = context;
                    }

                    if ((this.rampTexture == null) && (context != null))
                    {
                        this.rampTexture = new viz.Texture(context, IrPluginViewSettings.cRampTextureLength, 1, viz.TextureFormat.R8G8B8A8_UNORM, false);

                        UpdateRampTexture();
                    }

                    value = this.rampTexture;
                }

                return value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public float Amplification
        {
            get
            {
                lock (IrPluginViewSettings.lockObj)
                {
                    return this.amplification;
                }
            }
            set
            {
                bool doEvent = false;

                lock (IrPluginViewSettings.lockObj)
                {
                    if (this.amplification != value)
                    {
                        doEvent = true;
                        this.amplification = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("Amplification");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public float Gamma
        {
            get
            {
                lock (IrPluginViewSettings.lockObj)
                {
                    return this.gamma;
                }
            }
            set
            {
                bool doEvent = false;

                lock (IrPluginViewSettings.lockObj)
                {
                    if (this.gamma != value)
                    {
                        doEvent = true;
                        this.gamma = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("Gamma");
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (IrPluginViewSettings.lockObj)
                {
                    if (this.rampTexture != null)
                    {
                        this.rampTexture.Dispose();
                        this.rampTexture = null;
                    }
                }
            }
        }

        // should be locked or initializing
        private void UpdateRampTexture()
        {
            Debug.Assert(this.rampTexture != null);

            int[] rampData = new int[IrPluginViewSettings.cRampTextureLength];

            for (int i = 0; i < rampData.Length; i++)
            {
                rampData[i] = IRToRGBA((float)i / (float)(rampData.Length - 1), this.amplification, this.gamma);
            }
            unsafe
            {
                fixed (int* pRampData = rampData)
                {
                    this.rampTexture.UpdateData((byte*)pRampData, (uint)rampData.Length * sizeof(int));
                }
            }
        }

        private static int IRToRGBA(float normalizedValue, float amplification, float gamma)
        {
            double normalized = (double)normalizedValue;
            double gammaCorrected = Math.Min(1, amplification * Math.Pow(normalized, gamma));
            int greyValue = (int)(255 * gammaCorrected);
            int alpha = 255;
            return (alpha << 24) | (greyValue << 16) | (greyValue << 8) | greyValue;
        }

        private const uint cRampTextureLength = 512;

        private static readonly object lockObj = new object(); // no need for a lock for each instance

        private readonly IPluginService pluginService;
        private readonly EventType eventType;
        private viz.Context lastContext = null;
        private viz.Texture rampTexture = null;

        private float amplification = 1.0f;
        private float gamma = 0.32f;

        private bool requirementsSatisified = true;
    }
}
