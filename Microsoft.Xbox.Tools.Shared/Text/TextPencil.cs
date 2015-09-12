//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public abstract class TextPencil : IDisposable
    {
        public abstract TextBuffer Buffer { get; }
        public abstract TextChange Change { get; }
        public abstract TextUndoUnit UndoUnit { get; }
        public abstract bool CanWrite(TextLocation start, TextLocation end);
        public abstract bool Write(TextLocation start, TextLocation end, TextData replacement);
        public abstract void Dispose();
    }
}
