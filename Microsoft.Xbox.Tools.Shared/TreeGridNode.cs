//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared
{
    abstract class TreeGridNode
    {
        static object[] emptyItems = new object[0];

        int childIndex;                                 // This is the index of this node in its parent's items collection.  -1 indicates that this node has been invalidated.
        IList items;                                    // These are the child data elements of this node.  Their children are not included.  These are data objects, not nodes.
        SortedDictionary<int, TreeGridNode> childNodes; // The active child nodes at this level.  Null until first child is created.
        int flatCount;                                  // This count includes all expansion.  Meaning flatCount = items.Count + childNodes.Values.Sum(c => c.flatCount).  DOES NOT INCLUDE THIS NODE.
        short externalReferences;                       // This node's external reference count (number of TreeGridNodeReference objects that refer to it)
        short level;                                    // This is the nodes level (root node level is 0)
        NodeFlags flags;                                // Flags

        private TreeGridNode()
        {
        }

        public static TreeGridNode CreateRootNode(TreeGrid owner)
        {
            // Note that the root node is always expanded...
            return new RootTreeGridNode(owner) { childIndex = 0, IsExpanded = true, level = 0 };
        }

        protected abstract TreeGridNode Parent { get; }

        protected virtual TreeGrid OwnerGrid
        {
            get
            {
                if (this.Parent != null)
                {
                    return this.Parent.OwnerGrid;
                }

                Debug.Fail("A node is not parented by a RootTreeGridNode!");
                return null;
            }
        }

        public int FlatCount { get { return flatCount; } }

        bool IsExpanded
        {
            get { return this.flags.HasFlag(NodeFlags.IsExpanded); }
            set
            {
                if (value)
                {
                    this.flags |= NodeFlags.IsExpanded;
                }
                else
                {
                    this.flags &= ~NodeFlags.IsExpanded;
                }
            }
        }

        bool IsSuppressingChildCountNotification
        {
            get { return this.flags.HasFlag(NodeFlags.SuppressChildCountNotification); }
            set
            {
                if (value)
                {
                    this.flags |= NodeFlags.SuppressChildCountNotification;
                }
                else
                {
                    this.flags &= ~NodeFlags.SuppressChildCountNotification;
                }
            }
        }

        bool TryGetFlatIndex(out int flatIndex)
        {
            if (this.Parent == null)
            {
                // The flat index of the root node is always -1 (because its first child is always flat index 0)
                flatIndex = -1;
                return true;
            }

            int parentFlatIndex;
            bool success = this.Parent.TryGetFlatIndex(out parentFlatIndex);

            flatIndex = parentFlatIndex + this.Parent.GetChildFlatIndexOffset(this.childIndex);

            // Only return true if the entire heritage of this node is expanded (otherwise the flat index is a lie,
            // but it would be accurate if the necessary nodes were expanded.
            return success && this.Parent.IsExpanded;
        }

        int GetChildFlatIndexOffset(int childIndex)
        {
            // The flat index offset is the number of flat nodes (rows) below this node that the specified child node falls.
            // The absolute flat index of a node can be computed by adding its parent's FlatIndex and its own FlatIndexOffset.
            // The first child's flat index offset is always 1 (because it always appears directly below its parent).
            // For any other child index, its flat index offset is the sum of:
            //  1) Its child index plus one (1 per node including itself)
            //  2) The FlatCounts of all expanded nodes that appear before it
            // ...so, in a tree with child 0 expanded with 10 children, the flat index offset of child 1 is 2 + 10 = 12.
            int offset = childIndex + 1;

            if (this.childNodes != null)
            {
                foreach (var kvp in this.childNodes)
                {
                    if (kvp.Key < childIndex && kvp.Value.IsExpanded)
                    {
                        offset += kvp.Value.FlatCount;
                    }
                }
            }

            return offset;
        }

        TreeGridNode GetOrCreateChildNode(int index)
        {
            TreeGridNode child;

            if (this.childNodes == null)
            {
                this.childNodes = new SortedDictionary<int, TreeGridNode>();
            }

            if (!this.childNodes.TryGetValue(index, out child))
            {
                if (index < 0 || index >= this.items.Count)
                {
                    Debug.Fail("What...?");
                }

                child = new ChildTreeGridNode(this) { childIndex = index, level = (short)(this.level + 1) };
                this.childNodes.Add(index, child);
            }

            return child;
        }

        void AddExternalReference()
        {
            this.externalReferences += 1;
        }

        void ReleaseExternalReference()
        {
            this.externalReferences -= 1;
            if (this.externalReferences == 0 && (this.childNodes == null || this.childNodes.Count == 0) && !this.IsExpanded)
            {
                if (this.Parent != null)
                {
                    this.Parent.OnChildFinalRelease(this);
                }
            }
        }

        void EnsureItems()
        {
            if (this.items == null)
            {
                TreeGrid grid = this.OwnerGrid;
                IEnumerable rawItems = null;

                if (this.Parent != null)
                {
                    rawItems = grid.ChildrenFunc(this.Parent.items[this.childIndex]);
                }

                if (rawItems == null)
                {
                    rawItems = emptyItems;
                }

                this.items = rawItems as IList;
                if (this.items == null)
                {
                    // If we don't get an indexable collection as our items, then we must create one.
                    this.items = rawItems.OfType<object>().ToArray();
                }

                ObserveItems();
                this.flatCount = this.items.Count;
            }
        }

        void RefreshItems(IEnumerable newItems)
        {
            // Should only be called if we already have items (otherwise you've wasted time/memory
            // getting the new collection, since we don't need them yet).
            Debug.Assert(this.items != null, "Don't refresh a node's items if it hasn't even acquired them yet!");

            int oldFlatCount = this.flatCount;

            // The idea here is to replace the items collection with the new one, but keep expansion
            // state for items that exist in both the new and old collections.  The order can change, too.
            if (newItems == null)
            {
                newItems = emptyItems;
            }

            var itemsList = newItems as IList;
            if (itemsList == null)
            {
                // Must have an indexable collection...
                itemsList = newItems.OfType<object>().ToArray();
            }

            // An initial optimization is to check if we have no child nodes, in which case we only need to replace
            // our items collection and update our count.
            if (this.childNodes == null || this.childNodes.Count == 0)
            {
                this.items = itemsList;
                this.flatCount = itemsList.Count;
                NotifyParentChildCountChanged(this, oldFlatCount);
                return;
            }

            // We need to find each child node's item in the new collection.  If it's there,
            // we need to preserve it (and recurse), as well as re-insert it in the new child node table under its
            // (potentially) new index.
            // We do this by first creating a dictionary out of the existing child nodes' values, and then iterating 
            // through the *new* items, looking up each item in that dictionary.  If found, move it to a new child
            // node table (and remove from the dictionary).  When done, any remaining nodes in the dictionary are
            // no longer valid and need to be discarded.
            var existingChildren = this.childNodes.Values.ToDictionary(c => this.items[c.childIndex]);
            var newChildNodes = new SortedDictionary<int, TreeGridNode>();

            for (int i = 0; i < itemsList.Count; i++)
            {
                var newItem = itemsList[i];
                TreeGridNode existingChild;

                if (existingChildren.TryGetValue(newItem, out existingChild))
                {
                    newChildNodes.Add(i, existingChild);
                    existingChild.childIndex = i;
                    existingChildren.Remove(newItem);
                }
            }

            // All still-present nodes are accounted for in newChildNodes.  Need to recurse on them
            // and have them update their child items as appropriate.  Note that we suppress parent
            // notification of child count changes, because we recalc our flat count here when we're done.
            this.IsSuppressingChildCountNotification = true;
            foreach (var childNode in newChildNodes.Values)
            {
                if (childNode.items != null)
                {
                    childNode.RefreshItems(this.OwnerGrid.ChildrenFunc(itemsList[childNode.childIndex]));
                }
            }
            this.IsSuppressingChildCountNotification = false;

            // The remaining nodes in existingChildren are now dead.
            foreach (var deadChild in existingChildren.Values)
            {
                deadChild.Invalidate(this.OwnerGrid);
            }

            this.items = itemsList;
            this.childNodes = newChildNodes;
            this.flatCount = this.items.Count + this.childNodes.Values.Sum(c => c.IsExpanded ? c.FlatCount : 0);
            NotifyParentChildCountChanged(this, oldFlatCount);
        }

        public TreeGridNodeReference CreateNodeReferenceForChildItem(object item)
        {
            EnsureItems();

            int index = this.items.IndexOf(item);

            if (index == -1)
            {
                return null;
            }

            return new Reference(GetOrCreateChildNode(index));
        }

        public TreeGridNodeReference CreateNodeReference(int flatIndex)
        {
            int skippedExpandedChildren = 0;

            EnsureItems();

            if (this.childNodes != null)
            {
                foreach (var kvp in this.childNodes.Where(p => p.Value.IsExpanded))
                {
                    var expandedNode = kvp.Value;
                    var expandedNodeFlatIndex = expandedNode.childIndex + skippedExpandedChildren;

                    if (flatIndex <= expandedNodeFlatIndex)
                    {
                        // We passed it.  It's one of our direct children.  Account for the expanded children we've skipped so far.
                        return new Reference(GetOrCreateChildNode(flatIndex - skippedExpandedChildren));
                    }

                    if (flatIndex <= expandedNodeFlatIndex + expandedNode.flatCount)
                    {
                        // The node we're looking for is within this expanded child.  Because expandedNodeFlatIndex
                        // is the index of the expanded node, its first child is expandedNodeFlatIndex + 1.  Convert flatIndex to
                        // be zero-based relative to the expanded node and recurse.
                        return expandedNode.CreateNodeReference(flatIndex - (expandedNodeFlatIndex + 1));
                    }

                    // It's somewhere beyond this expanded child node.  Keep track of the (flat) number of children we've skipped
                    // and keep going.
                    skippedExpandedChildren += expandedNode.flatCount;
                }
            }

            // No more expanded nodes, so unless the index is past our end, the node is one of ours beyond the last (if any)
            // expanded node.
            if (flatIndex - skippedExpandedChildren < this.items.Count)
            {
                return new Reference(GetOrCreateChildNode(flatIndex - skippedExpandedChildren));
            }

            // If we get here, it means the index is past the end.  Should only happen at the top level.
            if (this.level != 0)
            {
                Debug.Assert(this.level == 0, "Incorrect index calculation in CreateReference!");
            }
            return null;
        }

        public void SaveExpansionState(HashSet<object> expandedItems)
        {
            if (this.childNodes != null)
            {
                foreach (var kvp in this.childNodes)
                {
                    if (kvp.Value.IsExpanded && kvp.Key < this.items.Count)
                    {
                        expandedItems.Add(this.items[kvp.Key]);
                        kvp.Value.SaveExpansionState(expandedItems);
                    }
                }
            }
        }

        public void RestoreExpansionState(HashSet<object> expandedItems)
        {
            this.EnsureItems();

            for (int i = 0; i < this.items.Count; i++)
            {
                if (expandedItems.Contains(this.items[i]) && this.OwnerGrid.HasChildrenFunc(this.items[i]))
                {
                    var childNode = this.GetOrCreateChildNode(i);

                    if (!childNode.IsExpanded)
                    {
                        ToggleChildExpansion(childNode);
                    }

                    childNode.RestoreExpansionState(expandedItems);
                }
            }
        }

        void ObserveItems()
        {
            var incc = this.items as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged += OnItemsCollectionChanged;
            }
        }

        void StopObservingItems()
        {
            var incc = this.items as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged -= OnItemsCollectionChanged;
            }
        }

        void Invalidate(TreeGrid owner)
        {
            InvalidateChildren(owner);
            StopObservingItems();
            this.childIndex = -1;
            owner.OnNodeInvalidated(new Reference(this));
        }

        void InvalidateChildren(TreeGrid owner)
        {
            if (this.childNodes != null)
            {
                foreach (var child in this.childNodes.Values)
                {
                    child.Invalidate(owner);
                }
            }

            this.childNodes = null;
        }

        public virtual void SetItems(IEnumerable rawItems)
        {
            // If you get here, you're doing the wrong thing.  SetItems should only be called on the root node.
            throw new InvalidOperationException();
        }

        public void ExpandChildren()
        {
            this.EnsureItems();

            for (int i = 0; i < this.items.Count; i++)
            {
                if (this.OwnerGrid.HasChildrenFunc(this.items[i]))
                {
                    var childNode = this.GetOrCreateChildNode(i);

                    if (!childNode.IsExpanded)
                    {
                        ToggleChildExpansion(childNode);
                    }
                    childNode.ExpandChildren();
                }
            }
        }

        public void CollapseChildren()
        {
            if (this.childNodes != null)
            {
                var childrenToCollapse = this.childNodes.Values.ToArray();
                foreach (var childNode in childrenToCollapse)
                {
                    childNode.CollapseChildren();
                    if (childNode.IsExpanded)
                    {
                        ToggleChildExpansion(childNode);
                    }
                }
            }
        }

        void ToggleChildExpansion(TreeGridNode childNode)
        {
            int oldFlatCount = this.flatCount;

            Debug.Assert(childNode.Parent == this, "Expanding a child node that isn't a child?");

            if (childNode.IsExpanded)
            {
                this.flatCount -= childNode.flatCount;
                childNode.IsExpanded = false;
            }
            else
            {
                childNode.EnsureItems();
                this.flatCount += childNode.flatCount;
                childNode.IsExpanded = true;
            }

            if (this.IsExpanded)
            {
                NotifyParentChildCountChanged(this, oldFlatCount);
            }

            if (!childNode.IsExpanded)
            {
                // Collapsing a node may be its final reference release
                if ((childNode.childNodes == null || childNode.childNodes.Count == 0) && childNode.externalReferences == 0)
                {
                    OnChildFinalRelease(childNode);
                }
            }
        }

        protected virtual void NotifyParentChildCountChanged(TreeGridNode child, int oldCount)
        {
            if (!this.IsSuppressingChildCountNotification)
            {
                // If this.Parent is null, then there's a parenting problem -- the root node overrides this method.
                this.Parent.OnChildCountChanged(child, oldCount);
            }
        }

        void OnChildCountChanged(TreeGridNode child, int oldCount)
        {
            int ourOldCount = this.flatCount;

            this.flatCount = this.flatCount - oldCount + child.flatCount;

            if (this.IsExpanded)
            {
                NotifyParentChildCountChanged(this, ourOldCount);
            }
        }

        void OnChildFinalRelease(TreeGridNode child)
        {
            if (this.childNodes != null && this.childNodes.ContainsKey(child.childIndex))
            {
                this.childNodes.Remove(child.childIndex);
                if (this.childNodes.Count == 0 && this.externalReferences == 0 && !this.IsExpanded)
                {
                    if (this.Parent != null)
                    {
                        this.Parent.OnChildFinalRelease(this);
                    }
                }
            }
            else
            {
                Debug.Fail("Final release called; child not found in parent.");
            }
        }

        void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

