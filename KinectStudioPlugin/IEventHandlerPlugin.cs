//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using Microsoft.Kinect.Tools;

    public enum EventType
    {
        Monitor,
        Inspection,
    };

    public interface IEventHandlerPlugin
    {
        bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId);

        void ClearEvents(EventType eventType);

        void HandleEvent(EventType eventType, KStudioEvent eventObj);
    }
}
