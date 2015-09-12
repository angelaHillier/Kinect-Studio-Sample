//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Xbox.Tools.Shared
{
    using System;

    public sealed class SlotChangedEventArgs : EventArgs
    {
        public SlotChangedEventArgs(bool affectsStructure)
        {
            AffectsStructure = affectsStructure;
        }

        public bool AffectsStructure { get; private set; }
    }
}
