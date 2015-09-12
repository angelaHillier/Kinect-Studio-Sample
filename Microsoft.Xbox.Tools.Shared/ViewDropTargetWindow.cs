//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ViewDropTargetWindow : Window
    {
        public static readonly DependencyProperty TargetSlotProperty = DependencyProperty.Register(
            "TargetSlot", typeof(Slot), typeof(ViewDropTargetWindow), new FrameworkPropertyMetadata(OnTargetSlotChanged));

        public static readonly DependencyProperty RootSlotProperty = DependencyProperty.Register(
            "RootSlot", typeof(Slot), typeof(ViewDropTargetWindow));

        static readonly DependencyPropertyKey horizontalParentSlotPropertyKey = DependencyProperty.RegisterReadOnly(
            "HorizontalParentSlot", typeof(Slot), typeof(ViewDropTargetWindow), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty HorizontalParentSlotProperty = horizontalParentSlotPropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey isHorizontalParentVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
            "IsHorizontalParentVisible", typeof(bool), typeof(ViewDropTargetWindow), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsHorizontalParentVisibleProperty = isHorizontalParentVisiblePropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey verticalParentSlotPropertyKey = DependencyProperty.RegisterReadOnly(
            "VerticalParentSlot", typeof(Slot), typeof(ViewDropTargetWindow), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty VerticalParentSlotProperty = verticalParentSlotPropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey isVerticalParentVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
            "IsVerticalParentVisible", typeof(bool), typeof(ViewDropTargetWindow), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsVerticalParentVisibleProperty = isVerticalParentVisiblePropertyKey.DependencyProperty;

        public static readonly DependencyProperty IsTabbedSpotVisibleProperty = DependencyProperty.Register(
            "IsTabbedSpotVisible", typeof(bool), typeof(ViewDropTargetWindow));

        public static readonly DependencyProperty AreDockSpotsVisibleProperty = DependencyProperty.Register(
            "AreDockSpotsVisible", typeof(bool), typeof(ViewDropTargetWindow), new FrameworkPropertyMetadata(false));

        public Slot TargetSlot
        {
            get { return (Slot)GetValue(TargetSlotProperty); }
            set { SetValue(TargetSlotProperty, value); }
        }

        public Slot RootSlot
        {
            get { return (Slot)GetValue(RootSlotProperty); }
            set { SetValue(RootSlotProperty, value); }
        }

        public Slot HorizontalParentSlot
        {
            get { return (Slot)GetValue(HorizontalParentSlotProperty); }
            private set { SetValue(horizontalParentSlotPropertyKey, value); }
        }

        public bool IsHorizontalParentVisible
        {
            get { return (bool)GetValue(IsHorizontalParentVisibleProperty); }
            private set { SetValue(isHorizontalParentVisiblePropertyKey, value); }
        }

        public Slot VerticalParentSlot
        {
            get { return (Slot)GetValue(VerticalParentSlotProperty); }
            private set { SetValue(verticalParentSlotPropertyKey, value); }
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

        public ViewDropTargetWindow()
        {
            this.AllowsTransparency = true;
            this.WindowStyle = WindowStyle.None;
        }

        void OnTargetSlotChanged()
        {
            if (this.TargetSlot == null)
            {
                this.IsVerticalParentVisible = false;
                this.IsHorizontalParentVisible = false;
            }
            else
            {
                var parent = (this.TargetSlot == this.RootSlot) ? null : this.TargetSlot.Parent;
                var grandparent = parent != null && parent != this.RootSlot ? parent.Parent : null;

                if (parent != null && parent.Orientation == Orientation.Vertical)
                {
                    this.VerticalParentSlot = grandparent;
                }
                else
                {
                    this.VerticalParentSlot = parent;
                }

                if (parent != null && parent.Orientation == Orientation.Horizontal)
                {
                    this.HorizontalParentSlot = grandparent;
                }
                else
                {
                    this.HorizontalParentSlot = parent;
                }

                this.IsVerticalParentVisible = this.VerticalParentSlot != null;
                this.IsHorizontalParentVisible = this.HorizontalParentSlot != null;
            }
        }

        static void OnTargetSlotChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ViewDropTargetWindow window = obj as ViewDropTargetWindow;

            if (window != null)
            {
                window.OnTargetSlotChanged();
            }
        }

    }
}
