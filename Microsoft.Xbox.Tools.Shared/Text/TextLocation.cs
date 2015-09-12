//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public struct TextLocation  : IEquatable<TextLocation>
    {
        public int Line { get; set; }
        public int Index { get; set; }

        public TextLocation(int line, int index)
            : this()
        {
            this.Line = line; 
            this.Index = index;
        }

        public bool Equals(TextLocation other)
        {
            return this.Line == other.Line && this.Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            if (obj is TextLocation)
            {
                return this.Equals((TextLocation)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.Line.GetHashCode() + this.Index.GetHashCode();
        }

        public static bool operator ==(TextLocation c1, TextLocation c2)
        {
            return c1.Line == c2.Line && c1.Index == c2.Index;
        }

        public static bool operator !=(TextLocation c1, TextLocation c2)
        {
            return !(c1 == c2);
        }

        public static bool operator <(TextLocation c1, TextLocation c2)
        {
            return c1.Line < c2.Line || (c1.Line == c2.Line && c1.Index < c2.Index);
        }

        public static bool operator >(TextLocation c1, TextLocation c2)
        {
            return c1.Line > c2.Line || (c1.Line == c2.Line && c1.Index > c2.Index);
        }

        public static bool operator <=(TextLocation c1, TextLocation c2)
        {
            return (c1 < c2) || (c1 == c2);
        }

        public static bool operator >=(TextLocation c1, TextLocation c2)
        {
            return (c1 > c2) || (c1 == c2);
        }
    }
}
