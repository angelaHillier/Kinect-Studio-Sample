//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Diagnostics;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public class TextLine
    {
        static string[] endMarkerStrings;

        static TextLine()
        {
            Empty = new TextLine(string.Empty, EndMarker.EOB);
            endMarkerStrings = new string[4];
            endMarkerStrings[(int)EndMarker.CR] = "\r";
            endMarkerStrings[(int)EndMarker.LF] = "\n";
            endMarkerStrings[(int)EndMarker.CRLF] = "\r\n";
            endMarkerStrings[(int)EndMarker.EOB] = string.Empty;
        }

        public TextLine(string text, EndMarker endMarker)
        {
            this.Text = text;
            this.EndMarker = endMarker;
        }

        public EndMarker EndMarker { get; private set; }
        public string Text { get; private set; }
        public string TextWithEndMarker { get { return this.Text + EndMarkerText(this.EndMarker); } }
        public int Length { get { return this.Text.Length; } }
        public int LengthWithEndMarker { get { return this.Text.Length + EndMarkerText(this.EndMarker).Length; } }

        public static TextLine Empty { get; private set; }
        public static string EndMarkerText(EndMarker endMarker)
        {
            return endMarkerStrings[(int)endMarker];
        }

        public override string ToString()
        {
            return this.Text + "<" + this.EndMarker + ">";
        }
    }
}
