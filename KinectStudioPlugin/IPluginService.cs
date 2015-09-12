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
    using System.Windows;
    using Microsoft.Kinect.Tools;
    using nui = Microsoft.Xbox.Input.Nui;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using KStudioBridge;

    public interface IAvailableStreams
    {
        string MonitorViewStateTitle { get; }
        string ComboViewStateTitle { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        HashSet<KStudioEventStreamIdentifier> GetAvailableMonitorStreams();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        HashSet<KStudioEventStreamIdentifier> GetAvailableComboStreams();
    }

    public interface IPluginService
    {
        IEnumerable<IPlugin> Plugins { get; }

        viz.Context GetContext(EventType eventType);

        viz.D3DImageContext GetImageContext(EventType eventType);

        nui.Registration GetRegistration(EventType eventType);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ir"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ir")]
        DepthIrEngine DepthIrEngine { get; }

        void Initialize();

        bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId);

        void ClearEvents(EventType eventType);

        void HandleEvent(EventType eventType, KStudioEvent eventObj);

        DataTemplate GetReadOnlyFileMetadataDataTemplate(Type valueType, string keyName);

        DataTemplate GetWritableFileMetadataDataTemplate(Type valueType, string keyName);

        DataTemplate GetReadOnlyStreamMetadataDataTemplate(Type valueType, string keyName, Guid dataTypeId, Guid semanticId);

        DataTemplate GetWritableStreamMetadataDataTemplate(Type valueType, string keyName, Guid dataTypeId, Guid semanticId);

        bool ShowMetadataPlugins(Window owner);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        void Update2DPropertyView(EventType eventType, double x, double y, uint width, uint height);

        void Clear2DPropertyView();
    }
}
