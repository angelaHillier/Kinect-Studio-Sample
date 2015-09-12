//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Microsoft.Xbox.Tools.Shared
{
    public interface IEventLaneDataSource
    {
        ulong MinTime { get; }
        ulong MaxTime { get; }

        // This is the render-time data provision workhorse.  Each element of the array given represents
        // a single-pixel-wide column of the event lane.  This method is required to fill the array with
        // event node "hits" at the points in time represented by each element, starting at startTime and
        // increasing by timeStride for each element.  
        void PopulateLaneRenderData(ulong startTime, ulong timeStride, IEventLaneNode[] columns);

        // Find the node at the given time point, which typically maps to the mouse cursor position.
        // The time "stride" is provided to allow determination of whether the mouse is "on" the hit node
        // or not based on pixel width.
        IEventLaneNode FindNode(ulong time, ulong timeStride);

        // These are the nodes at the top level.  
        IEnumerable<IEventLaneNode> Nodes { get; }

        // Node selection happens from the "lane" when the user clicks/keyboards to a new event.
        // OnNodeSelected is called in that case.  Selection is then "driven" by the data source
        // by updating the SelectedNodes collection and firing the SelectedNodesChanged event.
        // It is possible that a single selected node can cause multiple nodes to appear in the
        // SelectedNodes collection.  All SelectedNodes are displayed in the lane as "selected"
        // but there is at most one "primary" selected node which is the "focused" node.
        IEnumerable<IEventLaneNode> SelectedNodes { get; }
        void OnNodeSelected(IEventLaneNode node);
        event EventHandler SelectedNodesChanged;

        // This catch-all event tells the event lane to redraw itself
        event EventHandler RenderInvalidated;

        event EventHandler TimeRangeChanged;
    }

    public interface IEventLaneNode
    {
        ulong StartTime { get; }
        ulong Duration { get; }
        string Name { get; }
        object ToolTip { get; }
        EventRenderStyle Style { get; }
        uint Color { get; }
        bool HasChildren { get; }
        IEventLaneNode Parent { get; }
        IEnumerable<IEventLaneNode> Children { get; }
    }
}
