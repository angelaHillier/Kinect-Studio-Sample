//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DocumentFactoryAttribute : Attribute
    {
        public DocumentFactoryAttribute(string factoryName)
        {
            this.FactoryName = factoryName;
        }

        public string FactoryName { get; private set; }
    }
}
