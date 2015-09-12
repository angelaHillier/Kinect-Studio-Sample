//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ActivatableTabControl : TabControl, IActivationSite
    {
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            "IsActive", typeof(bool), typeof(ActivatableTabControl));

        public static readonly DependencyProperty TabNodeProperty = DependencyProperty.Register(
            "TabNode", typeof(TabNode), typeof(ActivatableTabControl));

        ActivatableTabItem lastActiveChildSite;

        public TabNode TabNode
        {
            get { return (TabNode)GetValue(TabNodeProperty); }
            set { SetValue(TabNodeProperty, value); }
        }

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public ActivatableTabControl()
        {
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ActivatableTabItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ActivatableTabItem;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            var activatableItem = element as ActivatableTabItem;

            if (activatableItem != null)
            {
                activatableItem.ParentSite = this;
                activatableItem.TabControlParent = this;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!this.IsKeyboardFocusWithin)
            {
                var item = this.SelectedItem as ActivatableTabItem;

                if (item != null && item.View != null)
                {
                    item.View.Activate();
                    e.Handled = true;
                }
            }

            base.OnMouseDown(e);
        }

        public IActivationSite ParentSite { get; set; }

        public void BubbleActivation(object child)
        {
            var tabItem = child as ActivatableTabItem;

            if (tabItem != null && this.Items.Contains(tabItem))
            {
                this.lastActiveChildSite = tabItem;
                this.SelectedIndex = this.Items.IndexOf(tabItem);
                this.ParentSite.BubbleActivation(this);
            }
        }

        public void TunnelActivation()
        {
            if (this.lastActiveChildSite == null)
            {
                this.lastActiveChildSite = this.SelectedItem as ActivatableTabItem;
            }

            if (this.lastActiveChildSite != null)
            {
                this.lastActiveChildSite.TunnelActivation();
            }
        }

        public void NotifyActivation(object child)
        {
            var tabItem = child as ActivatableTabItem;

            if (tabItem != null)
            {
                this.lastActiveChildSite = tabItem;
                this.ParentSite.NotifyActivation(this);
            }
        }
    }
}
