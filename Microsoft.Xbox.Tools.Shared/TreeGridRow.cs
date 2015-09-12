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
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class TreeGridRow : Control
    {
        public static readonly DependencyProperty RowDataProperty = DependencyProperty.Register(
            "RowData", typeof(object), typeof(TreeGridRow), new FrameworkPropertyMetadata(OnRowDataChanged));

        public static readonly DependencyProperty IsExpandableProperty = DependencyProperty.Register(
            "IsExpandable", typeof(bool), typeof(TreeGridRow));

        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            "IsExpanded", typeof(bool), typeof(TreeGridRow), new FrameworkPropertyMetadata(OnIsExpandedChanged));

        public static readonly DependencyProperty IsCurrentProperty = DependencyProperty.Register(
            "IsCurrent", typeof(bool), typeof(TreeGridRow), new FrameworkPropertyMetadata(OnIsCurrentChanged));

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof(bool), typeof(TreeGridRow));

        public static readonly DependencyProperty ExpansionLevelProperty = DependencyProperty.Register(
            "ExpansionLevel", typeof(int), typeof(TreeGridRow));

        public static readonly DependencyProperty GridHasKeyboardFocusProperty = DependencyProperty.Register(
            "GridHasKeyboardFocus", typeof(bool), typeof(TreeGridRow));

        // This event is fired only when a row becomes current -- it is used by the tree grid to maintain at most one current row.
        public static readonly RoutedEvent CurrentItemChangedEvent = EventManager.RegisterRoutedEvent("CurrentItemChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeGridRow));

        public static readonly RoutedEvent RowClickedEvent = EventManager.RegisterRoutedEvent("RowClicked", RoutingStrategy.Bubble, typeof(EventHandler<RowClickedEventArgs>), typeof(TreeGridRow));

        TreeGrid treeGrid;
        TreeGridRowPanel panel;
        TreeGridNodeReference nodeReference;
        bool ignoreIsExpandedChanges;
        internal int lastLayoutPassUsed;
        internal TreeGridRow prevLink;
        internal TreeGridRow nextLink;
        internal bool isPartiallyVisible;

        static TreeGridRow()
        {
            MinHeightProperty.OverrideMetadata(typeof(TreeGridRow), new FrameworkPropertyMetadata(14d));
        }

        public TreeGridRow(TreeGrid treeGrid, bool isHeaderRow)
        {
            this.treeGrid = treeGrid;
            this.IsHeaderRow = isHeaderRow;

            SetBinding(Canvas.LeftProperty, new Binding
            {
                Source = treeGrid,
                Path = new PropertyPath(TreeGrid.HorizontalOffsetProperty),
                Converter = new NegatingConverter()
            });

            SetBinding(GridHasKeyboardFocusProperty, new Binding { Source = treeGrid, Path = new PropertyPath(TreeGrid.IsKeyboardFocusWithinProperty) });

            this.MouseDown += OnMouseDown;
        }

        public TreeGrid OwnerGrid { get { return this.treeGrid; } }

        public bool IsHeaderRow { get; private set; }

        public int ExpansionLevel
        {
            get { return (int)GetValue(ExpansionLevelProperty); }
            set { SetValue(ExpansionLevelProperty, value); }
        }

        public object RowData
        {
            get { return (object)GetValue(RowDataProperty); }
            set { SetValue(RowDataProperty, value); }
        }

        public bool IsExpandable
        {
            get { return (bool)GetValue(IsExpandableProperty); }
            set { SetValue(IsExpandableProperty, value); }
        }

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public bool IsCurrent
        {
            get { return (bool)GetValue(IsCurrentProperty); }
            set { SetValue(IsCurrentProperty, value); }
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public bool GridHasKeyboardFocus
        {
            get { return (bool)GetValue(GridHasKeyboardFocusProperty); }
            set { SetValue(GridHasKeyboardFocusProperty, value); }
        }

        public TreeGridRowPanel Panel { get { return this.panel; } }

        // Consider either making this a method that returns a clone, or build the notion of immutability into TreeGridNodeReference.
        public TreeGridNodeReference NodeReference { get { return nodeReference; } }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.panel = this.Template.FindName("PART_RowPanel", this) as TreeGridRowPanel;
            this.panel.OwnerRow = this;   // If you crash here, fix your template / make sure it's being used
        }

        void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!this.IsHeaderRow)
            {
                this.RaiseEvent(new RowClickedEventArgs(this, e));
            }
        }

        internal void UpdateRowData(TreeGridNodeReference newNodeReference, bool isCurrent, bool isSelected)
        {
            if (this.nodeReference != null)
            {
                this.nodeReference.Dispose();
            }

            this.ignoreIsExpandedChanges = true;
            try
            {
                // NOTE:  Since we keep this reference we must Clone it.
                this.nodeReference = newNodeReference.Clone();
                this.IsCurrent = isCurrent;
                this.IsSelected = isSelected;
                this.ExpansionLevel = newNodeReference.ExpansionLevel;
                this.IsExpanded = newNodeReference.IsExpanded;
                this.RowData = newNodeReference.Item;
            }
            finally
            {
                this.ignoreIsExpandedChanges = false;
            }
        }

        internal void UpdateExpansionState()
        {
            // The expansion state is updated manually on each of the grid's "paint" passes.
            // Avoids binding / observing expansion state changes per node.
            this.ignoreIsExpandedChanges = true;
            try
            {
                this.IsExpanded = this.nodeReference.IsExpanded;
                this.IsExpandable = this.treeGrid.HasChildrenFunc(this.RowData);
            }
            finally
            {
                this.ignoreIsExpandedChanges = false;
            }
        }

        internal void OnRowRemoved()
        {
            if (this.panel != null)
            {
                this.panel.OnRowRemoved();
            }

            if (this.nodeReference != null)
            {
                this.nodeReference.Dispose();
                this.nodeReference = null;
            }
        }

        internal bool TryGetDesiredCellWidth(TreeGridColumn column, out double desiredWidth)
        {
            return this.panel.TryGetDesiredCellWidth(column, out desiredWidth);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.HeightChanged && this.OwnerGrid != null)
            {
                this.OwnerGrid.OnRowHeightChanged(this);
            }
        }

        static void OnIsExpandedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TreeGridRow row = obj as TreeGridRow;

            if (row != null && row.nodeReference != null && !row.ignoreIsExpandedChanges)
            {
                //row.ParentNode.ToggleChildExpansion(row.ChildIndex, row.treeGrid.ChildrenFunc);
                row.nodeReference.IsExpanded = !row.nodeReference.IsExpanded;
            }
        }

        static void OnRowDataChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TreeGridRow row = obj as TreeGridRow;

            if (row != null)
            {
                if (!row.IsHeaderRow) 
                {
                    row.IsExpandable = row.treeGrid.HasChildrenFunc(row.RowData);
                }
            }
        }

        static void OnIsCurrentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TreeGridRow row = obj as TreeGridRow;

            if (row != null)
            {
                if (row.IsCurrent)
                    row.RaiseEvent(new RoutedEventArgs(CurrentItemChangedEvent, row));
            }
        }

        class NegatingConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is double)
                    return -(double)value;

                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class LevelToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int)
            {
                return new Thickness((int)value * 12, 0, 4, 0);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RowClickedEventArgs : RoutedEventArgs
    {
        public TreeGridRow Row { get; private set; }
        public ModifierKeys Modifiers { get; private set; }
        public MouseButtonEventArgs OriginalArgs { get; private set; }

        public RowClickedEventArgs(TreeGridRow row, MouseButtonEventArgs originalArgs)
            : base(TreeGridRow.RowClickedEvent, row)
        {
            this.Row = row;
            this.OriginalArgs = originalArgs;
            this.Modifiers = Keyboard.Modifiers;
        }
    }
}
