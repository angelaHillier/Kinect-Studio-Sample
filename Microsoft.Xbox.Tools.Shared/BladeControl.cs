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
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.Xbox.Tools.Shared
{
    public class BladeControl : Selector
    {
        public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(
            "ContentTemplate", typeof(DataTemplate), typeof(BladeControl));

        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            "HeaderTemplate", typeof(DataTemplate), typeof(BladeControl));

        BladePanel bladePanel;

        public BladeControl()
        {
            this.AddHandler(BladePage.SelectedEvent, (RoutedEventHandler)OnBladePageSelected);
        }

        public DataTemplate ContentTemplate
        {
            get { return (DataTemplate)GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new BladePage();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is BladePage;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            BindingOperations.SetBinding(element, BladePage.HeaderTemplateProperty, new Binding { Source = this, Path = new PropertyPath(HeaderTemplateProperty) });
            BindingOperations.SetBinding(element, BladePage.ContentTemplateProperty, new Binding { Source = this, Path = new PropertyPath(ItemTemplateProperty) });
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.bladePanel = this.Template.FindName("PART_BladePanel", this) as BladePanel;
        }

        void OnBladePageSelected(object sender, RoutedEventArgs e)
        {
            BladePage page = e.OriginalSource as BladePage;

            if (page != null)
            {
                this.SelectedIndex = this.ItemContainerGenerator.IndexFromContainer(page);
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            for (int index = 0; index < this.Items.Count; index++)
            {
                var page = this.ItemContainerGenerator.ContainerFromIndex(index) as BladePage;

                if (page != null)
                    page.IsSelected = (index == this.SelectedIndex);
            }

            if (this.bladePanel != null)
            {
                this.bladePanel.InvalidateArrange();
            }

            base.OnSelectionChanged(e);
        }
    }
}
