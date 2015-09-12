//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Xbox.Tools.Shared
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ViewFactoryAttribute : Attribute
    {
        public ViewFactoryAttribute(string registeredName)
        {
            this.RegisteredName = registeredName;
        }

        public string RegisteredName { get; private set; }

        public bool IsSingleInstance { get; set; }

        public bool IsSingleInstancePerLayout { get; set; }

        public bool IsInternalOnly { get; set; }

        public string DocumentAffinities { get; set; }

        public string DefaultShortcutKey { get; set; }
    }
}
