//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared
{
    public interface IReportProgress
    {
        event EventHandler<ProgressEventArgs> Progress;
    }

    public sealed class ProgressEventArgs : EventArgs
    {
        public ulong? TotalSize { get; private set; }
        public ulong SizeSoFar { get; private set; }
        public string Message { get; private set; }
        public bool Cancel { get; set; }

        public ProgressEventArgs(ulong sizeSoFar, string message) : this(null, sizeSoFar, message) { }
        public ProgressEventArgs(ulong? totalSize, ulong sizeSoFar, string message)
        {
            Update(totalSize, sizeSoFar, message);
        }

        public void Update(ulong? totalSize, ulong sizeSoFar, string message)
        {
            this.TotalSize = totalSize;
            this.SizeSoFar = sizeSoFar;

            if (message != null)
            {
                this.Message = message;
            }
        }
    }
}
