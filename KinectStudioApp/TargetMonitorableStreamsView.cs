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
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    public class TargetMonitorableStreamsView : View
    {
        public TargetMonitorableStreamsView(IServiceProvider serviceProvider)
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

            this.viewContent = new TargetMonitorableStreamsViewContent(this.kstudioService);

            return this.viewContent;
        }

        public static View CreateView(IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            return new TargetMonitorableStreamsView(serviceProvider);
        }

        private IKStudioService kstudioService = null;
        private TargetMonitorableStreamsViewContent viewContent = null;
    }
}
