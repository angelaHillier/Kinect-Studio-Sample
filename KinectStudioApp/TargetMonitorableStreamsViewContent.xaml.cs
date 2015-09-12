//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System.Windows.Controls;
    using KinectStudioUtility;

    public partial class TargetMonitorableStreamsViewContent : UserControl
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "kstudio")]
        public TargetMonitorableStreamsViewContent(IKStudioService kstudioService)
        {
            DebugHelper.AssertUIThread();

            this.KStudioService = kstudioService;

            this.InitializeComponent();
        }

        public IKStudioService KStudioService { get; private set; }
    }
}
