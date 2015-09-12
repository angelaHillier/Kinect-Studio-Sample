//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.ComponentModel;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class DocumentIdentity
    {
        // Identifies the document factory for this document
        public string FactoryName { get; set; }

        // Not user-facing -- used by the factory to identify the document (i.e. file name, console address, whatever)
        public string Moniker { get; set; }

        // These properties are all user-facing
        public string Kind { get; set; }
        public string ShortDisplayName { get; set; }
        public string FullDisplayName { get; set; }
        public bool IsUntitled { get; set; }

        public bool Equals(DocumentIdentity other)
        {
            return other != null && StringComparer.OrdinalIgnoreCase.Equals(this.Moniker, other.Moniker);
        }
    }
}
