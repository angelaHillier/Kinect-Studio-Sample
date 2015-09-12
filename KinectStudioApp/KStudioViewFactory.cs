//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioPlugin;

    [ViewFactory("TargetConnectionView", IsSingleInstancePerLayout=true)]
    [ViewFactory("TargetRecordableStreamsView", IsSingleInstancePerLayout=true)]
    [ViewFactory("PlaybackableStreamsView", IsSingleInstancePerLayout=true)]
    [ViewFactory("TargetMonitorableStreamsView", IsSingleInstancePerLayout=true)]
    [ViewFactory("Image2DPropertyView")]
    [ViewFactory("Image2DVisualizationView")] // Monitor
    [ViewFactory("Image3DVisualizationView")] // Monitor
    [ViewFactory("MetadataView")]
    [ViewFactory("InspectionImage2DVisualizationView")] // Combo Playack/Monitor-when-playing, called this for backcompat with views
    [ViewFactory("InspectionImage3DVisualizationView")] // Combo Playack/Monitor-when-playing, called this for backcompat with views
    public class KStudioViewFactory : IViewFactory
    {
        public object CreateView(string registeredViewName, IServiceProvider serviceProvider)
        {
            object value = null;

            switch (registeredViewName)
            {
                case "TargetConnectionView":
                    value = TargetConnectionView.CreateView(serviceProvider);
                    break;

                case "TargetRecordableStreamsView":
                    value = TargetRecordableStreamsView.CreateView(serviceProvider);
                    break;

                case "PlaybackableStreamsView":
                    value = PlaybackableStreamsView.CreateView(serviceProvider);
                    break;

                case "TargetMonitorableStreamsView":
                    value = TargetMonitorableStreamsView.CreateView(serviceProvider);
                    break;

                case "Image2DVisualizationView":
                    value = Image2DVisualizationView.CreateView(serviceProvider, EventType.Monitor);
                    break;

                case "Image3DVisualizationView":
                    value = Image3DVisualizationView.CreateView(serviceProvider, EventType.Monitor);
                    break;

                case "InspectionImage2DVisualizationView":
                    value = Image2DVisualizationView.CreateView(serviceProvider, EventType.Inspection);
                    break;

                case "InspectionImage3DVisualizationView":
                    value = Image3DVisualizationView.CreateView(serviceProvider, EventType.Inspection);
                    break;

                case "Image2DPropertyView":
                    value = Image2DPropertyView.CreateView(serviceProvider);
                    break;

                case "MetadataView":
                    if (serviceProvider != null)
                    {
                        IMetadataViewService metadataViewService = serviceProvider.GetService(typeof(IMetadataViewService)) as IMetadataViewService;
                        if (metadataViewService != null)
                        {
                            value = metadataViewService.CreateView(serviceProvider);
                        }
                    }
                    break;
            }

            return value;
        }

        public string GetViewDisplayName(string registeredViewName)
        {
            string value = null;

            switch (registeredViewName)
            {
                case "TargetConnectionView":
                    value = Strings.ConnectionView_Title; 
                    break;

                case "TargetRecordableStreamsView":
                    value = Strings.RecordView_Title;
                    break;

                case "PlaybackableStreamsView":
                    value = Strings.PlaybackView_Title;
                    break;

                case "TargetMonitorableStreamsView":
                    value = Strings.MonitorView_Title;
                    break;

                case "Image2DVisualizationView":
                    value = Strings.MonitorImage2DView_Title;
                    break;

                case "InspectionImage2DVisualizationView":
                    value = Strings.PlaybackImage2DView_Title;
                    break;

                case "Image2DPropertyView":
                    value = Strings.Image2DPropertyView_Title;
                    break;

                case "Image3DVisualizationView":
                    value = Strings.MonitorImage3DView_Title;
                    break;

                case "InspectionImage3DVisualizationView":
                    value = Strings.PlaybackImage3DView_Title;
                    break;

                case "MetadataView":
                    value = Strings.MetadataView_Title;
                    break;
            }

            return value;
        }
    }
}
