//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ServiceFactoryAttribute : Attribute
    {
        public ServiceFactoryAttribute(Type serviceType)
        {
            this.ServiceType = serviceType;
        }

        public Type ServiceType { get; private set; }
    }
}
