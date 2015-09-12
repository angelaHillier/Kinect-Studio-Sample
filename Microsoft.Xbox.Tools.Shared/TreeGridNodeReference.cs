//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.Xbox.Tools.Shared
{
    public abstract class TreeGridNodeReference : IDisposable
    {
        public abstract object Item { get; }
        public abstract int ExpansionLevel { get; }
        public abstract bool TryGetFlatIndex(out int flatIndex);
        public abstract bool IsValid { get; }
        public abstract bool IsExpanded { get; set; }
        public abstract bool IsExpandable { get; }
        public abstract bool IsSelected { get; }
        public abstract void Dispose();
        public abstract void ExpandParents();
        public abstract void ExpandChildren();
        public abstract void CollapseChildren();
        public abstract bool MoveToNextFlatNode();
        public abstract bool MoveToPreviousFlatNode();
        public abstract bool MoveToChildItemNode(object item);
        public abstract bool MoveToParentNode();
        public abstract bool MoveToFirstCollapsePoint();
        public abstract void ScrollIntoView();
        public abstract void Select(bool clearExisting, bool setAsCurrent);
        public abstract TreeGridNodeReference Clone();
    }
}
