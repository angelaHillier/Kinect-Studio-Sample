//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

// Code behind file for TreeGridView
namespace Microsoft.Xbox.Tools.Shared
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Data;
    using System.Reflection;
    using System.Windows.Input;
    using System.Diagnostics;
    using System.Collections.Specialized;

    /// <summary>
    /// Interaction logic for WPFTreeGrid.xaml
    /// </summary>
    /// <remarks>
    /// We use ListView to mimic the appeareance of a Tree
    /// A new DP named "HierarchicalItemsSource" is added that expects a hierarchical data structure
    /// but internally a flat list is generated from HierarchicalItemsSource; 
    /// and assigned to ItemsSource, from which ListView populates its rows.
    /// Each row is indented based on nesting level of the item in the tree.
    /// 
    /// A few things about handling of the flat list:
    /// 1. the flat list contains all currently "visible" items 
    ///    (an item becomes "visible" when its visible parent is expanded)
    /// 2. items are inserted/removed from the list when the parent item is expanded/collapsed.
    ///     2.1 when an item is expanded, its child items are inserted right after it
    ///     2.2 when an item is collapsed, its child items (including grandchildren) are removed from the list;
    ///         the child items are found by looking for any following items that have a higher nesting level
    /// 
    /// </remarks>
    public partial class TreeGridView : ListView
    {
        #region Dependency Properties

        /// <summary>
        /// Defines the DP for foreground brush of selected item
        /// Default value: Brushes.White
        /// </summary>
        public static readonly DependencyProperty SelectionForegroundProperty =
            DependencyProperty.Register(
            "SelectionForeground",
            typeof(Brush),
            typeof(TreeGridView),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        /// <summary>
        /// Defines DP for background brush of selected item
        /// Default value: Brushes.DarkBlue
        /// </summary>
        public static readonly DependencyProperty SelectionBackgroundProperty =
            DependencyProperty.Register(
            "SelectionBackground",
            typeof(Brush),
            typeof(TreeGridView),
            new FrameworkPropertyMetadata(SystemColors.HighlightBrush, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        public static readonly DependencyProperty SelectionBorderProperty = DependencyProperty.Register(
            "SelectionBorder", typeof(Brush), typeof(TreeGridView),
            new FrameworkPropertyMetadata(Brushes.Blue, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        /// <summary>
        /// Dependency property that contains the hierarchical data source
        /// </summary>
        /// <remarks>
        /// This replaces use of ListView.ItemsSource; the latter should NOT be used.
        /// User should also assign a HierarchicalTemplate to ItemsTemplate, specifying the binding path to
        /// get children items in the data object
        /// </remarks>
        public static readonly DependencyProperty HierarchicalItemsSourceProperty =
            DependencyProperty.Register(
            "HierarchicalItemsSource",
            typeof(IEnumerable),
            typeof(TreeGridView),
            new FrameworkPropertyMetadata((IEnumerable)null, new PropertyChangedCallback(OnHierarchicalItemsSourceChanged)));
        #endregion

        #region private fields
        /// <summary>
        /// The flat list internally feeded into ListView.ItemsSource
        /// TreeGridView handles the process of transform hierarchical data source (found in HierarchicalItemsSource)
        /// to a flat list (feed into ItemsSource) as items are expanded/collapsed
        /// </summary>
        private BulkObservableCollection<TreeGridViewItemInfo> itemInfoFlatList = new BulkObservableCollection<TreeGridViewItemInfo>();

        /// <summary>
        /// Keeps expanded items that are removed from the flat list (due to parent being collapsed);
        /// Later if the parent is expanded again, we need to restore the expansion state of this item.
        /// This is handled in ExpandItem()
        /// </summary>
        private HashSet<object> orphanExpandedItemsStore = new HashSet<object>();

        private bool themeChanged = false;

        // the property name to get the child collection from a data object
        private string childCollectionPropertyName = null;
        private PropertyInfo childCollectionPropertyInfo = null;

        // side effect caused by bulk insertion/removal in the list view will result in the view to be refreshed,
        // and losing focus
        // these will be restored after next status change of ItemContainerGenerator
        private TreeGridViewItemInfo restoreFocusItemInfo = null;
        private TreeGridViewItemInfo focusItemInfo = null;

        #endregion

        /// <summary>
        /// Construct a TreeGridView
        /// </summary>
        public TreeGridView()
        {
            this.InitializeComponent();
            this.Loaded += new RoutedEventHandler(this.OnLoaded);
            this.ItemContainerGenerator.StatusChanged += new EventHandler(this.ItemContainerGenerator_StatusChanged);
            this.itemInfoFlatList.CollectionChanged += OnItemListCollectionChanged;
            this.CommandBindings.Add(new CommandBinding(TreeGridViewItem.ExpandAllCommand, OnExpandAllExecuted));
        }

        void OnExpandAllExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var toBeExpanded = this.itemInfoFlatList.Where(i => i.Level == 0).Select(i => i.Data).ToArray();

            this.suppressDisplayedItemsChangedEvent = true;
            try
            {
                foreach (var i in toBeExpanded)
                {
                    this.ExpandItemRecursive(i);
                }
            }
            finally
            {
                this.suppressDisplayedItemsChangedEvent = false;
            }

            RaiseDisplayedItemsChangedEvent();
        }

        void OnItemListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseDisplayedItemsChangedEvent();
        }

        void RaiseDisplayedItemsChangedEvent()
        {
            if (!this.suppressDisplayedItemsChangedEvent)
            {
                var handler = this.DisplayedItemsChanged;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }
        }

        public event EventHandler DisplayedItemsChanged;
        private bool suppressDisplayedItemsChangedEvent;    // Used to batch list recreation into a single DisplayedItemsChanged event

        #region Dependency Property Wrappers
        /// <summary>
        /// Gets or sets the hierarchical data source
        /// </summary>
        public IEnumerable HierarchicalItemsSource
        {
            get
            {
                return this.GetValue(TreeGridView.HierarchicalItemsSourceProperty) as IEnumerable;
            }
            set
            {
                this.SetValue(TreeGridView.HierarchicalItemsSourceProperty, value);
            }
        }

        /// <summary>
        /// Gets the object that is currently selected
        /// </summary>
        /// <remarks>
        /// Internally the selected item is of type TreeGridViewInfo, 
        /// but we'd like to give user the direct data object
        /// </remarks>
        public new object SelectedItem
        {
            get
            {
                TreeGridViewItemInfo info = base.SelectedItem as TreeGridViewItemInfo;
                if (info != null)
                {
                    return info.Data;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                int index;
                var info = FindItemInfoForData(value, out index);
                base.SelectedItem = info;
                this.focusItemInfo = info;
                var containerElement = this.ItemContainerGenerator.ContainerFromItem(info) as UIElement;

                if (containerElement != null && this.IsKeyboardFocusWithin)
                {
                    containerElement.Focus();
                }
                else
                {
                    restoreFocusItemInfo = this.focusItemInfo;
                }
            }
        }


        /// <summary>
        /// Gets or sets the foreground brush for selected item
        /// </summary>
        public Brush SelectionForeground
        {
            get
            {
                return this.GetValue(TreeGridView.SelectionForegroundProperty) as Brush;
            }
            set
            {
                this.SetValue(TreeGridView.SelectionForegroundProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the background brush for selected item
        /// </summary>
        public Brush SelectionBackground
        {
            get
            {
                return this.GetValue(TreeGridView.SelectionBackgroundProperty) as Brush;
            }
            set
            {
                this.SetValue(TreeGridView.SelectionBackgroundProperty, value);
            }
        }

        public Brush SelectionBorder
        {
            get { return (Brush)GetValue(SelectionBorderProperty); }
            set { SetValue(SelectionBorderProperty, value); }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Expands a node containing the given data.
        /// All its parent/grandparent nodes up to root must already be expanded. 
        /// Otherwise there is no effect.
        /// </summary>
        /// <param name="data">The data object that the target node contains</param>
        /// <returns>true if the item is already expanded, or the expand succeeds</returns>
        public bool ExpandItem(object data)
        {
            /* 
             * additionally handles a special scenario: 
             * Assume a tree like this (-> denotes parent-child relationship):
             * A(expanded) -> B(expanded) -> C
             * 
             * now user collapse A, all child/grandchild nodes (including B and C) are removed from the flat list;
             * later user expands A again, B is created (but default collapsed) and 
             * inserted after A into the list, but user expects B to be expanded as it was.
             * So we need to furthur expand B as well.
             * orphanExpandedItems is used to remember nodes like B (expanded before removed),
             * whenever an item is expanded, we'll check in to orphanExpandedItems to see if any 
             * of the child items need further expansion, and so on.
             * (as a matter of fact WPF TreeView internally does similar things.)
            */

            int index;
            TreeGridViewItemInfo itemInfo = this.FindItemInfoForData(data, out index);
            if (itemInfo == null)
            {
                return false;
            }

            if (itemInfo.IsExpanded)
            {
                return true;
            }

            var itemsToInsert = new List<TreeGridViewItemInfo>();
            this.GetChildAndOrphanItemInfo(itemInfo, itemsToInsert);

            if (itemsToInsert.Count > 0)
            {
                // save focus and restore later
                this.restoreFocusItemInfo = this.focusItemInfo;
                this.itemInfoFlatList.InsertRange(index + 1, itemsToInsert);
            }

            return true;
        }

        /// <summary>
        /// Collapse a node containing the given data.
        /// All its parent nodes up to root must already be expanded, including itself.
        /// Otherwise there is no effect.
        /// </summary>
        /// <param name="data">The data object that the target node contains</param>
        public void CollapseItem(object data)
        {
            // Collapse a node is a simple operation as to remove any following nodes whose level
            // is bigger than this one, until a node with equal or smaller level value is reached.
            // in the process if a removed node was expanded, we keep it in orphanExpandedItemsStore
            int index;
            TreeGridViewItemInfo itemInfo = this.FindItemInfoForData(data, out index);
            bool focusRemoved = false;

            if (itemInfo != null && itemInfo.IsExpanded)
            {
                int n = index + 1;
                int count = 0;
                while (n < this.itemInfoFlatList.Count)
                {
                    var childInfo = this.itemInfoFlatList[n];
                    n++;
                    if (childInfo.Level > itemInfo.Level)
                    {
                        count++;
                        // before remove it, check if its selected or focused on
                        if (base.SelectedItem == childInfo)
                        {
                            // selected item is removed, select the parent item instead
                            base.SelectedItem = itemInfo;
                        }

                        if (this.focusItemInfo == childInfo)
                        {
                            this.focusItemInfo = null;
                            focusRemoved = true;
                        }

                        if (childInfo.IsExpanded)
                        {
                            this.orphanExpandedItemsStore.Add(childInfo.Data);
                        }
                    }
                    else
                    {
                        // equal or smaller Level value reached, no more child nodes
                        break;
                    }
                }
                if (count > 0)
                {
                    itemInfo.IsExpanded = false;
                    if (focusRemoved)
                    {
                        this.restoreFocusItemInfo = itemInfo;
                    }
                    else
                    {
                        this.restoreFocusItemInfo = this.focusItemInfo;
                    }
                    this.itemInfoFlatList.RemoveRange(index + 1, count);
                }
            }
        }

        public void WhileSuppressingChangeEvents(Action action)
        {
            if (this.suppressDisplayedItemsChangedEvent)
            {
                action();
            }
            else
            {
                this.suppressDisplayedItemsChangedEvent = true;
                try
                {
                    action();
                }
                finally
                {
                    this.suppressDisplayedItemsChangedEvent = false;
                }

                // Assume change was made...
                RaiseDisplayedItemsChangedEvent();
            }
        }

        /// <summary>
        /// Expands a node recursively till all child/grandchild nodes are expanded.
        /// All its parent nodes up to root must already be expanded. 
        /// Otherwise there is no effect.
        /// </summary>
        /// <param name="data">The data object that the target node contains</param>
        public void ExpandItemRecursive(object data)
        {
            int index;
            TreeGridViewItemInfo itemInfo = this.FindItemInfoForData(data, out index);
            ExpandItemRecursiveInternal(itemInfo, index);
        }

        void ExpandItemRecursiveInternal(TreeGridViewItemInfo itemInfo, int index)
        {
            // Expand a node recursively till the whole subtree under it are expanded
            // this occurs when user press '*' on a node
            // handling should be careful since the subtree may already be partially expanded
            int partialExpandedCount = 0;

            if (itemInfo != null)
            {
                var itemsToInsert = new List<TreeGridViewItemInfo>();
                if (!itemInfo.IsExpanded)
                {
                    this.GetChildItemInfoRecursive(itemInfo, itemsToInsert);
                    partialExpandedCount = 0;
                }
                else
                {
                    int x = index + 1;
                    while (x < this.itemInfoFlatList.Count)
                    {
                        var childInfo = this.itemInfoFlatList[x];
                        x++;
                        if (childInfo.Level > itemInfo.Level)
                        {
                            partialExpandedCount++;
                            itemsToInsert.Add(childInfo);
                            if (!childInfo.IsExpanded)
                            {
                                this.GetChildItemInfoRecursive(childInfo, itemsToInsert);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (partialExpandedCount < itemsToInsert.Count)
                {
                    // save focus
                    this.restoreFocusItemInfo = this.focusItemInfo;
                    // first collapse the item (in case it is partially expanded already)
                    if (partialExpandedCount > 0)
                    {
                        this.itemInfoFlatList.RemoveRange(index + 1, partialExpandedCount);
                    }
                    // insert the whole sub tree
                    this.itemInfoFlatList.InsertRange(index + 1, itemsToInsert);
                }
            }
        }

        public void ScrollItemIntoView(object data)
        {
            int index;

            // Consider optimizing this for large lists...
            TreeGridViewItemInfo info = this.FindItemInfoForData(data, out index);
            if (info != null)
            {
                this.ScrollIntoView(info);
            }
        }

        /// <summary>
        /// Select/unselect a node. 
        /// All parent nodes up to root must already be expanded. 
        /// Otherwise there is no effect.
        /// </summary>
        /// <param name="data">The data that the target node contains</param>
        /// <param name="selected">true: select the node; false: unselect the node</param>
        public void SelectItem(object data, bool selected)
        {
            int index;
            TreeGridViewItemInfo info = this.FindItemInfoForData(data, out index);
            if (info != null)
            {
                this.ScrollIntoView(info);

                this.Dispatcher.BeginInvoke(
                    (Action)(() =>
                    {
                        TreeGridViewItem container = this.ItemContainerGenerator.ContainerFromIndex(index) as TreeGridViewItem;
                        if (container != null)
                        {
                            container.IsSelected = selected;
                        }
                    }),
                    System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        #endregion

        #region Internal Methods
        /// <summary>
        /// Move focus to parent item
        /// </summary>
        /// <param name="childItem">The currently focused item</param>
        /// <returns>true if succeeds; otherwise false</returns>
        internal bool FocusToParentItem(TreeGridViewItem childItem)
        {
            TreeGridViewItemInfo childInfo = this.ItemContainerGenerator.ItemFromContainer(childItem) as TreeGridViewItemInfo;
            if (childInfo != null)
            {
                var parentInfo = this.GetParentItemInfo(childInfo);
                if (parentInfo != null)
                {
                    this.ScrollIntoView(parentInfo);
                    TreeGridViewItem parentContainer = this.ItemContainerGenerator.ContainerFromItem(parentInfo) as TreeGridViewItem;
                    if (parentContainer != null)
                    {
                        return parentContainer.Focus();
                    }

                }
            }
            return false;
        }

        /// <summary>
        /// Called by an item to notify the view when it gets focus
        /// The view has to know the currently focused item
        /// </summary>
        /// <param name="childItem">the child item got focus</param>
        internal void NotifyItemGotFocus(TreeGridViewItem childItem)
        {
            this.focusItemInfo = this.ItemContainerGenerator.ItemFromContainer(childItem) as TreeGridViewItemInfo;
            // clear the pending focus restore since a new focus is set by user
            this.restoreFocusItemInfo = null;
        }


        #endregion

        #region Protected Methods
        /// <summary>
        /// Creates container for an item
        /// </summary>
        /// <returns>The container for an item</returns>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeGridViewItem();
        }

        /// <summary>
        /// Check if the item is already a container before creating a container for it
        /// </summary>
        /// <param name="item">The item to check </param>
        /// <returns>true if the item is already a container</returns>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeGridViewItem;
        }

        /// <summary>
        /// Prepare the item container.
        /// This is after ItemContainerGenerator created a TreeGridViewItem, and we have a chance to 
        /// do some intialization work 
        /// </summary>
        /// <param name="element">The TreeGridViewItem that is newly created (or recycled)</param>
        /// <param name="item">The data object associated (a TreeGridViewItemInfo object)</param>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            TreeGridViewItemInfo info = item as TreeGridViewItemInfo;
            TreeGridViewItem container = element as TreeGridViewItem;

            if (info != null)
            {
                container.ParentView = this;

                // initialize necessary properties
                container.Level = info.Level;
                container.IsExpanded = info.IsExpanded;

                container.HasChildren = this.DataHasChildren(info.Data);
                base.PrepareContainerForItemOverride(element, info.Data);
            }
        }

        /// <summary>
        /// Clean up the item container
        /// </summary>
        /// <param name="element">The TreeGridViewItem to be discarded</param>
        /// <param name="item">The associated data object (TreeGridViewItemInfo)</param>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            TreeGridViewItemInfo info = item as TreeGridViewItemInfo;
            TreeGridViewItem tgvi = element as TreeGridViewItem;
            tgvi.ParentView = null;
            base.ClearContainerForItemOverride(element, item);
        }

        /// <summary>
        /// Handle template change
        /// (when the control is first created, there'll be a template change. Later changing to some sytem theme could also result a template change)
        /// </summary>
        /// <param name="oldTemplate">The old template</param>
        /// <param name="newTemplate">The new template</param>
        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            // set a flag here as the visual tree is not constructed yet
            this.themeChanged = true;
        }

        /// <summary>
        /// When data template changes, get the new 'ItemsSource' as the property name for child items
        /// </summary>
        /// <param name="oldItemTemplate">the old data template</param>
        /// <param name="newItemTemplate">the new data template</param>
        protected override void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
        {
            base.OnItemTemplateChanged(oldItemTemplate, newItemTemplate);

            HierarchicalDataTemplate itemTemplate = this.ItemTemplate as HierarchicalDataTemplate;

            this.childCollectionPropertyName = null;
            this.childCollectionPropertyInfo = null;

            if (itemTemplate != null)
            {
                Binding childBinding = itemTemplate.ItemsSource as Binding;
                if (childBinding != null && childBinding.Path != null)
                {
                    // only support simple property path here, other parameters ignored
                    this.childCollectionPropertyName = childBinding.Path.Path;
                }
            }
        }

        protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == this && this.focusItemInfo != null)
            {
                var containerElement = this.ItemContainerGenerator.ContainerFromItem(this.focusItemInfo) as UIElement;

                if (containerElement != null)
                {
                    containerElement.Focus();
                    e.Handled = true;
                    return;
                }
            }
        }

        #endregion

        #region Private Methods

        // Called when HierarchicalItemsSource dependency property changed
        // When a new hierarchical data source is assigned, 
        // construct the initial flat list by taking only the root nodes (the initial view is all root nodes collapsed)
        private static void OnHierarchicalItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TreeGridView tree = (TreeGridView)d;
            IEnumerable oldValue = (IEnumerable)e.OldValue;
            IEnumerable newValue = (IEnumerable)e.NewValue;

            tree.suppressDisplayedItemsChangedEvent = true;
            try
            {
                tree.orphanExpandedItemsStore.Clear();
                tree.itemInfoFlatList.Clear();

                if (e.NewValue != null)
                {
                    foreach (object x in newValue)
                    {
                        // assign level 0 to root nodes 
                        tree.itemInfoFlatList.Add(new TreeGridViewItemInfo(x, 0));
                    }
                }
            }
            finally
            {
                tree.suppressDisplayedItemsChangedEvent = false;
            }
            tree.RaiseDisplayedItemsChangedEvent();
            ((ListView)tree).ItemsSource = tree.itemInfoFlatList;
        }


        // workaround an issue that the gridline is not aligned with header
        // this is due to different margin setting in GridViewHeaderRowPresenter, in different system themes. 
        // when the template is changed, we explictly clear the margin value on GridViewHeaderRowPresenter
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!IsVisible)
            {
                // only handle this when everything is visible
                return;
            }
            if (!this.themeChanged)
            {
                // only change once per theme
                return;
            }

            this.themeChanged = false;

            GridViewHeaderRowPresenter headerPresenter = null;
            // locate the header control by iterating the child element of the listview
            // this may not be cheap however, we only do this occasionally (when a theme changes, for example)
            Queue<DependencyObject> children = new Queue<DependencyObject>();
            children.Enqueue(this);

            while (headerPresenter == null && children.Count > 0)
            {
                DependencyObject child = children.Dequeue();
                headerPresenter = child as GridViewHeaderRowPresenter;
                int count = VisualTreeHelper.GetChildrenCount(child);
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        children.Enqueue(VisualTreeHelper.GetChild(child, i));
                    }
                }
            }

            if (headerPresenter != null)
            {
                headerPresenter.Margin = new Thickness(0, 0, 0, 0);
            }
        }

        private void TreeGridView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // when view is refreshed and we have pending item to restore focus, 
            // the current focus is on the view
            // upon navigational key input, the view will receive it but we need to scroll item
            // into view and focus the item instead
            if (this.restoreFocusItemInfo != null && this.itemInfoFlatList.IndexOf(this.restoreFocusItemInfo) >= 0)
            {
                switch (e.Key)
                {
                    case Key.Up:
                    case Key.Down:
                    case Key.Left:
                    case Key.Right:
                    case Key.Add:
                    case Key.Subtract:
                    case Key.Multiply:
                        this.ScrollIntoView(this.restoreFocusItemInfo);
                        e.Handled = true;
                        break;
                    default:
                        break;
                }
            }

        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (this.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                if (this.restoreFocusItemInfo != null)
                {
                    TreeGridViewItem tgvi = this.ItemContainerGenerator.ContainerFromItem(this.restoreFocusItemInfo) as TreeGridViewItem;
                    if (tgvi != null)
                    {
                        if (this.IsFocused)
                        {
                            tgvi.Focus();
                            this.restoreFocusItemInfo = null;
                        }
                    }
                }
            }
        }


        private PropertyInfo GetChildCollectionPropertyInfo(object data)
        {
            if (data == null)
            {
                return null;
            }

            if (this.childCollectionPropertyInfo == null)
            {
                if (!string.IsNullOrEmpty(this.childCollectionPropertyName))
                {
                    Type dataType = data.GetType();
                    try
                    {
                        this.childCollectionPropertyInfo = dataType.GetProperty(this.childCollectionPropertyName);
                    }
                    catch (Exception e)
                    {
                        // wrong property name specified
                        string msg = string.Format(
                            "TreeGridView: Property name '{0}' is not found on data type {1}",
                            this.childCollectionPropertyName,
                            dataType);
                        throw new InvalidOperationException(msg, e);
                    }
                }
            }
            return this.childCollectionPropertyInfo;
        }


        private IEnumerable GetChildCollection(object data)
        {
            IEnumerable result = null;
            if (data == null)
            {
                return result;
            }
            PropertyInfo pi = this.GetChildCollectionPropertyInfo(data);
            if (pi != null)
            {
                try
                {
                    result = pi.GetValue(data, null) as IEnumerable;
                }
                catch (Exception)
                {
                }
            }
            return result;
        }

        private bool DataHasChildren(object data)
        {
            IEnumerable childCollection = this.GetChildCollection(data);
            bool hasChildren = false;
            if (childCollection != null)
            {
                IEnumerator itor = childCollection.GetEnumerator();
                hasChildren = itor.MoveNext();
            }
            return hasChildren;
        }

        // Get children items of an item; 
        // The binding path is expected from TreeGridView.ItemsTemplate which should be a HierarchicalDataTemplate,
        // Use reflection to get the property value
        private List<TreeGridViewItemInfo> CreateChildItemInfoCollection(TreeGridViewItemInfo info)
        {
            List<TreeGridViewItemInfo> result = null;

            if (info == null || info.Data == null)
            {
                return result;
            }
            IEnumerable childCollection = this.GetChildCollection(info.Data);
            if (childCollection != null)
            {
                foreach (var c in childCollection)
                {
                    if (result == null)
                    {
                        result = new List<TreeGridViewItemInfo>();
                    }
                    result.Add(new TreeGridViewItemInfo(c, info.Level + 1));
                }
            }

            return result;
        }

        // searches the flat list for an TreeGridViewItemInfo that wraps the given data
        private TreeGridViewItemInfo FindItemInfoForData(object data, out int index)
        {
            index = 0;
            foreach (var info in this.itemInfoFlatList)
            {
                if (info.Data == data)
                {
                    return info;
                }
                index++;
            }
            return null;
        }

        private void GetChildItemInfoRecursive(TreeGridViewItemInfo itemInfo, List<TreeGridViewItemInfo> appendList)
        {

            List<TreeGridViewItemInfo> childrenInfo = this.CreateChildItemInfoCollection(itemInfo);
            if (childrenInfo != null && childrenInfo.Count > 0)
            {
                itemInfo.IsExpanded = true;
                foreach (var c in childrenInfo)
                {
                    appendList.Add(c);
                    if (this.orphanExpandedItemsStore.Contains(c.Data))
                    {
                        this.orphanExpandedItemsStore.Remove(c.Data);
                    }
                    this.GetChildItemInfoRecursive(c, appendList);
                }
            }
        }

        private void GetChildAndOrphanItemInfo(TreeGridViewItemInfo itemInfo, List<TreeGridViewItemInfo> appendList)
        {
            List<TreeGridViewItemInfo> childrenInfo = this.CreateChildItemInfoCollection(itemInfo);
            if (childrenInfo != null && childrenInfo.Count > 0)
            {
                itemInfo.IsExpanded = true;
                foreach (var c in childrenInfo)
                {
                    appendList.Add(c);
                    if (this.orphanExpandedItemsStore.Contains(c.Data))
                    {
                        this.orphanExpandedItemsStore.Remove(c.Data);
                        this.GetChildAndOrphanItemInfo(c, appendList);
                    }
                }
            }
        }

        // find parent item of specified item;
        // look back to find the first item whose Level value is smaller than this one
        private TreeGridViewItemInfo GetParentItemInfo(TreeGridViewItemInfo childInfo)
        {
            int childIndex = this.itemInfoFlatList.IndexOf(childInfo);
            if (childIndex >= 0 && childIndex < this.itemInfoFlatList.Count)
            {
                for (int i = childIndex - 1; i >= 0; i--)
                {
                    var parentInfo = this.itemInfoFlatList[i];
                    if (parentInfo.Level < childInfo.Level)
                    {
                        return parentInfo;
                    }
                }
            }
            return null;
        }

        #endregion

        /// <summary>
        /// ObservableCollection does not support bulk insertion/removal, which is crucial to scalability
        /// (the undelying storage is an array, so insert many items individually into the middle of the array
        /// will result as many memory move/or even allocation)
        /// Here we derive from OC and provide bulk insertion and removal support.
        /// We need to point out that ItemsControl does not respond well (or at all) to bulk change notifications
        /// that is why a 'Reset' change is sent out whenever a bulk operation occurs.
        /// The ItemsControl responds to 'Reset' notification by refreshing the view.
        /// </summary>
        /// <typeparam name="T">The type of object in the collection</typeparam>
        private class BulkObservableCollection<T> : ObservableCollection<T>
        {
            /// <summary>
            /// Bulk insert; side effect is the view will be refreshed.
            /// </summary>
            /// <param name="index">the starting index to insert</param>
            /// <param name="itemsToInsert">the list of items to insert</param>
            public void InsertRange(int index, IEnumerable<T> itemsToInsert)
            {
                // we are making asumption that the underlying storage is a array list (List<>), so InsertRange() is available
                // if not, will resort to InsertAt() repeatedly which will have scalability issue
                List<T> x = this.Items as List<T>;
                if (x != null)
                {
                    // we are making asumption that the underlying storage is an array list (List<>)
                    x.InsertRange(index, itemsToInsert);
                    this.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
                }
                else
                {
                    // if not a list, we will resort to repeatedly calling Insert, which has scalability issues
                    foreach (var i in itemsToInsert)
                    {
                        this.Insert(index++, i);
                    }
                }
            }

            /// <summary>
            /// Bulk removal; side effect is the view will be refreshed
            /// </summary>
            /// <param name="index">The starting index from where to remove</param>
            /// <param name="count">the number of items to remove</param>
            public void RemoveRange(int index, int count)
            {
                // we are making asumption that the underlying storage is an array list (List<>), so RemoveRange() is available
                // if not, will resort to RemoveAt() repeatedly which will have scalability issue
                List<T> x = this.Items as List<T>;

                if (x != null)
                {

                    x.RemoveRange(index, count);
                    this.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
                }
                else
                {
                    for (int i = 0; i < count; ++i)
                    {
                        this.RemoveAt(index);
                    }
                }
            }
        }
    }
}
