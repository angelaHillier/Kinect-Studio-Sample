//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    /// <summary>
    /// Represents an "on-screen" line and character index, where the line is the same as a TextLocation
    /// line, but the index is based on average character width, and may have a "virtual space" aspect.  
    /// A TextCoordinate must be converted to a TextLocation, which may require dealing with virtual spaces
    /// as appropriate, before being used to index text in a buffer.
    /// </summary>
    public struct TextCoordinate : IEquatable<TextCoordinate>
    {
        public int Line { get; set; }
        public int Index { get; set; }

        public TextCoordinate(int line, int index)
            : this()
        {
            this.Line = line; 
            this.Index = index;
        }

        public bool Equals(TextCoordinate other)
        {
            return this.Line == other.Line && this.Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            if (obj is TextCoordinate)
            {
                return this.Equals((TextCoordinate)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.Line.GetHashCode() + this.Index.GetHashCode();
        }

        public static bool operator ==(TextCoordinate c1, TextCoordinate c2)
        {
            return c1.Line == c2.Line && c1.Index == c2.Index;
        }

        public static bool operator !=(TextCoordinate c1, TextCoordinate c2)
        {
            return !(c1 == c2);
        }

        public static bool operator <(TextCoordinate c1, TextCoordinate c2)
        {
            return c1.Line < c2.Line || (c1.Line == c2.Line && c1.Index < c2.Index);
        }

        public static bool operator >(TextCoordinate c1, TextCoordinate c2)
        {
            return c1.Line > c2.Line || (c1.Line == c2.Line && c1.Index > c2.Index);
        }

        public static bool operator <=(TextCoordinate c1, TextCoordinate c2)
        {
            return (c1 < c2) || (c1 == c2);
        }

        public static bool operator >=(TextCoordinate c1, TextCoordinate c2)
        {
            return (c1 > c2) || (c1 == c2);
        }
    }
}
