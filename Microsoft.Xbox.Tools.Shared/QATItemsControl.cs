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
    public class QATItemsControl : ItemsControl
    {
        public static readonly DependencyProperty ButtonMarginProperty = DependencyProperty.Register(
            "ButtonMargin", typeof(Thickness), typeof(QATItemsControl));

        public Thickness ButtonMargin
        {
            get { return (Thickness)GetValue(ButtonMarginProperty); }
            set { SetValue(ButtonMarginProperty, value); }
        }
        
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is Button;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new Button();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            BindingOperations.SetBinding(element, Button.MarginProperty, new Binding { Source = this, Path = new PropertyPath(ButtonMarginProperty) });

            if (item is QATButtonDefinition)
            {
                BindingOperations.SetBinding(element, Button.ContentTemplateProperty, new Binding { Source = item, Path = new PropertyPath("ContentTemplate") });
                BindingOperations.SetBinding(element, Button.ToolTipProperty, new Binding { Source = item, Path = new PropertyPath("ToolTip") });
                BindingOperations.SetBinding(element, Button.CommandProperty, new Binding { Source = item, Path = new PropertyPath("Command") });
            }
        }
    }

    public class QATButtonDefinition
    {
        public DataTemplate ContentTemplate { get; set; }
        public string ToolTip { get; set; }
        public ICommand Command { get; set; }
    }
}
