//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Xbox.Tools.Shared.Text;
using System;

namespace Microsoft.Xbox.Tools.Shared
{
    public interface ILoggingService
    {
        string AccumulatedLog { get; }
        TextBuffer Buffer { get; }
        void LogLine(string format, params object[] args);
        void LogException(Exception ex);
        event EventHandler<LogEventArgs> MessageLogged;
    }

    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(string message)
        {
            this.Message = message;
        }

        public string Message { get; private set; }
    }
}
