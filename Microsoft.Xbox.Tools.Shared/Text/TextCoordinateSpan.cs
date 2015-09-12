//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public struct TextCoordinateSpan
    {
        public TextCoordinate Start { get; set; }
        public TextCoordinate End { get; set; }

        public TextCoordinateSpan(TextCoordinate start, TextCoordinate end)
            : this()
        {
            this.Start = start;
            this.End = end;
        }

        public bool IsEmpty { get { return this.Start == this.End; } }
    }
}
