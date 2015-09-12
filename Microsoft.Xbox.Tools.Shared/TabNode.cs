//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;
using System.Windows;

namespace Microsoft.Xbox.Tools.Shared
{
    public class TabNode
    {
        public TabNode Parent { get; set; }

        public Slot Slot { get; set; }

        // This are set if this node is split
        public List<TabNode> Children { get; set; }

        // This is set if the node is a leaf
        public ActivatableTabControl TabControl { get; set; }

        public Rect GetScreenRect()
        {
            var upperLeft = GetUpperLeftPoint();
            return new Rect(upperLeft, Slot.ActualSize);
        }

        private Point GetUpperLeftPoint()
        {
            if (this.Children != null)
            {
                return this.Children[0].GetUpperLeftPoint();
            }

            return this.TabControl.PointToScreenIndependent(new Point(0, 0));
        }

        private void FindLeaves(List<TabNode> leaves)
        {
            if (this.Children != null)
            {
                foreach (var child in this.Children)
                {
                    child.FindLeaves(leaves);
                }
            }
            else
            {
                leaves.Add(this);
            }
        }

        public IEnumerable<TabNode> LeafNodes
        {
            get
            {
                var list = new List<TabNode>();
                FindLeaves(list);
                return list;
            }
        }
    }
}
