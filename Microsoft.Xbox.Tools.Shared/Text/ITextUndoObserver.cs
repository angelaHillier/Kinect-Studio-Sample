//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Xbox.Tools.Shared.Text
{
    public interface ITextUndoObserver
    {
        void OnBeforeUndoRedoUnit(TextUndoUnit unit, object instigator, bool isUndo);
        void OnAfterUndoRedoUnit(TextUndoUnit unit, object instigator, bool isUndo);
    }
}
