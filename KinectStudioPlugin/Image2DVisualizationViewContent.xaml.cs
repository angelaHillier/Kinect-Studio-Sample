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

    public partial class Image2DVisualizationViewContent : UserControl, IDisposable
    {
        public Image2DVisualizationViewContent(IServiceProvider serviceProvider, EventType eventType, VisualizationViewSettings viewSettings, IAvailableStreams availableStreamsGetter)
        {
            DebugHelper.AssertUIThread();

            this.eventType = eventType;
            this.availableStreamsGetter = availableStreamsGetter;

            InitializeComponent();

            this.control = new Image2DVisualizationControl(serviceProvider, eventType, viewSettings, availableStreamsGetter);
            this.control.ZoomChanged += Control_ZoomChanged;

            this.ControlHost.Child = this.control;
        }

        ~Image2DVisualizationViewContent()
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
                return this.eventType;
            }
        }

        public string ViewerTypeTitle
        {
            get
            {
                return this.eventType == EventType.Inspection ? Strings.View_Playback_Label : Strings.View_Monitor_Label;
            }
        }

        public IAvailableStreams AvailableStreamsGetter
        {
            get
            {
                return this.availableStreamsGetter;
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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            ComboBox comboBox = sender as ComboBox;

            if (comboBox != null)
            {
                ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                if (comboBoxItem != null)
                {
                    if (this.control != null)
                    {
                        string tag = comboBoxItem.Tag as string;
                        if (tag == null)
                        {
                            this.control.IsZoomToFit = true;
                        }
                        else
                        {
                            int zoom;
                            if (int.TryParse(tag, out zoom))
                            {
                                this.control.Zoom = zoom;
                                this.control.IsZoomToFit = false;
                            }
                        }
                    }
                }
            }
        }

        private void Control_ZoomChanged(object sender, EventArgs e)
        {
            string display = Strings.View_ZoomToFit_ComboBoxItem;

            if (this.control != null)
            {
                if (!this.control.IsZoomToFit)
                {
                    display = String.Format(CultureInfo.CurrentCulture, Strings.View_Zoom_ComboBoxItem_Format, (int)this.control.Zoom);
                }
            }

            this.ZoomComboBox.SelectedIndex = -1;

            Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.ZoomComboBox.Text = display;
                }
            ));
        }

        private void Settings_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.control != null)
            {
                this.control.ShowSettings();
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

        private void ZoomToFit_Button_Click(object sender, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (this.control != null)
            {
                this.control.IsZoomToFit = true;
            }
        }

        private readonly EventType eventType;
        private readonly IAvailableStreams availableStreamsGetter = null;
        private Image2DVisualizationControl control = null;
    }
}
