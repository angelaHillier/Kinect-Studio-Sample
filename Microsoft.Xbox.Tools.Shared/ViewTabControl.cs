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
using System.Windows.Markup;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ViewTabControl : TabControl
    {
        public static readonly DependencyProperty ParentTabControlHasFocusProperty = DependencyProperty.RegisterAttached(
            "ParentTabControlHasFocus", typeof(bool), typeof(ViewTabControl), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty ActiveContentProperty = DependencyProperty.Register(
            "ActiveContent", typeof(object), typeof(ViewTabControl), new FrameworkPropertyMetadata(OnActiveContentChanged));

        public static readonly RoutedEvent ActiveContentChangedEvent = EventManager.RegisterRoutedEvent("ActiveContentChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ViewTabControl));

        ContentPresenter contentPresenter;

        public object ActiveContent
        {
            get { return (object)GetValue(ActiveContentProperty); }
            set { SetValue(ActiveContentProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.contentPresenter = GetTemplateChild("PART_SelectedContentHost") as ContentPresenter;
            if (this.contentPresenter != null)
            {
                this.SetBinding(ActiveContentProperty, new Binding { Source = this.contentPresenter, Path = new PropertyPath(ContentPresenter.ContentProperty) });
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            ToolsUIApplication.Instance.RequestViewBindingUpdate();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            var tabItem = element as TabItem;
            var site = item as ViewSite;

            if (tabItem != null && site != null)
            {
                tabItem.SetBinding(TabItem.HeaderProperty, new Binding { Source = site.ViewSource, Path = new PropertyPath(ViewSource.TitleProperty) });
                tabItem.Content = site;
                tabItem.ContentTemplate = site.Template;
                tabItem.SetBinding(ParentTabControlHasFocusProperty, new Binding { Source = this, Path = new PropertyPath(IsKeyboardFocusWithinProperty) });
            }
            else
            {
                base.PrepareContainerForItemOverride(element, item);
            }
        }

        public static bool GetParentTabControlHasFocus(DependencyObject obj)
        {
            return (bool)obj.GetValue(ParentTabControlHasFocusProperty);
        }

        public static void SetParentTabControlHasFocus(DependencyObject obj, bool value)
        {
            obj.SetValue(ParentTabControlHasFocusProperty, value);
        }

        static void OnActiveContentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ViewTabControl ctrl = obj as ViewTabControl;

            if (ctrl != null)
            {
                ctrl.RaiseEvent(new RoutedEventArgs(ActiveContentChangedEvent));

                var item = ctrl.SelectedItem as ViewSite;

                if (item != null && item.View != null && ctrl.IsLoaded)
                {
                    item.View.Activate();
                }
            }
        }
    }
}
