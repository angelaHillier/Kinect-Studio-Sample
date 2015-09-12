//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Threading;

    public static class DebugHelper
    {
        [Conditional("DEBUG")]
        public static void AssertUIThread()
        {
            Debug.Assert(Application.Current.Dispatcher == Dispatcher.CurrentDispatcher);
        }
    }
}
