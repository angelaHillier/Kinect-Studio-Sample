//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public struct TextFormatInfo
    {
        public TextRange Range { get; set; }
        public Brush Foreground { get; set; }
        public FormatFlags Flags { get; set; }
    }

    [Flags]
    public enum FormatFlags
    {
        Normal = 0x00,
        Bold = 0x01,
        Italic = 0x02,
        Underline = 0x04
    }
}