#if DEBUG_TREE
        void VerifyRoot()
        {
            var root = this;

            while (root.Parent != null)
                root = root.Parent;

            root.Verify();
        }

        void Verify()
        {
            StringBuilder sb = null;
            Action<string> append = (s) => { if (sb == null) { sb = new StringBuilder(); } sb.AppendLine(s); };
            Action<bool, string> assert = (condition, text) => { if (!condition) append(text); };
            int computedFlatCount = 0;

            assert(this.childIndex != -1, "Verifying an invalidated node");

            if (this.Parent != null && this.childIndex != -1)
            {
                assert(this is ChildTreeGridNode, "Non-null Parent for root node");
                assert(this.level == this.Parent.level + 1, "Level incorrect (based on Parent.level)");
                assert(this.Parent.items != null, "Parent doesn't have items(!)");
                assert(this.childIndex >= 0 && this.childIndex < this.Parent.items.Count, "Child index out of bounds");
                assert(!this.IsSuppressingChildCountNotification, "Child count notification is suppressed");

                if (this.items != null)
                {
                    computedFlatCount = this.items.Count;

                    if (this.childNodes != null)
                    {
                        foreach (var kvp in this.childNodes.Where(p => p.Value.IsExpanded))
                        {
                            computedFlatCount += kvp.Value.flatCount;
                        }
                    }

                    assert(this.flatCount == computedFlatCount, "Flat count incorrect");
                }
            }

            Debug.Assert(sb == null, sb == null ? null : sb.ToString());

            if (this.childNodes != null)
            {
                foreach (var child in this.childNodes.Values)
                {
                    child.Verify();
                }
            }
        }
