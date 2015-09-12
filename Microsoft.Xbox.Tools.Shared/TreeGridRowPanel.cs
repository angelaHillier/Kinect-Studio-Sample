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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class TreeGridRowPanel : Panel
    {
        double totalHeight;
        Dictionary<TreeGridColumn, TreeGridCell> cells { get; set; }
        Binding rowDataBinding;
        TreeGridRow ownerRow;

        public TreeGridRowPanel()
        {
            this.cells = new Dictionary<TreeGridColumn, TreeGridCell>();
        }

        TreeGridCell CreateCell(TreeGridColumn column)
        {
            TreeGridCell cc;

            if (this.OwnerRow.IsHeaderRow)
            {
                cc = new TreeGridHeaderCell { Column = column, OwnerRow = this.OwnerRow };
                cc.DataContext = column;
                cc.SetBinding(TreeGridCell.ContentTemplateProperty, new Binding { Source = column, Path = new PropertyPath(TreeGridColumn.HeaderTemplateProperty) });
                cc.SetBinding(TreeGridCell.ContentProperty, new Binding { Source = column, Path = new PropertyPath(TreeGridColumn.HeaderProperty) });
                cc.SetBinding(TreeGridHeaderCell.ContextMenuProperty, new Binding { Source = column, Path = new PropertyPath(TreeGridColumn.ContextMenuProperty) });
                cc.Loaded += OnHeaderCellLoaded;
            }
            else
            {
                cc = new TreeGridCell { Column = column, OwnerRow = this.OwnerRow };
                cc.SetBinding(TreeGridCell.DataContextProperty, rowDataBinding);
                cc.SetBinding(TreeGridCell.ContentProperty, column.CellBinding ?? this.rowDataBinding);
                cc.SetBinding(TreeGridCell.ContentTemplateProperty, new Binding { Source = column, Path = new PropertyPath(TreeGridColumn.CellTemplateProperty) });
                column.PropertyChanged += OnColumnPropertyChanged;
            }

            this.Children.Add(cc);
            return cc;
        }

        internal TreeGridRow OwnerRow
        {
            get
            {
                return this.ownerRow;
            }
            set
            {
                this.ownerRow = value;
                if (!this.ownerRow.IsHeaderRow)
                {
                    this.rowDataBinding = new Binding { Source = this.ownerRow, Path = new PropertyPath(TreeGridRow.RowDataProperty) };
                }

                this.ownerRow.OwnerGrid.Columns.CollectionChanged += OnColumnsChanged;
                UpdateCellTable();
            }
        }

        void OnHeaderCellLoaded(object sender, RoutedEventArgs e)
        {
            var cell = sender as TreeGridHeaderCell;

            if (cell != null)
            {
                cell.Loaded -= OnHeaderCellLoaded;
            }

            this.OwnerRow.OwnerGrid.InvalidateRowLayout(true);
        }

        internal void UpdateCellTable()
        {
            var existing = new HashSet<TreeGridColumn>(this.cells.Keys);

            foreach (var column in this.OwnerRow.OwnerGrid.Columns)
            {
                if (existing.Contains(column))
                {
                    existing.Remove(column);
                }
                else
                {
                    this.cells.Add(column, CreateCell(column));
                }
            }

            foreach (var column in existing)
            {
                RemoveCell(column);
            }

            bool first = true;
            foreach (var column in this.OwnerRow.OwnerGrid.Columns)
            {
                this.cells[column].IsLeftmost = first;
                first = false;
            }
        }

        void OnColumnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var column = sender as TreeGridColumn;
            TreeGridCell cc;

            if (column != null && e.PropertyName == "CellBinding" && this.cells.TryGetValue(column, out cc))
            {
                cc.SetBinding(TreeGridCell.ContentProperty, column.CellBinding ?? rowDataBinding);
            }
        }

        internal void OnRowRemoved()
        {
            this.ownerRow.OwnerGrid.Columns.CollectionChanged -= OnColumnsChanged;
            if (!this.ownerRow.IsHeaderRow)
            {
                foreach (var column in this.cells.Keys)
                {
                    column.PropertyChanged -= OnColumnPropertyChanged;
                }
            }

            this.cells.Clear();
            this.ownerRow = null;
        }

        void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count == 1)
            {
                var newColumn = e.NewItems[0] as TreeGridColumn;
                Debug.Assert(!this.cells.ContainsKey(newColumn), "How can this be?  Added the same column more than once?  Don't do that!");
                this.cells.Add(newColumn, CreateCell(newColumn));
                if (this.ownerRow.OwnerGrid.Columns.Count == 1)
                {
                    this.cells[newColumn].IsLeftmost = true;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.Count == 1)
            {
                var oldColumn = e.OldItems[0] as TreeGridColumn;
                Debug.Assert(this.cells.ContainsKey(oldColumn), "How can this be?  Removing a column we don't know about?  Bug!");
                RemoveCell(oldColumn);
                if (this.ownerRow.OwnerGrid.Columns.Count > 0)
                {
                    this.cells[this.ownerRow.OwnerGrid.Columns[0]].IsLeftmost = true;
                }
            }
            else
            {
                // Only the above two cases are optimized -- everything else is a reset (which isn't that bad...)
                UpdateCellTable();
            }

            this.OwnerRow.OwnerGrid.InvalidateRowLayout(true);
        }

        void RemoveCell(TreeGridColumn oldColumn)
        {
            this.Children.Remove(this.cells[oldColumn]);
            oldColumn.PropertyChanged -= OnColumnPropertyChanged;
            this.cells.Remove(oldColumn);
        }

        public bool TryGetDesiredCellWidth(TreeGridColumn column, out double desiredWidth)
        {
            TreeGridCell cc;

            if (this.cells.TryGetValue(column, out cc) && cc.IsLoaded)
            {
                desiredWidth = cc.DesiredSize.Width;
                return true;
            }

            desiredWidth = 0;
            return false;
        }


        protected override Size MeasureOverride(Size constraint)
        {
            double totalWidth = 0;

            totalHeight = this.MinHeight;

            if (this.ownerRow != null)
            {
                foreach (var child in this.ownerRow.OwnerGrid.Columns)
                {
                    var cc = this.cells[child];
                    var cellConstraint = constraint;

                    if (!double.IsNaN(child.Width))
                    {
                        cellConstraint.Width = child.Width;
                    }

                    cc.Measure(cellConstraint);
                    totalWidth += cc.DesiredSize.Width;
                    totalHeight = Math.Max(totalHeight, cc.DesiredSize.Height);
                }
            }
            else if (!double.IsInfinity(constraint.Width))
            {
                totalWidth = constraint.Width;
            }

            return new Size(totalWidth, totalHeight);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            double runningX = 0;

            if (this.ownerRow != null)
            {
                foreach (var child in this.ownerRow.OwnerGrid.Columns)
                {
                    var cc = this.cells[child];
                    cc.Arrange(new Rect(runningX, 0, child.ActualWidth, totalHeight));
                    runningX += child.ActualWidth;
                }
            }
            else
            {
                runningX = arrangeBounds.Width;
            }

            return new Size(runningX, totalHeight);
        }
    }
}
