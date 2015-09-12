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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared
{
    [ContentProperty("Items")]
    public class TreeGrid : Control
    {
        static readonly DependencyPropertyKey visibleRowCountPropertyKey = DependencyProperty.RegisterReadOnly(
            "VisibleRowCount", typeof(int), typeof(TreeGrid), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty VisibleRowCountProperty = visibleRowCountPropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey pageSizePropertyKey = DependencyProperty.RegisterReadOnly(
            "PageSize", typeof(int), typeof(TreeGrid), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty PageSizeProperty = pageSizePropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey itemCountPropertyKey = DependencyProperty.RegisterReadOnly(
            "ItemCount", typeof(int), typeof(TreeGrid), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty ItemCountProperty = itemCountPropertyKey.DependencyProperty;

        public static readonly DependencyProperty TopItemIndexProperty = DependencyProperty.Register(
            "TopItemIndex", typeof(int), typeof(TreeGrid), new FrameworkPropertyMetadata(OnTopItemIndexChanged));

        public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register(
            "HorizontalOffset", typeof(double), typeof(TreeGrid));

        static readonly DependencyPropertyKey horizontalScrollRangePropertyKey = DependencyProperty.RegisterReadOnly(
            "HorizontalScrollRange", typeof(double), typeof(TreeGrid), new FrameworkPropertyMetadata(0d));
        public static readonly DependencyProperty HorizontalScrollRangeProperty = horizontalScrollRangePropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey verticalScrollRangePropertyKey = DependencyProperty.RegisterReadOnly(
            "VerticalScrollRange", typeof(int), typeof(TreeGrid), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty VerticalScrollRangeProperty = verticalScrollRangePropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey selectedItemCountPropertyKey = DependencyProperty.RegisterReadOnly(
            "SelectedItemCount", typeof(int), typeof(TreeGrid), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty SelectedItemCountProperty = selectedItemCountPropertyKey.DependencyProperty;

        public static readonly DependencyProperty SelectedForegroundProperty = DependencyProperty.Register(
            "SelectedForeground", typeof(Brush), typeof(TreeGrid), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty SelectedBackgroundProperty = DependencyProperty.Register(
            "SelectedBackground", typeof(Brush), typeof(TreeGrid), new FrameworkPropertyMetadata(Brushes.Gray));

        public static readonly DependencyProperty FocusedSelectedForegroundProperty = DependencyProperty.Register(
            "FocusedSelectedForeground", typeof(Brush), typeof(TreeGrid), new FrameworkPropertyMetadata(Brushes.White));

        public static readonly DependencyProperty FocusedSelectedBackgroundProperty = DependencyProperty.Register(
            "FocusedSelectedBackground", typeof(Brush), typeof(TreeGrid), new FrameworkPropertyMetadata(Brushes.Blue));

        public static readonly RoutedCommand ExpandCommand = new RoutedCommand("Expand", typeof(TreeGrid));
        public static readonly RoutedCommand ExpandFullyCommand = new RoutedCommand("ExpandFully", typeof(TreeGrid));
        public static readonly RoutedCommand ExpandAllCommand = new RoutedCommand("ExpandAll", typeof(TreeGrid));
        public static readonly RoutedCommand CollapseCommand = new RoutedCommand("Collapse", typeof(TreeGrid));
        public static readonly RoutedCommand CollapseFullyCommand = new RoutedCommand("CollapseFully", typeof(TreeGrid));
        public static readonly RoutedCommand CollapseAllCommand = new RoutedCommand("CollapseAll", typeof(TreeGrid));
        public static readonly RoutedCommand SelectAllCommand = new RoutedCommand("SelectAll", typeof(TreeGrid));
        static readonly Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
        const int CachedPageCount = 4;

        ObservableCollection<TreeGridColumn> columns;
        TreeGridRow headerRow;
        List<TreeGridRow> visibleRows;                                  // These are the user-visible rows
        Dictionary<TreeGridNodeReference, TreeGridRow> rowCache;        // This is the row cache.  Rows in here are rendered but off-screen.
        TreeGridRow headOfRowCache;                                     // Rows are kept in a doubly-linked list as well...
        TreeGridRow tailOfRowCache;                                     // ...as a cheap way of recycling them in last-used order
        TreeGridNode rootNode;
        Canvas canvas;
        TreeGridRow currentRow;
        TreeGridNodeReference currentNode;
        Dictionary<TreeGridNodeReference, TreeGridNodeReference> selectedNodeReferences;
        IEnumerable items;
        Func<object, IEnumerable> childrenFunc;
        Func<object, bool> hasChildrenFunc;
        int selectionAnchorIndex;
        bool bottomUpLayout;
        bool rowLayoutRequested;
        bool fullRowLayoutRequired;
        bool ignoreTopIndexChanges;
        bool ignoreItemSupplyPropertyChanges;
        int invalidatedRowLayoutPass;
        int rowLayoutPass;
        SelectionChangeSuppressor changeSuppressor;
        List<object> originalContextMenuItems;

        static TreeGrid()
        {
            SelectAllCommand.InputGestures.Add(new KeyGesture(Key.A, ModifierKeys.Control));
            ExpandCommand.InputGestures.Add(new KeyGesture(Key.Add, ModifierKeys.None, "Plus"));
            ExpandCommand.InputGestures.Add(new KeyGesture(Key.OemPlus));
            ExpandFullyCommand.InputGestures.Add(new KeyGesture(Key.Add, ModifierKeys.Control, "Ctrl+Plus"));
            ExpandFullyCommand.InputGestures.Add(new KeyGesture(Key.OemPlus, ModifierKeys.Control));
            ExpandAllCommand.InputGestures.Add(new KeyGesture(Key.Add, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+Plus"));
            ExpandAllCommand.InputGestures.Add(new KeyGesture(Key.OemPlus, ModifierKeys.Control | ModifierKeys.Shift));
            CollapseCommand.InputGestures.Add(new KeyGesture(Key.Subtract, ModifierKeys.None, "Minus"));
            CollapseCommand.InputGestures.Add(new KeyGesture(Key.OemMinus));
            CollapseFullyCommand.InputGestures.Add(new KeyGesture(Key.Subtract, ModifierKeys.Control, "Ctrl+Minus"));
            CollapseFullyCommand.InputGestures.Add(new KeyGesture(Key.OemMinus, ModifierKeys.Control));
            CollapseAllCommand.InputGestures.Add(new KeyGesture(Key.Subtract, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+Minus"));
            CollapseAllCommand.InputGestures.Add(new KeyGesture(Key.OemMinus, ModifierKeys.Control | ModifierKeys.Shift));
        }

        public TreeGrid()
        {
            this.columns = new ObservableCollection<TreeGridColumn>();
            this.columns.CollectionChanged += OnColumnsChanged;

            // HasChildrenFunc is an optimization opportunity for data sets whose children require computation.
            this.hasChildrenFunc = DefaultHasChildren;
            this.childrenFunc = DefaultChildren;
            this.items = null;

            this.headerRow = new TreeGridRow(this, true);
            Canvas.SetTop(this.headerRow, 0);
            this.visibleRows = new List<TreeGridRow>();
            this.rowCache = new Dictionary<TreeGridNodeReference, TreeGridRow>();
            this.selectedNodeReferences = new Dictionary<TreeGridNodeReference, TreeGridNodeReference>();
            this.PreviewMouseDown += OnPreviewMouseDown;
            this.MouseWheel += OnMouseWheel;
            this.KeyDown += OnKeyDown;
            this.Focusable = true;
            this.selectionAnchorIndex = -1;

            this.rootNode = TreeGridNode.CreateRootNode(this);

            this.AddHandler(TreeGridRow.CurrentItemChangedEvent, (RoutedEventHandler)OnCurrentItemChanged);
            this.AddHandler(TreeGridRow.RowClickedEvent, (EventHandler<RowClickedEventArgs>)OnRowClicked);

            this.CommandBindings.Add(new CommandBinding(ExpandCommand, OnExpandExecuted, OnExpandCanExecute));
            this.CommandBindings.Add(new CommandBinding(ExpandFullyCommand, OnExpandFullyExecuted, OnExpandFullyCanExecute));
            this.CommandBindings.Add(new CommandBinding(ExpandAllCommand, OnExpandAllExecuted));
            this.CommandBindings.Add(new CommandBinding(CollapseCommand, OnCollapseExecuted, OnCollapseCanExecute));
            this.CommandBindings.Add(new CommandBinding(CollapseFullyCommand, OnCollapseFullyExecuted, OnCollapseFullyCanExecute));
            this.CommandBindings.Add(new CommandBinding(CollapseAllCommand, OnCollapseAllExecuted));
            this.CommandBindings.Add(new CommandBinding(SelectAllCommand, OnSelectAllExecuted));

            this.AdditionalContextMenuItems = new ObservableCollection<MenuItem>();
            this.AdditionalContextMenuItems.CollectionChanged += OnAdditionalContextMenuItemsCollectionChanged;
        }

        public ObservableCollection<TreeGridColumn> Columns { get { return columns; } }

        public int VisibleRowCount
        {
            get { return (int)GetValue(VisibleRowCountProperty); }
            private set { SetValue(visibleRowCountPropertyKey, value); }
        }

        public int TopItemIndex
        {
            get { return (int)GetValue(TopItemIndexProperty); }
            set { SetValue(TopItemIndexProperty, value); }
        }

        public int ItemCount
        {
            get { return (int)GetValue(ItemCountProperty); }
            private set { SetValue(itemCountPropertyKey, value); }
        }

        public int PageSize
        {
            get { return (int)GetValue(PageSizeProperty); }
            private set { SetValue(pageSizePropertyKey, value); }
        }

        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        public double HorizontalScrollRange
        {
            get { return (double)GetValue(HorizontalScrollRangeProperty); }
            private set { SetValue(horizontalScrollRangePropertyKey, value); }
        }

        public int VerticalScrollRange
        {
            get { return (int)GetValue(VerticalScrollRangeProperty); }
            private set { SetValue(verticalScrollRangePropertyKey, value); }
        }

        public TreeGridRow HeaderRow { get { return this.headerRow; } }

        public Brush SelectedForeground
        {
            get { return (Brush)GetValue(SelectedForegroundProperty); }
            set { SetValue(SelectedForegroundProperty, value); }
        }

        public Brush SelectedBackground
        {
            get { return (Brush)GetValue(SelectedBackgroundProperty); }
            set { SetValue(SelectedBackgroundProperty, value); }
        }

        public Brush FocusedSelectedForeground
        {
            get { return (Brush)GetValue(FocusedSelectedForegroundProperty); }
            set { SetValue(FocusedSelectedForegroundProperty, value); }
        }

        public Brush FocusedSelectedBackground
        {
            get { return (Brush)GetValue(FocusedSelectedBackgroundProperty); }
            set { SetValue(FocusedSelectedBackgroundProperty, value); }
        }

        public object CurrentItem { get { return this.currentNode != null ? this.currentNode.Item : null; } }

        public int CurrentItemIndex
        {
            get
            {
                if (this.currentNode == null)
                {
                    return -1;
                }

                int index;
                bool success = this.currentNode.TryGetFlatIndex(out index);
                Debug.Assert(success, "Current node index should always be valid.");
                return index;
            }
        }

        public IEnumerable<object> SelectedItems { get { return this.selectedNodeReferences.Values.Select(nr => nr.Item); } }

        public IEnumerable<TreeGridNodeReference> SelectedNodes
        {
            get
            {
                foreach (var node in this.selectedNodeReferences.Values)
                {
                    // NOTE, must dispose these!
                    yield return node.Clone();
                }
            }
        }

        public int SelectedItemCount
        {
            get { return (int)GetValue(SelectedItemCountProperty); }
            private set { SetValue(selectedItemCountPropertyKey, value); }
        }

        public IEnumerable Items
        {
            get { return this.items; }
            set
            {
                this.items = value;
                if (!this.ignoreItemSupplyPropertyChanges)
                {
                    OnItemSupplyChanged(false);
                }
            }
        }

        public Func<object, bool> HasChildrenFunc
        {
            get
            {
                return this.hasChildrenFunc;
            }
            set
            {
                this.hasChildrenFunc = value ?? this.DefaultHasChildren;
                if (!this.ignoreItemSupplyPropertyChanges)
                {
                    OnItemSupplyChanged(false);
                }
            }
        }

        public Func<object, IEnumerable> ChildrenFunc
        {
            get
            {
                return this.childrenFunc;
            }
            set
            {
                this.childrenFunc = value ?? this.DefaultChildren;
                if (!this.ignoreItemSupplyPropertyChanges)
                {
                    OnItemSupplyChanged(false);
                }
            }
        }

        public ObservableCollection<MenuItem> AdditionalContextMenuItems { get; private set; }

        public event EventHandler CurrentItemChanged;
        public event EventHandler SelectionChanged;
        public event EventHandler LayoutComplete;

        bool DefaultHasChildren(object obj)
        {
            if (this.ChildrenFunc != null)
            {
                var children = this.ChildrenFunc(obj);
                if (children != null)
                {
                    return children.GetEnumerator().MoveNext();
                }
            }

            return false;
        }

        IEnumerable DefaultChildren(object obj)
        {
            return null;
        }

        void SledgehammerReset()
        {
            ClearRowCache();
            ClearSelection(false);
            this.currentNode = null;
            if (this.currentRow != null)
            {
                this.currentRow.IsCurrent = false;
            }
            this.currentRow = null;
            this.TopItemIndex = 0;
            this.HorizontalOffset = 0;
            RaiseCurrentItemChanged();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.canvas = this.Template.FindName("PART_Canvas", this) as Canvas;
            this.canvas.SizeChanged += OnCanvasSizeChanged;
            this.canvas.Loaded += OnCanvasLoaded;
            this.canvas.Children.Add(this.headerRow);

            if (this.ContextMenu != null)
            {
                // The template supplied a context menu.  So that we can modify it, we need to make a COPY of
                // it (otherwise the same context menu instance is shared by all tree grids).
                this.originalContextMenuItems = this.ContextMenu.Items.OfType<object>().Select(i => CloneContextMenuItem(i)).ToList();
                this.ContextMenu = null;
            }
            else
            {
                this.originalContextMenuItems = new List<object>();
            }

            // Always rebuild the context menu, which will do nothing if we have no items.
            RebuildContextMenu();
        }

        static DependencyProperty[] clonedMenuItemProperties = 
        { 
            MenuItem.HeaderProperty, 
            MenuItem.CommandProperty, 
            MenuItem.CommandParameterProperty, 
            MenuItem.CommandTargetProperty,
        };

        object CloneContextMenuItem(object obj)
        {
            if (obj is Separator)
            {
                return new Separator();
            }

            var item = obj as MenuItem;

            if (item == null)
            {
                return obj;
            }

            var clone = new MenuItem();

            foreach (var dp in clonedMenuItemProperties)
            {
                var binding = BindingOperations.GetBinding(item, dp);

                if (binding != null)
                {
                    clone.SetBinding(dp, binding);
                }
                else
                {
                    clone.SetValue(dp, item.GetValue(dp));
                }
            }

            if (item.Items.Count > 0)
            {
                foreach (var child in item.Items.OfType<object>())
                {
                    clone.Items.Add(CloneContextMenuItem(child));
                }
            }

            return clone;
        }

        void OnAdditionalContextMenuItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.originalContextMenuItems != null)
            {
                RebuildContextMenu();
            }
        }

        void RebuildContextMenu()
        {
            if (this.ContextMenu == null)
            {
                // Set the data context on the context menu so bindings in XAML-supplied items (which we copied in OnApplyTemplate)
                // will work.
                this.ContextMenu = new ContextMenu { DataContext = this };
            }
            else
            {
                this.ContextMenu.Items.Clear();
            }

            foreach (var item in this.originalContextMenuItems)
            {
                this.ContextMenu.Items.Add(item);
            }

            if (this.ContextMenu.Items.Count > 0 && this.AdditionalContextMenuItems.Count > 0)
            {
                this.ContextMenu.Items.Add(new Separator());
            }

            foreach (var item in this.AdditionalContextMenuItems)
            {
                this.ContextMenu.Items.Add(item);
            }

            if (this.ContextMenu.Items.Count == 0)
            {
                this.ContextMenu = null;
            }
        }

        public void ClearCurrentItem()
        {
            SetCurrentNode(null);
        }

        public TreeGridNodeReference CreateNodeReferenceForTopLevelItem(object item)
        {
            return this.rootNode.CreateNodeReferenceForChildItem(item);
        }

        public TreeGridNodeReference CreateNodeReferenceForItemIndex(int index)
        {
            return this.rootNode.CreateNodeReference(index);
        }

        public void SetItems(IEnumerable items, Func<object, IEnumerable> childrenFunc, Func<object, bool> hasChildrenFunc, bool fullReset)
        {
            this.ignoreItemSupplyPropertyChanges = true;
            try
            {
                this.Items = items;
                this.ChildrenFunc = childrenFunc;
                this.HasChildrenFunc = hasChildrenFunc;
            }
            finally
            {
                this.ignoreItemSupplyPropertyChanges = false;
            }

            OnItemSupplyChanged(fullReset);
        }

        public object GetExpansionState()
        {
            return GetExpansionState(EqualityComparer<object>.Default);
        }

        public object GetExpansionState(IEqualityComparer<object> comparer)
        {
            var expandedItems = new HashSet<object>(comparer);
            this.rootNode.SaveExpansionState(expandedItems);
            return expandedItems;
        }

        public void RestoreExpansionState(object state)
        {
            var expandedItems = state as HashSet<object>;

            if (expandedItems == null)
            {
                Debug.Fail("Expansion state object is not the correct type!");
            }
            else
            {
                this.rootNode.RestoreExpansionState(expandedItems);
            }
        }

        void OnItemSupplyChanged(bool fullReset)
        {
            if (fullReset)
            {
                this.SledgehammerReset();
                this.rootNode.SetItems(null);
            }

            SetCurrentNode(null);
            ClearSelection(false);
            this.rootNode.SetItems(this.Items);
        }

        internal bool IsNodeSelected(TreeGridNodeReference nodeRef)
        {
            return this.selectedNodeReferences.ContainsKey(nodeRef);
        }

        void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            InvalidateRowLayout(false);
        }

        void OnCanvasLoaded(object sender, RoutedEventArgs e)
        {
            InvalidateRowLayout(true);
        }

        void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Focus();
        }

        void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.rootNode != null)
            {
                if (Math.Sign(e.Delta) < 0)
                {
                    this.TopItemIndex = Math.Min(this.TopItemIndex + Math.Max(1, (-e.Delta / 30)), this.rootNode.FlatCount - 1);
                }
                else
                {
                    this.TopItemIndex = Math.Max(this.TopItemIndex - Math.Max(1, (e.Delta / 30)), 0);
                }
            }
        }

        int? GetCurrentNodeIndexOffset()
        {
            int index;

            if (this.currentNode != null && this.currentNode.TryGetFlatIndex(out index))
            {
                return index - this.TopItemIndex;
            }

            return null;
        }

        void RestoreCurrentNodeIndex(int? offset)
        {
            if (offset.HasValue && this.currentNode != null)
            {
                int newIndex;

                if (this.currentNode.TryGetFlatIndex(out newIndex))
                {
                    this.TopItemIndex = newIndex - offset.Value;
                }
            }
        }

        void ExpandSelectedRows(bool expandChildrenToo)
        {
            int? offset = GetCurrentNodeIndexOffset();

            foreach (var nodeRef in this.selectedNodeReferences.Values.Where(nr => nr.IsExpandable))
            {
                nodeRef.IsExpanded = true;
                if (expandChildrenToo)
                {
                    nodeRef.ExpandChildren();
                }
            }

            RestoreCurrentNodeIndex(offset);
        }

        void CollapseSelectedRows(bool collapseChildrenToo)
        {
            int? offset = GetCurrentNodeIndexOffset();

            foreach (var nodeRef in this.selectedNodeReferences.Values.Where(nr => nr.IsExpandable))
            {
                if (collapseChildrenToo)
                {
                    nodeRef.CollapseChildren();
                }
                nodeRef.IsExpanded = false;
            }

            RestoreCurrentNodeIndex(offset);
        }

        void OnExpandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ExpandSelectedRows(false);
            e.Handled = true;
        }

        void OnExpandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.selectedNodeReferences.Count > 0 && this.selectedNodeReferences.Values.Any(nr => nr.IsExpandable && !nr.IsExpanded);
            e.Handled = true;
        }

        void OnExpandFullyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ExpandSelectedRows(true);
            e.Handled = true;
        }

        void OnExpandFullyCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.selectedNodeReferences.Count > 0 && this.selectedNodeReferences.Values.Any(nr => nr.IsExpandable);
            e.Handled = true;
        }

        void OnExpandAllExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int? offset = GetCurrentNodeIndexOffset();

            this.rootNode.ExpandChildren();
            RestoreCurrentNodeIndex(offset);
            e.Handled = true;
        }

        void OnCollapseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CollapseSelectedRows(false);
            e.Handled = true;
        }

        void OnCollapseCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.selectedNodeReferences.Count > 0 && this.selectedNodeReferences.Values.Any(nr => nr.IsExpanded);
            e.Handled = true;
        }

        void OnCollapseFullyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CollapseSelectedRows(true);
            e.Handled = true;
        }

        void OnCollapseFullyCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.selectedNodeReferences.Count > 0 && this.selectedNodeReferences.Values.Any(nr => nr.IsExpandable);
            e.Handled = true;
        }

        void OnCollapseAllExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int? offset = GetCurrentNodeIndexOffset();

            this.rootNode.CollapseChildren();
            RestoreCurrentNodeIndex(offset);
            e.Handled = true;
        }

        void OnSelectAllExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            SelectAll();
            e.Handled = true;
        }

        public object CurrentNode()
        {
            if (this.selectedNodeReferences.Values.Count != 1)
                return null;
            return this.selectedNodeReferences.Values.ElementAt(0).Item;
        }

        void ClearRowCache()
        {
            foreach (var kvp in this.rowCache)
            {
                this.canvas.Children.Remove(kvp.Value);
                kvp.Value.OnRowRemoved();
            }

            this.rowCache.Clear();
            this.visibleRows.Clear();
            this.headOfRowCache = null;
            this.tailOfRowCache = null;
        }

        public void SetCurrentNode(TreeGridNodeReference node)
        {
            if (this.currentNode != null)
            {
                if (this.currentRow != null)
                {
                    this.currentRow.IsCurrent = false;
                    this.currentRow = null;
                }

                this.currentNode.Dispose();
            }

            if (node != null)
            {
                this.currentNode = node.Clone();
                this.currentRow = this.visibleRows.FirstOrDefault(r => r.NodeReference.Equals(this.currentNode));
                if (this.currentRow != null)
                {
                    this.currentRow.IsCurrent = true;
                }
            }
            else
            {
                this.currentNode = null;
            }

            RaiseCurrentItemChanged();
        }

        public void ScrollNodeIntoView(TreeGridNodeReference node)
        {
            DoRowLayoutUntilValid();

            var visibleRow = this.visibleRows.FirstOrDefault(r => r.NodeReference.Equals(node));

            if (visibleRow == null)
            {
                node.ExpandParents();

                int flatIndex;

                if (node.TryGetFlatIndex(out flatIndex))
                {
                    this.TopItemIndex = Math.Max(0, flatIndex - this.visibleRows.Count / 3);
                }
            }
        }

        public void InvalidateRowLayout(bool recalcColumnWidths)
        {
            // Whatever the current layout pass number is, it's now invalid
            this.invalidatedRowLayoutPass = this.rowLayoutPass;

            // If asked to, make sure the requested layout is "full" (which does column width remeasuring)
            if (recalcColumnWidths)
            {
                this.fullRowLayoutRequired = true;
            }

            if (!this.rowLayoutRequested)
            {
                this.rowLayoutRequested = true;
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    // An odd name for this method -- see its comments below for what it really does.
                    DoRowLayoutUntilValid();
                }), DispatcherPriority.Background);
            }
        }

        void DoRowLayoutUntilValid()
        {
            if (this.rowLayoutRequested)
            {
                // Okay, time to do the layout.  Maybe.  It's possible that a layout was
                // done synchronously, after this was posted.  That's why we track the invalidated
                // layout pass... if the current pass is no longer what we invalidated, we don't 
                // need to layout again.  
                // Also, note that it is possible for a row layout pass to invalidate itself.  This
                // happens if the height of a row changes (for example).  So we use a 'while' instead
                // of an 'if'.
                bool layoutDone = false;

                while (this.invalidatedRowLayoutPass == this.rowLayoutPass)
                {
                    if (DoRowLayout())
                    {
                        layoutDone = true;
                    }
                }

                this.rowLayoutRequested = false;
                if (layoutDone)
                {
                    var handler = this.LayoutComplete;
                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
            }
        }

        bool DoRowLayout()
        {
            this.rowLayoutPass += 1;
            this.visibleRows.Clear();

            if (this.canvas == null || !this.canvas.IsLoaded)
            {
                // Do nothing until we're loaded
                return false;
            }

            if (this.fullRowLayoutRequired)
            {
                this.headerRow.Width = double.NaN;
                this.headerRow.Measure(infiniteSize);
                CollectDesiredColumnWidths(this.headerRow);
            }
            else
            {
                this.headerRow.UpdateLayout();
            }

            bool canvasFilled = (this.bottomUpLayout) ? DoBottomUpLayout() : DoTopDownLayout();

            // Bottom-up layout is on-demand only
            this.bottomUpLayout = false;

            if (this.fullRowLayoutRequired)
            {
                double totalWidth = this.columns.Sum(c => c.ActualWidth);
                this.headerRow.Width = totalWidth;

                foreach (var row in this.visibleRows)
                {
                    row.Width = totalWidth;
                }
            }

            // All rows in the cache that weren't used in this layout pass need to be shoved offscreen, but kept on the canvas.
            foreach (var kvp in this.rowCache)
            {
                if (kvp.Value.lastLayoutPassUsed != this.rowLayoutPass)
                {
                    Canvas.SetTop(kvp.Value, this.canvas.ActualHeight + 1);
                }
            }

            // Columns "lock" after a layout pass.  This will have no effect unless the column width has been set to NaN, in which
            // case new column widths will have been computed (by finding the max width of those visible) and this will cause the
            // rows to be updated.
            if (this.fullRowLayoutRequired)
            {
                foreach (var column in this.columns)
                {
                    if (column.IsWidthLocked)
                    {
                        column.Width = column.ActualWidth;
                    }
                }
            }

            // If we didn't fill the canvas completely, guess at the page size using the header row height.
            if (!canvasFilled)
            {
                this.PageSize = (int)(this.canvas.ActualHeight / this.headerRow.DesiredSize.Height);
            }
            else
            {
                this.PageSize = this.visibleRows.Count;
            }

            this.HorizontalScrollRange = Math.Max(0, this.headerRow.Width - this.canvas.ActualWidth);
            this.HorizontalOffset = Math.Min(this.HorizontalOffset, this.HorizontalScrollRange);
            this.VisibleRowCount = this.visibleRows.Count;
            this.fullRowLayoutRequired = false;
            return true;
        }

        bool DoTopDownLayout()
        {
            double top = this.headerRow.DesiredSize.Height;
            int topIndex = this.TopItemIndex;
            var nodeRef = this.rootNode.CreateNodeReference(topIndex);
            bool firstVisible = true;

            while (top < this.canvas.ActualHeight && nodeRef != null)
            {
                TreeGridRow row = GetRowForNode(nodeRef);

                this.visibleRows.Add(row);
                Canvas.SetTop(row, top);
                row.UpdateLayout();
                top += row.DesiredSize.Height;
                row.isPartiallyVisible = !firstVisible && (top > this.canvas.ActualHeight);

                if (!nodeRef.MoveToNextFlatNode())
                {
                    break;
                }

                firstVisible = false;
            }

            if (nodeRef != null)
            {
                nodeRef.Dispose();
                nodeRef = null;
            }

            return top >= this.canvas.ActualHeight;
        }

        bool DoBottomUpLayout()
        {
            // "Bottom up" layout means that the TopLineIndex is intended to actually be the bottom-most fully-visible line.
            // It is used when moving the selection downward in the list.  
            // When this layout pass is complete, the TopLineIndex value is (silently) updated to reflect the actual top line index.
            double top = this.headerRow.DesiredSize.Height;
            int topIndex = this.TopItemIndex;
            var nodeRef = this.rootNode.CreateNodeReference(topIndex);

            if (nodeRef == null || top >= this.canvas.ActualHeight)
            {
                // No rows visible.  Our return still needs to indicate whether we completely filled the canvas.
                return top >= this.canvas.ActualHeight;
            }

            var topNodeRef = nodeRef.Clone();
            double remainingSpace = this.canvas.ActualHeight - top;

            // First, get rows (starting with the top line index and working backwards) until we've "filled" the canvas.  
            // Note that we're not placing the rows just yet (because we're going backwards).
            while (true)
            {
                TreeGridRow row = GetRowForNode(nodeRef);

                row.UpdateLayout();
                remainingSpace -= row.DesiredSize.Height;
                if (remainingSpace > 0 || this.visibleRows.Count == 0)
                {
                    // This row either fits fully, or is TopLineIndex itself (which always goes in).
                    this.visibleRows.Insert(0, row);
                    row.isPartiallyVisible = false;
                }
                else
                {
                    // This row isn't actually used in this layout pass...
                    row.lastLayoutPassUsed -= 1;
                    topIndex += 1;
                }

                if (remainingSpace <= 0 || !nodeRef.MoveToPreviousFlatNode())
                {
                    break;
                }

                topIndex -= 1;
            }

            // Now position the rows.  Note that there may be remaining space at the end (because we might not have used
            // the last row we saw, because it might have been partially visible).
            foreach (var row in this.visibleRows)
            {
                Canvas.SetTop(row, top);
                top += row.DesiredSize.Height;
            }

            // Fill the remaining space (note that because of the possibility of variable row heights, more than one
            // row might fit) starting at ONE PAST the top index row.
            while (top < this.canvas.ActualHeight && topNodeRef.MoveToNextFlatNode())
            {
                var row = GetRowForNode(topNodeRef);
                this.visibleRows.Add(row);
                Canvas.SetTop(row, top);
                row.UpdateLayout();
                top += row.DesiredSize.Height;
                row.isPartiallyVisible = (top > this.canvas.ActualHeight);
            }

            // Update the top line index to be the actual top index now.  Make sure it doesn't trigger another update.
            this.ignoreTopIndexChanges = true;
            this.TopItemIndex = topIndex;
            this.ignoreTopIndexChanges = false;

            nodeRef.Dispose();
            topNodeRef.Dispose();

            return top >= this.canvas.ActualHeight;
        }

        TreeGridRow GetRowForNode(TreeGridNodeReference nodeRef)
        {
            TreeGridRow row;

            if (this.rowCache.TryGetValue(nodeRef, out row))
            {
                // Found in the cache.  That means the row is already in the canvas, and already has
                // the correct data.  It may need its IsCurrent/IsSelected flags set, but other than
                // that we just need to position it vertically.
                Debug.Assert(row.NodeReference.Equals(nodeRef));
                row.IsCurrent = (nodeRef.Equals(this.currentNode));
                row.IsSelected = this.selectedNodeReferences.ContainsKey(nodeRef);
                row.UpdateExpansionState();
                MoveRowToTopOfUsedList(row);

                if (this.fullRowLayoutRequired)
                {
                    row.Width = double.NaN;
                    row.Measure(infiniteSize);
                    CollectDesiredColumnWidths(row);
                }
                else
                {
                    row.Width = this.headerRow.DesiredSize.Width;
                }
            }
            else if ((this.rowCache.Count >= ((this.canvas.ActualHeight / this.headerRow.ActualHeight) * CachedPageCount)) &&
                (this.tailOfRowCache != null) && (this.tailOfRowCache.lastLayoutPassUsed != this.rowLayoutPass))
            {
                // Not in the cache with that row data, but the cache is full and thus has rows available for recycling.  Use the oldest one.
                // it will be in the cache, but keyed under a different node.  We need to remove it using that key (row.NodeReference)
                // and then update the row and add it to the cache with the new node reference key.
                row = this.tailOfRowCache;
                this.rowCache.Remove(row.NodeReference);
                row.UpdateRowData(nodeRef, nodeRef.Equals(this.currentNode), this.selectedNodeReferences.ContainsKey(nodeRef));
                if (this.fullRowLayoutRequired)
                {
                    row.Width = double.NaN;
                    row.Measure(infiniteSize);
                    CollectDesiredColumnWidths(row);
                }
                else
                {
                    row.Width = this.headerRow.DesiredSize.Width;
                    row.Measure(infiniteSize);
                }
                this.rowCache.Add(row.NodeReference, row);
                MoveRowToTopOfUsedList(row);
            }
            else
            {
                // Not in the cache, and we haven't created a full cache of rows yet.  Create a new one now.
                row = new TreeGridRow(this, false);
                this.canvas.Children.Add(row);
                row.UpdateRowData(nodeRef, nodeRef.Equals(this.currentNode), this.selectedNodeReferences.ContainsKey(nodeRef));
                if (this.fullRowLayoutRequired)
                {
                    row.Width = double.NaN;
                    row.UpdateLayout();
                    CollectDesiredColumnWidths(row);
                }
                else
                {
                    row.Width = this.headerRow.DesiredSize.Width;
                    row.UpdateLayout();
                }

                // NOTE:  Very important that we use the row's NodeReference value as the key here, because we need to make sure the
                // lifetime of the key outlives the lifetime of the row.  (If we used nodeRef, for example, it gets mutated as we
                // progress through the paint, and then gets disposed at the end.)
                this.rowCache.Add(row.NodeReference, row);
                AddRowToUsedList(row);
            }

            return row;
        }

        void MoveRowToTopOfUsedList(TreeGridRow row)
        {
            if (row == this.headOfRowCache)
            {
                // Already at the front. Since we're not calling AddRowToUsedList, we must do this.
                row.lastLayoutPassUsed = this.rowLayoutPass;
                return;
            }

            RemoveRowFromUsedList(row);

            // NOTE: This marks the row's last-used paint pass with the current pass value.
            AddRowToUsedList(row);
        }

        void RemoveRowFromUsedList(TreeGridRow row)
        {
            if (row.prevLink != null)
            {
                row.prevLink.nextLink = row.nextLink;
            }
            else
            {
                Debug.Assert(row == this.headOfRowCache, "Row doesn't have a prev link, so it should be the head!");
                this.headOfRowCache = row.nextLink;
            }

            if (row.nextLink != null)
            {
                row.nextLink.prevLink = row.prevLink;
            }
            else
            {
                Debug.Assert(row == this.tailOfRowCache, "Row doesn't have a next link, so it should be the tail!");
                this.tailOfRowCache = row.prevLink;
            }

            row.nextLink = null;
            row.prevLink = null;
        }

        void AddRowToUsedList(TreeGridRow row)
        {
            row.lastLayoutPassUsed = this.rowLayoutPass;

            row.prevLink = null;
            row.nextLink = this.headOfRowCache;

            if (this.headOfRowCache != null)
            {
                this.headOfRowCache.prevLink = row;
            }
            else
            {
                Debug.Assert(this.tailOfRowCache == null, "If we don't have a head, then we shouldn't have a tail!");
                this.tailOfRowCache = row;
            }
            this.headOfRowCache = row;
        }

        internal void OnRowHeightChanged(TreeGridRow row)
        {
            if (row.lastLayoutPassUsed == this.rowLayoutPass)
            {
                InvalidateRowLayout(false);
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            bool ctrl = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control);
            bool shift = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift);
            SelectionOperation op = SelectionOperation.SelectOne;
            bool moveCurrentOnly = false;

            if (ctrl && shift)
            {
                op = SelectionOperation.Add;
            }
            else if (ctrl)
            {
                moveCurrentOnly = true;
            }
            else if (shift)
            {
                op = SelectionOperation.ExtendTo;
            }

            if (e.Key == Key.Up)
            {
                if (this.currentNode != null)
                {
                    if (this.currentNode.MoveToPreviousFlatNode())
                    {
                        TreeGridRow newCurrentRow;

                        if (!moveCurrentOnly)
                        {
                            newCurrentRow = SelectNode(this.currentNode, op);
                        }
                        else
                        {
                            newCurrentRow = this.visibleRows.FirstOrDefault(r => r.NodeReference.Equals(this.currentNode));
                            if (newCurrentRow != null)
                            {
                                newCurrentRow.IsCurrent = true;
                            }
                        }

                        if (newCurrentRow == null || newCurrentRow.isPartiallyVisible)
                        {
                            int flatIndex;

                            if (this.currentNode.TryGetFlatIndex(out flatIndex))
                            {
                                this.bottomUpLayout = flatIndex > this.TopItemIndex;
                                this.TopItemIndex = flatIndex;

                                // Do row layout synchronously on keyboard navigation
                                DoRowLayout();
                            }
                            else
                            {
                                Debug.Fail("The current node must be exposed!");
                            }
                        }

                        RaiseCurrentItemChanged();
                    }
                }
                else
                {
                    using (var node = this.rootNode.CreateNodeReference(0))
                    {
                        if (node != null)
                        {
                            SetCurrentNode(node);
                        }
                    }
                    this.TopItemIndex = 0;
                    RaiseCurrentItemChanged();
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                if (this.currentNode != null)
                {
                    if (this.currentNode.MoveToNextFlatNode())
                    {
                        TreeGridRow newCurrentRow;

                        if (!moveCurrentOnly)
                        {
                            newCurrentRow = SelectNode(this.currentNode, op);
                        }
                        else
                        {
                            newCurrentRow = this.visibleRows.FirstOrDefault(r => r.NodeReference.Equals(this.currentNode));
                            if (newCurrentRow != null)
                            {
                                newCurrentRow.IsCurrent = true;
                            }
                        }

                        if (newCurrentRow == null || newCurrentRow.isPartiallyVisible)
                        {
                            int nodeIndex;

                            if (this.currentNode.TryGetFlatIndex(out nodeIndex))
                            {
                                this.bottomUpLayout = (nodeIndex > this.TopItemIndex);
                                this.TopItemIndex = nodeIndex;

                                // Do row layout synchronously on keyboard navigation
                                DoRowLayout();
                            }
                            else
                            {
                                Debug.Fail("the current node must be exposed!");
                            }
                        }
                        RaiseCurrentItemChanged();
                    }
                }
                else
                {
                    using (var node = this.rootNode.CreateNodeReference(0))
                    {
                        if (node != null)
                        {
                            SetCurrentNode(node);
                        }
                    }
                    this.TopItemIndex = 0;
                    RaiseCurrentItemChanged();
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                if (ctrl && !shift)
                {
                    if (this.currentNode != null)
                    {
                        int index;
                        TreeGridRow newCurrentRow = SelectNode(this.currentNode, SelectionOperation.Toggle);

                        if ((newCurrentRow == null || newCurrentRow.isPartiallyVisible) && this.currentNode.TryGetFlatIndex(out index))
                        {
                            this.TopItemIndex = index;

                            // Do row layout synchronously on keyboard navigation
                            DoRowLayout();
                        }
                    }
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                if (this.currentNode != null && this.currentNode.IsExpandable)
                {
                    if (this.currentNode.IsExpanded)
                    {
                        if (this.currentNode.MoveToNextFlatNode())
                        {
                            int index;
                            var newCurrentRow = SelectNode(this.currentNode, SelectionOperation.SelectOne);

                            if ((newCurrentRow == null || newCurrentRow.isPartiallyVisible) && this.currentNode.TryGetFlatIndex(out index))
                            {
                                this.bottomUpLayout = (index > this.TopItemIndex);
                                this.TopItemIndex = index;

                                // Do row layout synchronously on keyboard navigation
                                DoRowLayout();
                            }
                            RaiseCurrentItemChanged();
                        }
                    }
                    else
                    {
                        this.currentNode.IsExpanded = true;
                    }
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                if (this.currentNode != null)
                {
                    if (this.currentNode.IsExpandable && this.currentNode.IsExpanded)
                    {
                        this.currentNode.IsExpanded = false;
                    }
                    else if (this.currentNode.MoveToParentNode())
                    {
                        int index;
                        var newCurrentRow = SelectNode(this.currentNode, SelectionOperation.SelectOne);

                        if ((newCurrentRow == null || newCurrentRow.isPartiallyVisible) && this.currentNode.TryGetFlatIndex(out index))
                        {
                            this.bottomUpLayout = (index > this.TopItemIndex);
                            this.TopItemIndex = index;

                            // Do row layout synchronously on keyboard navigation
                            DoRowLayout();
                        }
                        RaiseCurrentItemChanged();
                    }
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                op = shift ? SelectionOperation.ExtendTo : SelectionOperation.SelectOne;

                using (var homeNode = this.rootNode.CreateNodeReference(0))
                {
                    if (homeNode != null)
                    {
                        SetCurrentNode(homeNode);
                        SelectNode(homeNode, op);
                        this.TopItemIndex = 0;
                    }
                }

                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                op = shift ? SelectionOperation.ExtendTo : SelectionOperation.SelectOne;

                using (var endNode = this.rootNode.CreateNodeReference(this.ItemCount - 1))
                {
                    if (endNode != null)
                    {
                        SetCurrentNode(endNode);
                        SelectNode(endNode, op);
                        this.bottomUpLayout = true;
                        this.TopItemIndex = Math.Max(0, this.ItemCount - 1);
                        DoRowLayout();
                    }
                }

                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                int index;
                op = shift ? SelectionOperation.ExtendTo : SelectionOperation.SelectOne;

                if (this.currentNode == null || !this.currentNode.TryGetFlatIndex(out index))
                {
                    using (var homeNode = this.rootNode.CreateNodeReference(0))
                    {
                        if (homeNode != null)
                        {
                            SetCurrentNode(homeNode);
                            SelectNode(homeNode, SelectionOperation.SelectOne);
                            this.TopItemIndex = 0;
                        }
                    }
                }
                else
                {
                    index = Math.Max(0, index - this.VisibleRowCount);

                    using (var upNode = this.rootNode.CreateNodeReference(index))
                    {
                        if (upNode != null)
                        {
                            SetCurrentNode(upNode);
                            SelectNode(upNode, op);
                            this.TopItemIndex = index;
                            DoRowLayout();
                        }
                    }
                }

                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                int index;
                op = shift ? SelectionOperation.ExtendTo : SelectionOperation.SelectOne;

                if (this.currentNode == null || !this.currentNode.TryGetFlatIndex(out index))
                {
                    using (var endNode = this.rootNode.CreateNodeReference(this.ItemCount - 1))
                    {
                        if (endNode != null)
                        {
                            SetCurrentNode(endNode);
                            SelectNode(endNode, SelectionOperation.SelectOne);
                            this.bottomUpLayout = true;
                            this.TopItemIndex = Math.Max(0, this.ItemCount - 1);
                            DoRowLayout();
                        }
                    }
                }
                else
                {
                    index = Math.Min(this.ItemCount - 1, index + this.VisibleRowCount);

                    using (var downNode = this.rootNode.CreateNodeReference(index))
                    {
                        if (downNode != null)
                        {
                            SetCurrentNode(downNode);
                            SelectNode(downNode, op);
                            this.bottomUpLayout = true;
                            this.TopItemIndex = index;
                            DoRowLayout();
                        }
                    }
                }

                e.Handled = true;
            }
        }

        void ClearSelection(bool updateVisuals)
        {
            using (new SelectionChangeSuppressor(this))
            {
                foreach (var kvp in this.selectedNodeReferences)
                {
                    kvp.Value.Dispose();
                    this.changeSuppressor.RegisterSelectionChange();
                }

                this.selectedNodeReferences.Clear();
                this.SelectedItemCount = 0;

                if (updateVisuals)
                {
                    foreach (var row in this.visibleRows)
                    {
                        row.IsSelected = false;
                    }
                }
            }
        }

        void RemoveNodeFromSelection(TreeGridNodeReference node)
        {
            using (new SelectionChangeSuppressor(this))
            {
                // This is why selectedNodeReferences must be a dictionary and not just a hash set.
                // Being in the selection implies a reference count on the node (i.e., the node reference is live).
                // Nodes have value semantics so they can be hashed/placed/found in a dictionary (or hash set).
                // But if we used a hash set, there would be no way to get the actual reference that we need to
                // dispose when we remove a node from the selection.  The incoming node has value equality but not
                // reference equality with the node reference in the table.  With a dictionary, we can do this weird-
                // looking statement:
                var selectedNode = this.selectedNodeReferences[node];

                // This ensures that 'selectedNode' not only references the same node as 'node', but is the actual
                // reference we need to dispose.
                // BUT:  Do NOT dispose it before removing it from the dictionary, because Dispose alters
                // the node reference's hash code.
                this.selectedNodeReferences.Remove(node);
                this.SelectedItemCount = this.selectedNodeReferences.Count;
                selectedNode.Dispose();
                this.changeSuppressor.RegisterSelectionChange();
            }
        }

        void AddNodeToSelection(TreeGridNodeReference node)
        {
            using (new SelectionChangeSuppressor(this))
            {
                // Must clone the node (creating the active reference)...
                var selectedNode = node.Clone();

                // ...and then be SURE that the clone is used for both the key AND the value.  The value is what we
                // dispose when this node is removed from the selection, and the key must not get disposed while it
                // is used as a key (because, again, disposal alters the value semantics).
                this.selectedNodeReferences.Add(selectedNode, selectedNode);
                this.SelectedItemCount = this.selectedNodeReferences.Count;
                this.changeSuppressor.RegisterSelectionChange();
            }
        }

        public TreeGridRow SelectNode(TreeGridNodeReference node, SelectionOperation op)
        {
            int nodeIndex;

            using (new SelectionChangeSuppressor(this))
            {
                if (!node.TryGetFlatIndex(out nodeIndex))
                {
                    Debug.Fail("Selected node must be exposed (can't be in a collapsed parent)!");
                    return null;
                }

                if (op == SelectionOperation.SelectOne)
                {
                    ClearSelection(false);
                    AddNodeToSelection(node);
                    this.selectionAnchorIndex = nodeIndex;
                }
                else if (op == SelectionOperation.Toggle)
                {
                    if (!this.selectedNodeReferences.ContainsKey(node))
                    {
                        AddNodeToSelection(node);
                    }
                    else
                    {
                        RemoveNodeFromSelection(node);
                    }

                    if (this.selectionAnchorIndex == -1)
                    {
                        this.selectionAnchorIndex = nodeIndex;
                    }
                }
                else if (op == SelectionOperation.Add)
                {
                    if (!this.selectedNodeReferences.ContainsKey(node))
                    {
                        AddNodeToSelection(node);
                        if (this.selectionAnchorIndex == -1)
                        {
                            this.selectionAnchorIndex = nodeIndex;
                        }
                    }
                }
                else if (op == SelectionOperation.ExtendTo)
                {
                    if (this.selectionAnchorIndex != -1)
                    {
                        int min = Math.Min(this.selectionAnchorIndex, nodeIndex);
                        int max = Math.Max(this.selectionAnchorIndex, nodeIndex);

                        ClearSelection(false);

                        using (var nodeToSelect = this.rootNode.CreateNodeReference(min))
                        {
                            while (nodeToSelect != null && (min <= max))
                            {
                                AddNodeToSelection(nodeToSelect);
                                if (!nodeToSelect.MoveToNextFlatNode())
                                {
                                    break;
                                }
                                min += 1;
                            }
                        }
                    }
                }

                TreeGridRow newCurrentRow = null;

                foreach (var row in this.visibleRows)
                {
                    row.IsSelected = this.selectedNodeReferences.ContainsKey(row.NodeReference);
                    if (row.NodeReference.Equals(this.currentNode))
                    {
                        newCurrentRow = row;
                    }
                }

                if (newCurrentRow != null)
                {
                    newCurrentRow.IsCurrent = true;
                }

                return newCurrentRow;
            }
        }

        public void SelectAll()
        {
            using (new SelectionChangeSuppressor(this))
            {
                using (var nodeRef = CreateNodeReferenceForItemIndex(0))
                {
                    while (nodeRef != null)
                    {
                        if (!this.selectedNodeReferences.ContainsKey(nodeRef))
                        {
                            AddNodeToSelection(nodeRef);
                        }

                        if (!nodeRef.MoveToNextFlatNode())
                        {
                            break;
                        }
                    }
                }

                foreach (var row in this.visibleRows)
                {
                    row.IsSelected = true;
                }
            }
        }

        void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var c in this.Columns)
            {
                c.Owner = this;
            }
        }

        void CollectDesiredColumnWidths(TreeGridRow row)
        {
            if (row.IsLoaded)
            {
                foreach (var column in this.columns)
                {
                    if (double.IsNaN(column.Width))
                    {
                        double desiredWidth;

                        if (row.TryGetDesiredCellWidth(column, out desiredWidth))
                        {
                            column.IsWidthLocked = true;
                        }
                        else
                        {
                            column.IsWidthLocked = false;
                        }

                        column.ActualWidth = Math.Max(column.MinWidth, row == this.headerRow ? desiredWidth : Math.Max(column.ActualWidth, desiredWidth));
                    }
                    else
                    {
                        column.ActualWidth = Math.Max(column.Width, column.MinWidth);
                        column.IsWidthLocked = true;
                    }
                }
            }
        }

        void OnCurrentItemChanged(object sender, RoutedEventArgs e)
        {
            var row = e.OriginalSource as TreeGridRow;

            if (this.currentRow != null && this.currentRow != row)
            {
                this.currentRow.IsCurrent = false;
            }

            if (this.currentNode != null)
            {
                this.currentNode.Dispose();
                this.currentNode = null;
            }

            this.currentRow = row;
            this.currentNode = row.NodeReference.Clone();
            RaiseCurrentItemChanged();
        }

        void OnRowClicked(object sender, RowClickedEventArgs e)
        {
            bool shift = e.Modifiers.HasFlag(ModifierKeys.Shift);
            bool ctrl = e.Modifiers.HasFlag(ModifierKeys.Control);

            // On right-click, preserve any selection that is more than 1 row
            if (e.OriginalArgs.ChangedButton == MouseButton.Left || this.selectedNodeReferences.Count <= 1)
            {
                SetCurrentNode(e.Row.NodeReference);

                if (shift && ctrl)
                {
                    SelectNode(e.Row.NodeReference, SelectionOperation.Add);
                }
                else if (shift)
                {
                    SelectNode(e.Row.NodeReference, SelectionOperation.ExtendTo);
                }
                else if (ctrl)
                {
                    SelectNode(e.Row.NodeReference, SelectionOperation.Toggle);
                }
                else
                {
                    SelectNode(e.Row.NodeReference, SelectionOperation.SelectOne);
                }

                if (e.OriginalArgs.ClickCount == 2)
                {
                    if (e.Row.IsExpandable)
                    {
                        e.Row.IsExpanded = !e.Row.IsExpanded;
                    }
                }
            }
        }

        void RaiseCurrentItemChanged()
        {
            var handler = this.CurrentItemChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        static void OnTopItemIndexChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TreeGrid grid = obj as TreeGrid;

            if (grid != null && !grid.ignoreTopIndexChanges)
            {
                if (grid.TopItemIndex < 0)
                {
                    grid.ignoreTopIndexChanges = true;
                    grid.TopItemIndex = 0;
                    grid.ignoreTopIndexChanges = false;
                }
                else if (grid.TopItemIndex >= grid.ItemCount)
                {
                    grid.ignoreTopIndexChanges = true;
                    grid.TopItemIndex = Math.Max(grid.ItemCount - 1, 0);
                    grid.ignoreTopIndexChanges = false;
                }
                grid.InvalidateRowLayout(false);
            }
        }

        internal void OnChildCountChanged(TreeGridNode child, int oldCount)
        {
            this.ItemCount = this.rootNode.FlatCount;
            this.VerticalScrollRange = this.ItemCount - 1;

            // When nodes collapse, it's possible to "swallow" the current node.  If that happens, we need
            // to move the current node up to it's "collapse point" -- the last (downward) exposed node in its heritage.
            if (this.currentNode != null)
            {
                if (this.currentNode.MoveToFirstCollapsePoint())
                {
                    RaiseCurrentItemChanged();
                }
            }

            // Don't do anything if we haven't had our template applied yet...
            if (this.canvas != null)
            {
                if (this.TopItemIndex >= this.ItemCount)
                {
                    // A collapse-all, filter, or similar operation could cause this.  Do a bottom-up
                    // (and therefore synchronous) layout.
                    this.TopItemIndex = Math.Max(0, this.ItemCount - 1);
                    this.bottomUpLayout = true;
                    DoRowLayout();
                }
                else
                {
                    InvalidateRowLayout(false);
                }
            }
        }

        internal void OnNodeInvalidated(TreeGridNodeReference invalidatedNodeRef)
        {
            bool updateNeeded = false;

            if (invalidatedNodeRef.Equals(this.currentNode))
            {
                SetCurrentNode(null);
                updateNeeded = true;
            }

            if (this.selectedNodeReferences.ContainsKey(invalidatedNodeRef))
            {
                RemoveNodeFromSelection(invalidatedNodeRef);
                updateNeeded = true;
            }

            if (!updateNeeded)
            {
                updateNeeded = this.visibleRows.Any(r => r.NodeReference.Equals(invalidatedNodeRef));
            }

            if (updateNeeded)
            {
                InvalidateRowLayout(false);
            }
        }

        public enum SelectionOperation
        {
            // Select just the node passed in, clear all others
            SelectOne,

            // Extend the selection from the anchor to the node passed in
            ExtendTo,

            // Add the given node to the selection (if not already there)
            Add,

            // Toggle the selection state of the given node
            Toggle,
        }

        // This little class ensures that we only fire the SelectionChanged event once for
        // actions that may make many changes to the selection.  Usage pattern is to enclose
        // code that modifies the selection in a "using (new SelectionChangeSuppressor)" clause,
        // and it will correctly stack and accumulate calls to RegisterSelectionChange, and
        // fire the event on disposal of the last in the stack as appropriate.
        internal class SelectionChangeSuppressor : IDisposable
        {
            TreeGrid grid;
            SelectionChangeSuppressor previous;
            bool changed;

            public SelectionChangeSuppressor(TreeGrid grid)
            {
                this.grid = grid;
                this.previous = grid.changeSuppressor;
                this.changed = false;
                grid.changeSuppressor = this;
            }

            public void RegisterSelectionChange()
            {
                this.changed = true;
            }

            public void Dispose()
            {
                Debug.Assert(grid.changeSuppressor == this);
                grid.changeSuppressor = this.previous;
                if (this.previous == null)
                {
                    var handler = this.grid.SelectionChanged;

                    if (handler != null && this.changed)
                    {
                        handler(this.grid, EventArgs.Empty);
                    }
                }
                else if (this.changed)
                {
                    this.previous.changed = true;
                }
            }
        }
    }
}
