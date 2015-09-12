//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ActivatableTabItem : TabItem, IActivationSite
    {
        static readonly DependencyPropertyKey tabControlParentPropertyKey = DependencyProperty.RegisterReadOnly(
            "TabControlParent", typeof(ActivatableTabControl), typeof(ActivatableTabItem), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty TabControlParentProperty = tabControlParentPropertyKey.DependencyProperty;

        public static RoutedEvent ActivatedEvent = EventManager.RegisterRoutedEvent("Activated", RoutingStrategy.Bubble, typeof(EventHandler<RoutedEventArgs>), typeof(ActivatableTabItem));
        public static RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Bubble, typeof(EventHandler<RoutedEventArgs>), typeof(ActivatableTabItem));

        public static readonly DependencyProperty ViewCreatorProperty = DependencyProperty.Register(
            "ViewCreator", typeof(IViewCreationCommand), typeof(ActivatableTabItem));

        public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(
            "View", typeof(View), typeof(ActivatableTabItem));

        public static readonly DependencyProperty TabItemProperty = DependencyProperty.RegisterAttached(
            "TabItem", typeof(ActivatableTabItem), typeof(ActivatableTabItem));

        public ActivatableTabItem()
        {
            this.CommandBindings.Add(new CommandBinding(SplitTabsControl.CloseViewCommand, OnCloseViewExecuted));
        }

        public IViewCreationCommand ViewCreator
        {
            get { return (IViewCreationCommand)GetValue(ViewCreatorProperty); }
            set { SetValue(ViewCreatorProperty, value); }
        }

        public View View
        {
            get { return (View)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        public ActivatableTabControl TabControlParent
        {
            get { return (ActivatableTabControl)GetValue(TabControlParentProperty); }
            set { SetValue(tabControlParentPropertyKey, value); }
        }

        void OnCloseViewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
        }

        void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (this.View != null)
                {
                    this.View.Activate();
                }

                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    TabItemDragManager.BeginDrag(this, e);
                }
            }), DispatcherPriority.Background);

            e.Handled = true;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var header = this.Template.FindName("PART_Header", this) as FrameworkElement;

            if (header != null)
            {
                header.MouseLeftButtonDown += OnHeaderMouseLeftButtonDown;
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var r = base.MeasureOverride(constraint);
            r = new Size(r.Width, Math.Ceiling(r.Height));
            return r;
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            if (this.IsKeyboardFocusWithin)
                this.RaiseEvent(new RoutedEventArgs(ActivatedEvent, this));

            base.OnIsKeyboardFocusWithinChanged(e);
        }

        public static ActivatableTabItem GetTabItem(DependencyObject obj)
        {
            return (ActivatableTabItem)obj.GetValue(TabItemProperty);
        }

        public static void SetTabItem(DependencyObject obj, ActivatableTabItem value)
        {
            obj.SetValue(TabItemProperty, value);
        }

        public IActivationSite ParentSite { get; set; }

        public void BubbleActivation(object child)
        {
            if (object.ReferenceEquals(child, this.View) && (this.ParentSite != null))
            {
                this.ParentSite.BubbleActivation(this);
            }
        }

        public void TunnelActivation()
        {
            // Tunnel activation is called when a parent site (typically a tab control) has switched
            // to make this view active.  It can be called as a result of View.Activate(), but the
            // call is idempotent here in that case.
            if (this.View != null)
            {
                this.View.Activate();
            }
        }

        public void NotifyActivation(object child)
        {
            if (object.ReferenceEquals(child, this.View) && (this.ParentSite != null))
            {
                this.ParentSite.NotifyActivation(this);
            }
        }
    }
}
