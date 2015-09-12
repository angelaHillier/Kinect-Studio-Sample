//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public class TextDataChangedEventArgs : EventArgs
    {
        public TextDataChangedEventArgs(TextChange change)
        {
            this.Change = change;
        }

        public TextChange Change { get; private set; }
    }
}
