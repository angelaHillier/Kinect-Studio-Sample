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
    public interface IEventDataNode
    {
        ulong StartTime { get;}
        ulong Duration { get; }
        int EventIndex { get; }
        string Name { get; }
        bool Visible { get; }
        uint Color { get; }
        int ZIndex { get; }
        EventRenderStyle Style { get; }
    }

    public enum EventRenderStyle
    {
        /// <summary>
        /// Normal events which nest based on ZIndex
        /// </summary>
        Normal,

        /// <summary>
        /// Events are drawn half as tall as normal events (that have no children).  
        /// </summary>
        HalfHeight,

        /// <summary>
        /// "Normal" markers -- drawn as 3px wide marks as high as sibling events
        /// </summary>
        ParentedMarker,

        /// <summary>
        /// "Universal" markers -- drawn as lines that extrude above and below the event bar regardless of nesting depth
        /// </summary>
        SlicingMarker,
    }

    public class SelectionEventArgs : EventArgs
    {
        public IEventDataNode SelectedEvent;

        public SelectionEventArgs(IEventDataNode node)
        {
            SelectedEvent = node;
        }
    }

    // NOTE:  This interface is deprecated in favor of the new IEventLaneDataSource.
    public interface IEventDataSource
    {
        ulong MinTime { get; }
        ulong MaxTime { get; }
        string ToolTipFormat { get; }
        IEventDataNode SelectedNode { get; }
        IEnumerable<IEventDataNode> Nodes { get; }
        event EventHandler DataChanged;
        void OnSelectionChangedInternal(IEventDataNode node);
        event EventHandler SelectionChangedExternal;
        IEventDataNode NextNode(IEventDataNode node);
        IEventDataNode PreviousNode(IEventDataNode node);
        bool ContiguousEvents { get; }
    }

    public interface IEventDataSelection
    {
        IEnumerable<IEventDataNode> SelectedNodes { get; }
        event EventHandler SelectedNodesChanged;
    }
}
