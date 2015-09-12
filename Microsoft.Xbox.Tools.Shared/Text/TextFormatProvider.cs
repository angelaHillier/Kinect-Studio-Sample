//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public abstract class TextFormatProvider
    {
        public TextBuffer Buffer { get; private set; }

        protected TextFormatProvider(TextBuffer buffer)
        {
            this.Buffer = buffer;
            buffer.TextDataChanged += OnBufferTextDataChanged;
        }

        public event EventHandler FormatDataChanged;

        protected void RaiseFormatDataChangedEvent()
        {
            var handler = this.FormatDataChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnBufferTextDataChanged(object sender, TextDataChangedEventArgs e)
        {
        }

        public abstract TextFormatInfo[] GetFormatDataForLine(int line, out TextChange change);
    }
}
