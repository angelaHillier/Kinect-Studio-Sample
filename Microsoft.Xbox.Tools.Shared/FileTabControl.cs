//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class FileTabControl : TabControl
    {
        public static readonly DependencyProperty IsSeparatorProperty = DependencyProperty.RegisterAttached(
            "IsSeparator", typeof(bool), typeof(FileTabControl), new FrameworkPropertyMetadata(false));

        public static readonly RoutedEvent TabChangedEvent = EventManager.RegisterRoutedEvent("TabChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FileTabControl));
        public static readonly RoutedCommand ShortcutCommand = new RoutedCommand("Shortcut", typeof(FileTabControl));

        public FileTabControl()
        {
            this.CommandBindings.Add(new CommandBinding(ShortcutCommand, OnShortcutCommandExecuted, OnShortcutCommandCanExecute));
            this.KeyUp += OnKeyUp;
        }

        void OnShortcutCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var tabData = e.Parameter as FileTabDefinition;
            bool leaveShortcutMode = false;

            if (tabData != null)
            {
                if (tabData.Command != null)
                {
                    tabData.Command.Execute(tabData.CommandParameter);
                    leaveShortcutMode = true;
                }
                else
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
                    else
                    {
                        leaveShortcutMode = true;
                    }
                }

                if (leaveShortcutMode)
                {
                    var toolsWindow = this.FindParent<ToolsUIWindow>();

                    if (toolsWindow != null)
                    {
                        toolsWindow.LeaveShortcutMode();
                    }
                }
            }
        }

        void OnShortcutCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var tabData = e.Parameter as FileTabDefinition;

            if (tabData != null)
            {
                if (tabData.Command != null)
                {
                    e.CanExecute = tabData.Command.CanExecute(tabData.CommandParameter);
                }
                else
                {
                    e.CanExecute = true;
                }
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
            if (this.IsLoaded && this.IsVisible)
            {
                RaiseEvent(new RoutedEventArgs(TabChangedEvent));
            }
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                var data = this.Items[this.SelectedIndex] as FileTabDefinition;

                if (data != null && data.Command != null)
                {
                    RoutedCommand command = data.Command as RoutedCommand; 

                    if (command != null)
                    {
                        command.Execute(data.CommandParameter, data.CommandTarget);
                    }
                    else
                    {
                        data.Command.Execute(data.CommandTarget);
                    }
                }

                e.Handled = true;
            }
        }

        public static bool GetIsSeparator(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSeparatorProperty);
        }

        public static void SetIsSeparator(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSeparatorProperty, value);
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is FileTabItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new FileTabItem();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if (item is FileTabDefinition && element is FileTabItem)
            {
                var data = item as FileTabDefinition;
                var tabItem = element as FileTabItem;

                tabItem.SetFileTabDefinition(data);
            }
            else
            {
                base.PrepareContainerForItemOverride(element, item);
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new FileTabControlAutomationPeer(this);
        }
    }

    public class FileTabItem : TabItem
    {
        RoutedCommand command;

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public void SetFileTabDefinition(FileTabDefinition fileTabDefinition)
        {
            this.DataContext = fileTabDefinition;
            if (fileTabDefinition.IsSeparator)
            {
                this.Content = new Separator();
                this.IsEnabled = false;
            }
            else
            {
                this.SetBinding(TabItem.HeaderProperty, new Binding { Source = fileTabDefinition, Path = new PropertyPath(FileTabDefinition.HeaderProperty) });

                var presenter = new ContentPresenter();
                presenter.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding { Source = fileTabDefinition, Path = new PropertyPath(FileTabDefinition.ContentTemplateProperty) });
                this.Content = presenter;

                this.command = fileTabDefinition.Command as RoutedCommand;

                if (fileTabDefinition.Command != null)
                {
                    fileTabDefinition.Command.CanExecuteChanged += OnCommandCanExecuteChanged;
                    this.IsEnabled = this.command == null ? fileTabDefinition.Command.CanExecute(fileTabDefinition.CommandParameter) : this.command.CanExecute(fileTabDefinition.CommandParameter, fileTabDefinition.CommandTarget);
                }
            }
        }

        public void ClearFileTabData()
        {
            var data = this.DataContext as FileTabDefinition;

            if (data != null && data.Command != null)
            {
                data.Command.CanExecuteChanged -= OnCommandCanExecuteChanged;
            }
        }

        void OnCommandCanExecuteChanged(object sender, EventArgs e)
        {
            var data = this.DataContext as FileTabDefinition;

            if (data != null)
            {
                this.IsEnabled = this.command == null ? data.Command.CanExecute(data.CommandParameter) : this.command.CanExecute(data.CommandParameter, data.CommandTarget);
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            var data = this.DataContext as FileTabDefinition;

            if (data != null)
            {
                if (data.Command != null)
                {
                    // This tab item has a command, which overrides its "page".  
                    e.MouseDevice.Capture(this);
                    this.MouseUp += OnMouseUp;
                    this.LostMouseCapture += OnLostCapture;
                    e.Handled = true;
                    return;
                }
            }

            base.OnPreviewMouseDown(e);
        }

        void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(null);

            if (this.IsMouseOver)
            {
                var data = this.DataContext as FileTabDefinition;

                if (data != null && data.Command != null)
                {
                    if (this.command != null)
                    {
                        this.command.Execute(data.CommandParameter, data.CommandTarget);
                    }
                    else
                    {
                        data.Command.Execute(data.CommandTarget);
                    }
                }
            }
            e.Handled = true;
        }

        void OnLostCapture(object sender, MouseEventArgs e)
        {
            this.MouseUp -= OnMouseUp;
            this.LostMouseCapture -= OnLostCapture;
            e.Handled = true;
        }
    }

    public class FileTabControlAutomationPeer : TabControlAutomationPeer
    {
        FileTabControl owner;
        static string[] additionalUIAutomationChildrenNames = { "PART_HideButton" };

        public FileTabControlAutomationPeer(FileTabControl owner)
            : base(owner)
        {
            this.owner = owner;
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            var list = base.GetChildrenCore();

            foreach (var name in additionalUIAutomationChildrenNames)
            {
                var element = owner.Template.FindName(name, owner) as UIElement;

                if (element != null)
                {
                    list.Add(UIElementAutomationPeer.CreatePeerForElement(element));
                }
            }

            return list;
        }
    }
}
