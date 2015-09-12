//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.Xbox.Tools.Shared
{
    public class BladePage : HeaderedContentControl
    {
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof(bool), typeof(BladePage), new FrameworkPropertyMetadata(OnIsSelectedChanged));

        public static readonly RoutedEvent SelectedEvent = EventManager.RegisterRoutedEvent("Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(BladePage));

        FrameworkElement headerPart;
        FrameworkElement lastElementWithFocus;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.headerPart = this.Template.FindName("PART_Header", this) as FrameworkElement;

            if (this.headerPart == null)
            {
                // You haven't styled/templated the control correctly.  Make sure there's a PART_Header defined.
                throw new InvalidOperationException();
            }

            this.headerPart.MouseDown += OnHeaderMouseDown;
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        internal double DesiredHeaderHeight { get { return this.headerPart.DesiredSize.Height; } }

        void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.IsSelected = true;
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            if (this.IsKeyboardFocusWithin)
            {
                this.lastElementWithFocus = e.NewFocus as FrameworkElement;
            }
        }

        static void OnIsSelectedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            BladePage page = obj as BladePage;

            if (page != null)
            {
                var parentControl = page.FindParent<BladeControl>();

                if (page.IsSelected)
                {
                    page.RaiseEvent(new RoutedEventArgs(SelectedEvent, page));
                    if (parentControl != null && parentControl.IsKeyboardFocusWithin && 
                        page.lastElementWithFocus != null && page.lastElementWithFocus.IsLoaded)
                    {
                        page.lastElementWithFocus.Focus();
                    }
                }
            }
        }
    }
}
