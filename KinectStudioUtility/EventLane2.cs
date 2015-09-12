//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Controls;
    using System.Windows;
    using Microsoft.Xbox.Tools.Shared;
    using Microsoft.Kinect.Tools;
    using System.Windows.Input;
    using System.Windows.Threading;

    public class EventLane2 : EventLane
    {
        public EventLane2(ulong startTimeTicks, ulong endTimeTicks)
        {
            this.startTime = startTimeTicks;
            this.endTime = endTimeTicks;
        }

        public override ulong TimeEnd
        {
            get
            {
                return this.endTime;
            }
        }

        public override ulong TimeStart
        {
            get
            {
                return this.startTime;
            }
        }

        protected override bool DoZoomBeforeLoad
        {
            get
            {
                return false;
            }
        }

        private ulong startTime;
        private ulong endTime;
    }
}
