//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.Xbox.Tools.Shared
{
    public class TreeGridColumn : DependencyObject, INotifyPropertyChanged
    {
        public static readonly DependencyProperty CellTemplateProperty = DependencyProperty.Register(
            "CellTemplate", typeof(DataTemplate), typeof(TreeGridColumn));

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header", typeof(object), typeof(TreeGridColumn));

        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            "HeaderTemplate", typeof(DataTemplate), typeof(TreeGridColumn));

        public static readonly DependencyProperty DisplaysHierarchyProperty = DependencyProperty.Register(
            "DisplaysHierarchy", typeof(bool), typeof(TreeGridColumn));

        public static readonly DependencyProperty MinWidthProperty = DependencyProperty.Register(
            "MinWidth", typeof(double), typeof(TreeGridColumn), new FrameworkPropertyMetadata(20d, OnMeasureAffectingPropertyChanged));

        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
            "Width", typeof(double), typeof(TreeGridColumn), new FrameworkPropertyMetadata(double.NaN, OnMeasureAffectingPropertyChanged));

        public static readonly DependencyProperty ContextMenuProperty = DependencyProperty.Register(
            "ContextMenu", typeof(ContextMenu), typeof(TreeGridColumn));

        double actualWidth;
        BindingBase cellBinding;

        public double ActualWidth
        {
            get
            {
                return this.actualWidth;
            }
            set
            {
                if (this.actualWidth != value)
                {
                    this.actualWidth = value;
                    Notify("ActualWidth");
                }
            }
        }

        public bool IsWidthLocked { get; set; }

        public BindingBase CellBinding
        {
            get
            {
                return this.cellBinding;
            }
            set
            {
                if (this.cellBinding != value)
                {
                    this.cellBinding = value;
                    Notify("CellBinding");
                }
            }
        }

        public bool DisplaysHierarchy
        {
            get { return (bool)GetValue(DisplaysHierarchyProperty); }
            set { SetValue(DisplaysHierarchyProperty, value); }
        }

        internal TreeGrid Owner { get; set; }

        public DataTemplate CellTemplate
        {
            get { return (DataTemplate)GetValue(CellTemplateProperty); }
            set { SetValue(CellTemplateProperty, value); }
        }

        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        public double MinWidth
        {
            get { return (double)GetValue(MinWidthProperty); }
            set { SetValue(MinWidthProperty, value); }
        }

        public double Width
        {
            get { return (double)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        public ContextMenu ContextMenu
        {
            get { return (ContextMenu)GetValue(ContextMenuProperty); }
            set { SetValue(ContextMenuProperty, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void Notify(string property)
        {
            var handler = this.PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        static void OnMeasureAffectingPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TreeGridColumn col = obj as TreeGridColumn;

            if (col != null)
            {
                if (e.Property == WidthProperty && double.IsNaN((double)e.NewValue))
                {
                    col.IsWidthLocked = false;
                }

                if (col.Owner != null)
                {
                    col.Owner.InvalidateRowLayout(true);
                }
            }
        }

    }
}