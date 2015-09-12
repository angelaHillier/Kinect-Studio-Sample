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
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class OpenTabControl : TabControl
    {
        public static readonly RoutedEvent TabChangedEvent = EventManager.RegisterRoutedEvent("TabChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(OpenTabControl));
        public static readonly RoutedCommand ShortcutCommand = new RoutedCommand("Shortcut", typeof(OpenTabControl));

        public OpenTabControl()
        {
            this.CommandBindings.Add(new CommandBinding(ShortcutCommand, OnShortcutCommandExecuted, OnShortcutCommandCanExecute));
        }

        void OnShortcutCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var tabData = e.Parameter as OpenTabItemData;

            if (tabData != null)
            {
                this.SelectedItem = tabData;

                if (tabData.SubMode != null)
                {
                    var manager = ShortcutManager.GetInstance(this);

                    if (manager != null)
                    {
                        manager.PushUISubMode(tabData.SubMode);
                    }
                }
            }
        }

        void OnShortcutCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var tabData = e.Parameter as OpenTabItemData;

            if (tabData != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            // We fire a custom event for animation purposes, because animation picks up on unhandled selection changed events
            // from any contained element...
            RaiseEvent(new RoutedEventArgs(TabChangedEvent));
        }
    }

    public class OpenTabItemData : DependencyObject
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content", typeof(object), typeof(OpenTabItemData));

        public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(
            "ContentTemplate", typeof(DataTemplate), typeof(OpenTabItemData));

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header", typeof(string), typeof(OpenTabItemData));

        public static readonly DependencyProperty IconTemplateProperty = DependencyProperty.Register(
            "IconTemplate", typeof(DataTemplate), typeof(OpenTabItemData));

        public static readonly DependencyProperty ShortcutProperty = DependencyProperty.Register(
            "Shortcut", typeof(string), typeof(OpenTabItemData));

        public static readonly DependencyProperty SubModeProperty = DependencyProperty.Register(
            "SubMode", typeof(string), typeof(OpenTabItemData));

        public object Content
        {
            get { return (object)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public DataTemplate ContentTemplate
        {
            get { return (DataTemplate)GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public DataTemplate IconTemplate
        {
            get { return (DataTemplate)GetValue(IconTemplateProperty); }
            set { SetValue(IconTemplateProperty, value); }
        }

        public string Shortcut
        {
            get { return (string)GetValue(ShortcutProperty); }
            set { SetValue(ShortcutProperty, value); }
        }

        public string SubMode
        {
            get { return (string)GetValue(SubModeProperty); }
            set { SetValue(SubModeProperty, value); }
        }
    }
}
