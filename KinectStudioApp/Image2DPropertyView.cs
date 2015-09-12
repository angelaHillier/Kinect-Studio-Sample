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

    public class Image2DPropertyView : View
    {
        public Image2DPropertyView(IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            this.serviceProvider = serviceProvider;
        }

        protected override FrameworkElement CreateViewContent()
        {
            DebugHelper.AssertUIThread();

            if (this.viewContent == null)
            {
                this.viewContent = new Image2DPropertyViewContent(this.serviceProvider, viewSettings);
            }

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
                viewSettingsElement = this.viewSettings.Save();
            }

            return viewSettingsElement;
        }

        public static View CreateView(IServiceProvider serviceProvider)
        {
            return new Image2DPropertyView(serviceProvider);
        }

        private readonly IServiceProvider serviceProvider;
        private readonly VisualizationViewSettings viewSettings = new VisualizationViewSettings();
        private Image2DPropertyViewContent viewContent = null;
    }
}
