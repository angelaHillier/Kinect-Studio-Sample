//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.Xbox.Tools.Shared
{
    public interface IActiveDocumentTracker
    {
        Document ActiveDocument { get; }
        DocumentManager DocumentManager { get; }
        IEnumerable<Document> Documents { get; }
        bool IsTrackingMainWindow { get; }
        event EventHandler ActiveDocumentChanged;
        void ActivateDocument(Document document);
    }
}