#endif

        class Reference : TreeGridNodeReference
        {
            TreeGridNode node;

            public Reference(TreeGridNode node)
            {
                this.node = node;
                Debug.Assert(node != null, "You cannot create a reference to a null node.");
                Debug.Assert(this.node.Parent != null, "You cannot create a reference to the root node.");
                this.node.AddExternalReference();
            }

            public override object Item { get { return this.node == null || this.node.Parent == null ? null : this.node.Parent.items[node.childIndex]; } }

            public override int ExpansionLevel { get { return this.node == null ? -1 : this.node.level; } }

            public override bool IsValid
            {
                get { return this.node != null && this.node.childIndex != -1; }
            }

            public override bool IsExpanded
            {
                get
                {
                    return this.node != null ? this.node.IsExpanded : false;
                }
                set
                {
                    if (this.node != null && this.node.Parent != null && this.node.IsExpanded != value)
                    {
                        this.node.Parent.ToggleChildExpansion(this.node);
                    }
                }
            }

            public override bool IsExpandable
            {
                get
                {
                    if (this.node == null || this.node.Parent == null)
                    {
                        return false;
                    }

                    return this.node.OwnerGrid.HasChildrenFunc(this.node.Parent.items[this.node.childIndex]);
                }
            }

            public override bool IsSelected
            {
                get
                {
                    if (this.node == null)
                    {
                        return false;
                    }

                    return this.node.OwnerGrid.IsNodeSelected(this);
                }
            }

            public override bool TryGetFlatIndex(out int flatIndex)
            {
                flatIndex = -1;

                if (this.node == null)
                    return false;

                return this.node.TryGetFlatIndex(out flatIndex);
            }

            public override void ExpandParents()
            {
                if (this.node == null)
                {
                    return;
                }

                for (var node = this.node.Parent; node != null && node.Parent != null; node = node.Parent)
                {
                    if (!node.IsExpanded)
                    {
                        node.Parent.ToggleChildExpansion(node);
                    }
                }
            }

            public override void ExpandChildren()
            {
                if (this.node == null)
                {
                    return;
                }

                this.node.ExpandChildren();
            }

            public override void CollapseChildren()
            {
                if (this.node == null)
                {
                    return;
                }

                this.node.CollapseChildren();
            }

            public override bool MoveToNextFlatNode()
            {
                if (this.node == null)
                {
                    return false;
                }

                TreeGridNode nextNode = this.node;

                if (nextNode.IsExpanded && nextNode.items.Count > 0)
                {
                    // We're on a node that is expanded.  Move to its first child.  
                    nextNode = nextNode.GetOrCreateChildNode(0);
                }
                else
                {
                    while (nextNode.Parent == null || nextNode.childIndex == nextNode.Parent.items.Count - 1)
                    {
                        // Reached the end of this node.  Pop up a level.  If we're already at the top, we're done.
                        if (nextNode.Parent == null)
                        {
                            return false;
                        }

                        nextNode = nextNode.Parent;
                    }

                    nextNode = nextNode.Parent.GetOrCreateChildNode(nextNode.childIndex + 1);
                }

                nextNode.AddExternalReference();
                this.node.ReleaseExternalReference();
                this.node = nextNode;
                return true;
            }

            public override bool MoveToPreviousFlatNode()
            {
                if (this.node == null)
                {
                    return false;
                }

                TreeGridNode nextNode = this.node;

                if (this.node.childIndex == 0)
                {
                    // We're at the top of this list of children.  Pop up to the parent if we have one.
                    if (this.node.Parent is RootTreeGridNode)
                    {
                        return false;
                    }

                    nextNode = this.node.Parent;
                }
                else
                {
                    nextNode = this.node.Parent.GetOrCreateChildNode(this.node.childIndex - 1);
                    nextNode = FindLastNode(nextNode);
                }

                nextNode.AddExternalReference();
                this.node.ReleaseExternalReference();
                this.node = nextNode;
                return true;
            }

            public override bool MoveToChildItemNode(object item)
            {
                if (this.node == null)
                {
                    return false;
                }

                this.node.EnsureItems();

                int index = this.node.items.IndexOf(item);

                if (index == -1)
                {
                    return false;
                }

                var nextNode = this.node.GetOrCreateChildNode(index);
                nextNode.AddExternalReference();
                this.node.ReleaseExternalReference();
                this.node = nextNode;
                return true;
            }

            public override bool MoveToParentNode()
            {
                if (this.node == null)
                {
                    return false;
                }

                TreeGridNode parentNode = this.node.Parent;

                // Can't move up to root node...
                if (parentNode is ChildTreeGridNode)
                {
                    parentNode.AddExternalReference();
                    this.node.ReleaseExternalReference();
                    this.node = parentNode;
                    return true;
                }

                return false;
            }

            public override bool MoveToFirstCollapsePoint()
            {
                if (this.node == null)
                {
                    return false;
                }

                TreeGridNode collapsePoint = FindFirstCollapsePoint(this.node);

                if (collapsePoint == this.node || collapsePoint == null)
                {
                    // Didn't move
                    return false;
                }

                collapsePoint.AddExternalReference();
                this.node.ReleaseExternalReference();
                this.node = collapsePoint;
                return true;
            }

            public override void Select(bool clearExisting, bool setAsCurrent)
            {
                if (this.node == null)
                {
                    return;
                }

                if (setAsCurrent)
                {
                    this.node.OwnerGrid.SetCurrentNode(this);
                }

                this.node.OwnerGrid.SelectNode(this, clearExisting ? TreeGrid.SelectionOperation.SelectOne : TreeGrid.SelectionOperation.Add);
            }

            public override void ScrollIntoView()
            {
                if (this.node == null)
                {
                    return;
                }

                this.node.OwnerGrid.ScrollNodeIntoView(this);
            }

            static TreeGridNode FindFirstCollapsePoint(TreeGridNode node)
            {
                TreeGridNode parentCollapsePoint = null;

                if (node.level > 1)
                {
                    Debug.Assert(node.Parent != null, "Node level is wrong in FindFirstCollapsePoint!");
                    parentCollapsePoint = FindFirstCollapsePoint(node.Parent);
                }

                if (node.IsExpanded || (parentCollapsePoint != null))
                {
                    // We're expanded, or our parent has a collapse point.  If we're expanded
                    // then it's okay if our parent doesn't have a collapsed parent (since we 
                    // don't either in that case).
                    return parentCollapsePoint;
                }

                // We're collapsed, and either have no parent, or our parent has no collapse point.
                // We're it.
                return node;
            }

            static TreeGridNode FindLastNode(TreeGridNode node)
            {
                if (node.IsExpanded && node.items != null && node.items.Count > 0)
                {
                    var lastChild = node.GetOrCreateChildNode(node.items.Count - 1);
                    return FindLastNode(lastChild);
                }

                return node;
            }

            public override TreeGridNodeReference Clone()
            {
                return new Reference(this.node);
            }

            public override bool Equals(object obj)
            {
                var that = obj as Reference;
                return that != null && that.node == this.node;
            }

            public override int GetHashCode()
            {
                return this.node == null ? 0 : this.node.GetHashCode();
            }

            public override void Dispose()
            {
                if (this.node != null && this.node.childIndex != -1)
                {
                    this.node.ReleaseExternalReference();
                    this.node = null;
                }
            }
        }

        class ChildTreeGridNode : TreeGridNode
        {
            TreeGridNode parent;

            internal ChildTreeGridNode(TreeGridNode parent)
            {
                this.parent = parent;
            }

            protected override TreeGridNode Parent { get { return this.parent; } }
        }

        class RootTreeGridNode : TreeGridNode
        {
            TreeGrid owner;

            internal RootTreeGridNode(TreeGrid owner)
            {
                this.owner = owner;

                // The root node should always have a non-null items collection.
                this.items = emptyItems;
            }

            protected override TreeGrid OwnerGrid { get { return this.owner; } }
            protected override TreeGridNode Parent { get { return null; } }

            public override void SetItems(IEnumerable rawItems)
            {
                RefreshItems(rawItems);
            }

            protected override void NotifyParentChildCountChanged(TreeGridNode child, int oldCount)
            {
                if (!this.IsSuppressingChildCountNotification)
                {
                    this.owner.OnChildCountChanged(this, oldCount);
                }
            }
        }

        [Flags]
        enum NodeFlags
        {
            IsExpanded = 0x0001,
            SuppressChildCountNotification = 0x0002,
        }
    }

}
