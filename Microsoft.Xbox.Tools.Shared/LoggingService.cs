//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Xbox.Tools.Shared.Text;
using System;
using System.Text;

namespace Microsoft.Xbox.Tools.Shared
{
    public class LoggingService : ILoggingService
    {
        StringBuilder accumulatedLog = new StringBuilder(32768);    // Arbitrary starting capacity

        public LoggingService()
        {
            this.Buffer = new TextBuffer();
        }

        public event EventHandler<LogEventArgs> MessageLogged;

        public string AccumulatedLog { get { return accumulatedLog.ToString(); } }
        public TextBuffer Buffer { get; private set; }

        public void LogLine(string format, params object[] args)
        {
            var time = DateTime.Now;
            string loggedLine = string.Format("{0}:  {1}\r\n", time.ToString("MM/dd/yy HH:mm:ss.ffff"), string.Format(format, args));

            this.accumulatedLog.Append(loggedLine);

            TextPencil pencil;

            if (this.Buffer.TryGetPencil(out pencil))
            {
                using (pencil)
                {
                    var end = pencil.Buffer.TextData.End;

                    pencil.Write(end, end, TextData.FromString(loggedLine));
                }
            }

            var handler = MessageLogged;
            if (handler != null)
            {
                handler(this, new LogEventArgs(loggedLine));
            }
        }

        public void LogException(Exception ex)
        {
            LogLine("{0}: {1}\r\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
        }
    }
}
