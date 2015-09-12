//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System.Windows;
    using System.Windows.Controls;
    using KinectStudioUtility;
    using System.Windows.Threading;

    public partial class PlaybackableStreamsViewContent : UserControl
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "kstudio")]
        public PlaybackableStreamsViewContent(IKStudioService kstudioService)
        {
            DebugHelper.AssertUIThread();

            this.KStudioService = kstudioService;

            this.InitializeComponent();
        }

        public IKStudioService KStudioService { get; private set; }

        public bool IsSidebarExpanded
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (bool)this.GetValue(PlaybackableStreamsViewContent.IsSidebarExpandedProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(PlaybackableStreamsViewContent.IsSidebarExpandedProperty, value);
            }
        }

        public readonly static DependencyProperty IsSidebarExpandedProperty = DependencyProperty.Register("IsSidebarExpanded", typeof(bool), typeof(PlaybackableStreamsViewContent), new PropertyMetadata(OnSidebarExpanded));

        private static void OnSidebarExpanded(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PlaybackableStreamsViewContent viewControl = d as PlaybackableStreamsViewContent;

            if ((viewControl != null) && (viewControl.Timeline != null))
            {
                viewControl.Timeline.Nudge();
            }
        }
    }
}
