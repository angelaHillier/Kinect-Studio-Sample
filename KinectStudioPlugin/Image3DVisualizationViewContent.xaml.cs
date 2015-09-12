//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    public partial class Image3DVisualizationViewContent : UserControl, IDisposable
    {
        public Image3DVisualizationViewContent(IServiceProvider serviceProvider, EventType eventType, VisualizationViewSettings viewSettings, IAvailableStreams availableStreamsGetter)
        {
            DebugHelper.AssertUIThread();

            this.eventType = eventType;
            this.availableStreamsGetter = availableStreamsGetter;

            InitializeComponent();

            this.control = new Image3DVisualizationControl(serviceProvider, eventType, viewSettings, availableStreamsGetter);

            this.ControlHost.Child = this.control; 
        }

        ~Image3DVisualizationViewContent()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        public EventType EventType
        {
            get
            {
                return eventType;
            }
        }

        public string ViewerTypeTitle
        {
            get
            {
                return eventType == EventType.Inspection ? Strings.View_Playback_Label : Strings.View_Monitor_Label;
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                DebugHelper.AssertUIThread();

                if (this.control != null)
                {
                    this.control.Dispose();
                    this.control = null;
                }
            }
        }

        public IAvailableStreams AvailableStreamsGetter
        {
            get
            {
                return this.availableStreamsGetter;
            }
        }

        private void Settings_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.control != null)
            {
                this.control.ShowSettings();
            }
        }

        private void DefaultView_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.control != null)
            {
                this.control.ViewDefault();
            }
        }

        private void FrontView_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.control != null)
            {
                this.control.ViewFront();
            }
        }

        private void LeftView_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.control != null)
            {
                this.control.ViewLeft();
            }
        }

        private void TopView_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.control != null)
            {
                this.control.ViewTop();
            }
        }

        private void ZoomIn_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.control != null)
            {
                this.control.ZoomIn();
            }
        }

        private void ZoomOut_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.control != null)
            {
                this.control.ZoomOut();
            }
        }

        private readonly EventType eventType;
        private readonly IAvailableStreams availableStreamsGetter = null;
        private Image3DVisualizationControl control = null;
    }
}