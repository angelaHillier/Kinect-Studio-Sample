//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared
{
    public interface IDocumentFactory
    {
        string Name { get; }
        string DocumentKind { get; }
        string ColorThemePropertyName { get; }
        int DefaultLayoutVersion { get; }
        Document CreateDocument(DocumentIdentity identity);
        BackgroundRequest CreateOpenRequest(IServiceProvider serviceProvider, Document document, Action<HResult> callback);
        DocumentIdentity CreateDocumentIdentity(string moniker);
        DocumentIdentity CreateNextUntitledDocumentIdentity();
        bool TryGetFileName(DocumentIdentity identity, out string fileName);
    }
}
