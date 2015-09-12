//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Xml.Linq;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Playbackable")]
    public class PlaybackableStreamsView : View
    {
        public PlaybackableStreamsView(IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            if (serviceProvider != null)
            {
                this.kstudioService = serviceProvider.GetService(typeof(IKStudioService)) as IKStudioService;
            }
        }

        protected override FrameworkElement CreateViewContent()
        {
            DebugHelper.AssertUIThread();

            this.viewContent = new PlaybackableStreamsViewContent(this.kstudioService)
                {
                    IsSidebarExpanded = this.sideBarExpanded, 
                };

            return this.viewContent;
        }

        public override void LoadViewState(XElement state)
        {
            DebugHelper.AssertUIThread();

            if (state != null)
            {
                this.sideBarExpanded = XmlExtensions.GetAttribute(state, "sidebarExpanded", this.sideBarExpanded);
            }
        }

        public override XElement GetViewState()
        {
            if (this.viewContent != null)
            {
                this.sideBarExpanded = this.viewContent.IsSidebarExpanded;
            }

            XElement element = new XElement("Settings");
            element.SetAttributeValue("sidebarExpanded", this.sideBarExpanded.ToString(CultureInfo.InvariantCulture));

            return element;
        }

        public static View CreateView(IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            return new PlaybackableStreamsView(serviceProvider);
        }

        private IKStudioService kstudioService = null;
        private PlaybackableStreamsViewContent viewContent = null;
        private bool sideBarExpanded = false;
    }
}
