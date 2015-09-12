//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public struct TextRange
    {
        public TextLocation Start { get; set; }
        public TextLocation End { get; set; }

        public TextRange(TextLocation start, TextLocation end)
            : this()
        {
            this.Start = start;
            this.End = end;
        }

        public TextRange(int startLine, int startIndex, int endLine, int endIndex)
            : this(new TextLocation(startLine, startIndex), new TextLocation(endLine, endIndex))
        {
        }

        public bool IsEmpty { get { return this.Start == this.End; } }
    }
}
