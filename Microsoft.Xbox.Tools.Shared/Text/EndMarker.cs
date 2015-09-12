//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    // Note that these are used to index into an array, so keep 'em linear/contiguous/zero-based.
    public enum EndMarker
    {
        CRLF = 0,
        CR = 1,
        LF = 2,
        EOB = 3,
    }
}
