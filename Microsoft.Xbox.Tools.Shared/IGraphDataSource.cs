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
    public interface IGraphDataNode
    {
        ulong StartTime { get; }
        ulong Duration { get; }
        double Y { get; }
    }

    // Hidden interface (no implementation) so we can QI. This is used for the clock hidden source.
    public interface IHiddenGraphDataSource : IGraphDataSource
    {
    }

    public interface IGraphDataSource
    {
        ulong MinX { get; }
        ulong MaxX { get; }
        double MinY { get; }
        double MaxY { get; }
        string Name { get; }
        IEnumerable<IGraphDataNode> Nodes { get; }
        event EventHandler DataChanged;
    }

    public interface IPausePoint
    {
        ulong StartTime { get; }
        ulong Duration { get; }
    }

    public interface IPausePointSource
    {
        IEnumerable<IPausePoint> PausePoints { get; }
        event EventHandler DataChanged;
    }
}
