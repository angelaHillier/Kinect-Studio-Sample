//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Xbox.Tools.Shared
{
    public class TabDropTargetWindow : Window
    {
        public static readonly DependencyProperty TargetNodeProperty = DependencyProperty.Register(
            "TargetNode", typeof(TabNode), typeof(TabDropTargetWindow));

        public static readonly DependencyProperty HorizontalParentNodeProperty = DependencyProperty.Register(
            "HorizontalParentNode", typeof(TabNode), typeof(TabDropTargetWindow), new FrameworkPropertyMetadata(OnParentNodeChanged));

        static readonly DependencyPropertyKey isHorizontalParentVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
            "IsHorizontalParentVisible", typeof(bool), typeof(TabDropTargetWindow), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsHorizontalParentVisibleProperty = isHorizontalParentVisiblePropertyKey.DependencyProperty;

        public static readonly DependencyProperty VerticalParentNodeProperty = DependencyProperty.Register(
            "VerticalParentNode", typeof(TabNode), typeof(TabDropTargetWindow), new FrameworkPropertyMetadata(OnParentNodeChanged));

        static readonly DependencyPropertyKey isVerticalParentVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
            "IsVerticalParentVisible", typeof(bool), typeof(TabDropTargetWindow), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsVerticalParentVisibleProperty = isVerticalParentVisiblePropertyKey.DependencyProperty;

        public static readonly DependencyProperty IsTabbedSpotVisibleProperty = DependencyProperty.Register(
            "IsTabbedSpotVisible", typeof(bool), typeof(TabDropTargetWindow));

        public static readonly DependencyProperty AreDockSpotsVisibleProperty = DependencyProperty.Register(
            "AreDockSpotsVisible", typeof(bool), typeof(TabDropTargetWindow), new FrameworkPropertyMetadata(true));

        public TabNode TargetNode
        {
            get { return (TabNode)GetValue(TargetNodeProperty); }
            set { SetValue(TargetNodeProperty, value); }
        }

        public TabNode HorizontalParentNode
        {
            get { return (TabNode)GetValue(HorizontalParentNodeProperty); }
            set { SetValue(HorizontalParentNodeProperty, value); }
        }

        public bool IsHorizontalParentVisible
        {
            get { return (bool)GetValue(IsHorizontalParentVisibleProperty); }
            private set { SetValue(isHorizontalParentVisiblePropertyKey, value); }
        }

        public TabNode VerticalParentNode
        {
            get { return (TabNode)GetValue(VerticalParentNodeProperty); }
            set { SetValue(VerticalParentNodeProperty, value); }
        }

        public bool IsVerticalParentVisible
        {
            get { return (bool)GetValue(IsVerticalParentVisibleProperty); }
            private set { SetValue(isVerticalParentVisiblePropertyKey, value); }
        }

        public bool IsTabbedSpotVisible
        {
            get { return (bool)GetValue(IsTabbedSpotVisibleProperty); }
            set { SetValue(IsTabbedSpotVisibleProperty, value); }
        }

        public bool AreDockSpotsVisible
        {
            get { return (bool)GetValue(AreDockSpotsVisibleProperty); }
            set { SetValue(AreDockSpotsVisibleProperty, value); }
        }

        static void OnParentNodeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TabDropTargetWindow target = obj as TabDropTargetWindow;

            if (target != null)
            {
                target.IsHorizontalParentVisible = target.HorizontalParentNode != null;
                target.IsVerticalParentVisible = target.VerticalParentNode != null;
            }
        }
    }

    public class BooleanFalseToHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool && !((bool)value))
            {
                return Visibility.Hidden;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
