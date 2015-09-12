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
    public class TextChange
    {
        TextChange nextChange;

        public TextData OldTextData { get; set; }
        public TextData NewTextData { get; set; }
        public TextLocation Start { get; set; }
        public TextLocation OldEnd { get; set; }
        public TextLocation NewEnd { get; set; }
        public TextData Replacement { get; set; }

        public TextChange NextChange
        {
            get
            {
                return this.nextChange;
            }
            set
            {
                Debug.Assert(this.nextChange == null, "Can't modify the NextChange property once it is set!");
                this.nextChange = value;

                var handler = this.NextChangeMade;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        public TextLocation ApplyTo(TextLocation location, bool negativeTracking)
        {
            bool updated;
            return ApplyTo(location, negativeTracking, out updated);
        }

        public TextLocation ApplyTo(TextLocation location, bool negativeTracking, out bool updated)
        {
            if ((this.Start > location) || (negativeTracking && (this.Start == location)))
            {
                // This change happened after the location given, so it doesn't move.
                updated = false;
                return location;
            }

            updated = true;
            if (this.OldEnd >= location)
            {
                // The location is within the modified portion of the edit, so the location
                // snaps to the start (negativeTracking) or end (!negativeTracking).
                return negativeTracking ? this.Start : this.NewEnd;
            }

            // The location is fully after the edit, so it moves according to the size of the
            // insertion/deletion.
            if (location.Line == this.OldEnd.Line)
            {
                // The location was on the old end line of the changed text, so both the line
                // and index get updated.
                return new TextLocation(this.NewEnd.Line, location.Index - this.OldEnd.Index + this.NewEnd.Index);
            }

            // The location was on a line not affected by the edit, so only the line changes
            return new TextLocation(location.Line + this.NewEnd.Line - this.OldEnd.Line, location.Index);
        }

        public TextRange ApplyTo(TextRange range)
        {
            bool updated;
            return ApplyTo(range, out updated);
        }

        public TextRange ApplyTo(TextRange range, out bool updated)
        {
            bool startUpdated, endUpdated;
            var start = ApplyTo(range.Start, false, out startUpdated);
            var end = ApplyTo(range.End, true, out endUpdated);

            updated = startUpdated || endUpdated;
            return updated ? new TextRange(start, end) : range;
        }

        public event EventHandler NextChangeMade;
    }
}
