//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Windows;
    using System.Xml.Linq;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class Image3DVisualizationView : View
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "serviceProvider")]
        public Image3DVisualizationView(IServiceProvider serviceProvider, EventType eventType)
        {
            DebugHelper.AssertUIThread();

            this.eventType = eventType;

            if (serviceProvider != null)
            {
                this.availableStreamsGetter = serviceProvider.GetService(typeof(IKStudioService)) as IKStudioService;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected override FrameworkElement CreateViewContent()
        {
            DebugHelper.AssertUIThread();

            this.viewContent = new Image3DVisualizationViewContent(this.ServiceProvider, this.eventType, this.viewSettings, this.availableStreamsGetter);

            return this.viewContent;
        }

        public override void LoadViewState(XElement state)
        {
            DebugHelper.AssertUIThread();

            if (this.viewSettings != null)
            {
                this.viewSettings.Load(state);
            }
        }

        public override XElement GetViewState()
        {
            XElement viewSettingsElement = null;

            if (this.viewSettings != null)
            {
                if (this.viewContent != null)
                {
                    Image3DVisualizationControl child = this.viewContent.FindVisualChild<Image3DVisualizationControl>();
                    if (child != null)
                    {
                        child.RefreshSettings();
                    }
                }

                viewSettingsElement = this.viewSettings.Save();
            }

            return viewSettingsElement;
        }

        public static View CreateView(IServiceProvider serviceProvider, EventType eventType)
        {
            return new Image3DVisualizationView(serviceProvider, eventType);
        }

        private EventType eventType;
        private IAvailableStreams availableStreamsGetter;
        private Image3DVisualizationViewContent viewContent = null;
        private VisualizationViewSettings viewSettings = new VisualizationViewSettings();
    }
}
