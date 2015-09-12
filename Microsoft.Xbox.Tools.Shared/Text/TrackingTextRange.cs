//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public class TrackingTextRange
    {
        TextChange change;

        public TrackingTextRange(TextBuffer buffer, TextRange range)
        {
            this.Buffer = buffer;
            this.change = buffer.LastChange;
            this.Range = range;
            buffer.TextDataChanged += OnBufferTextDataChanged;
        }

        public TextRange Range { get; private set; }
        public TextBuffer Buffer { get; private set; }

        public event EventHandler RangeChanged;

        void OnBufferTextDataChanged(object sender, TextDataChangedEventArgs e)
        {
            bool updatedAtLeastOnce = false;

            while (this.change.NextChange != null)
            {
                bool updated;

                this.Range = this.change.NextChange.ApplyTo(this.Range, out updated);
                this.change = this.change.NextChange;
                if (updated)
                {
                    updatedAtLeastOnce = true;
                }
            }

            if (updatedAtLeastOnce)
            {
                var handler = this.RangeChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }
    }
}
