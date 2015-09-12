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
    public interface IViewCreationCommand
    {
        string RegisteredName { get; }
        string DisplayName { get; }
        string DisplayNameWithAccessText { get; }
        string ShortcutKey { get; }
        bool IsSingleInstance { get; }
        bool IsSingleInstancePerLayout { get; }
        bool IsInternalOnly { get; }
        IEnumerable<string> DocumentFactoryAffinities { get; }
        View CreateView(IServiceProvider serviceProvider);
    }
}
