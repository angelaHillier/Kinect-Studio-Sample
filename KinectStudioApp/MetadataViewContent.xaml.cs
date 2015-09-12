//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using KinectStudioUtility;
    
    // Cannot do two-way bindings on GridViewColumn widths, so let's be creative here...

    public partial class MetadataViewContent : UserControl
    {
        public MetadataViewContent(MetadataView view)
        {
            DebugHelper.AssertUIThread();

            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            this.MetadataInfo = MetadataViewContent.emptyMetadataInfo;

            this.view = view;

            this.InitializeComponent();

            this.GotFocus += MetadataViewContent_GotFocus;

            this.PublicMetadataItemsControl.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(MetadataItemsControl_DragDelta), true);
            this.PublicMetadataItemsControl.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(MetadataItemsControl_DragStarted), true);
            this.PublicMetadataItemsControl.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(MetadataItemsControl_DragCompleted), true);

            this.PersonalMetadataItemsControl.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(MetadataItemsControl_DragDelta), true);
            this.PersonalMetadataItemsControl.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(MetadataItemsControl_DragStarted), true);
            this.PersonalMetadataItemsControl.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(MetadataItemsControl_DragCompleted), true);
        }

        public void CloseMetadataView(ISet<MetadataInfo> metadataViewsToClose)
        {
            DebugHelper.AssertUIThread();

            if (metadataViewsToClose != null)
            {
                if (metadataViewsToClose.Contains(this.MetadataInfo))
                {
                    this.MetadataInfo = MetadataViewContent.emptyMetadataInfo;
                }
            }
        }

        public MetadataView View
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.view;
            }
        }

        public MetadataInfo MetadataInfo
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(MetadataInfoProperty) as MetadataInfo;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(MetadataInfoProperty, value);
            }
        }

        public double PublicKeyWidth
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (double)this.GetValue(MetadataViewContent.PublicKeyWidthProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.HackSetWidth(PublicMetadataItemsControl, PublicKeyWidthProperty, 1, value);
            }
        }

        public double PublicValueWidth
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (double)this.GetValue(MetadataViewContent.PublicValueWidthProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.HackSetWidth(PublicMetadataItemsControl, PublicValueWidthProperty, 0, value);
            }
        }

        public double PersonalKeyWidth
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (double)this.GetValue(MetadataViewContent.PersonalKeyWidthProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.HackSetWidth(PersonalMetadataItemsControl, PersonalKeyWidthProperty, 1, value);
            }
        }

        public double PersonalValueWidth
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (double)this.GetValue(MetadataViewContent.PersonalValueWidthProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.HackSetWidth(PersonalMetadataItemsControl, PersonalValueWidthProperty, 0, value);
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // deal with the fact that textbox eats the up/down which makes it difficult to navigate the list items
            UIElement element = FocusManager.GetFocusedElement(Window.GetWindow(this)) as UIElement;
            if (element != null)
            {
                if ((e.Key == Key.Up) || (e.Key == Key.Down) || ((e.Key == Key.Tab) && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift)))
                {
                    e.Handled = true;

                    ListViewItem item = element.GetVisualParent<ListViewItem>();
                    if (item != null)
                    {
                        item.MoveFocus(new TraversalRequest((e.Key == Key.Down) ? FocusNavigationDirection.Down : FocusNavigationDirection.Up));
                    }
                }
            }

            if (!e.Handled)
            {
                base.OnPreviewKeyDown(e);
            }
        }

        private void UnselectMetadataCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.MetadataInfo = null;
        }

        private void UnselectMetadataCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = this.MetadataInfo != null;
            }
        }

        private void MetadataViewContent_GotFocus(object sender, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            MetadataViewContent.lastFocused = this;
        }

        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            ListView listView = sender as ListView;

            if (listView != null)
            {
                listView.ItemContainerGenerator.ItemsChanged +=
                    (sender2, e2) => this.ListView_ItemsChanged(listView, e2);

                listView.ItemContainerGenerator.StatusChanged +=
                    (sender2, e2) => this.ListView_StatusChanged(listView, e2);

                if (listView == this.PublicMetadataItemsControl)
                {
                    this.PublicKeyWidth = (double)this.GetValue(MetadataViewContent.PublicKeyWidthProperty);
                    this.PublicValueWidth = (double)this.GetValue(MetadataViewContent.PublicValueWidthProperty);
                }
                else if (listView == this.PersonalMetadataItemsControl)
                {
                    this.PersonalKeyWidth = (double)this.GetValue(MetadataViewContent.PersonalKeyWidthProperty);
                    this.PersonalValueWidth = (double)this.GetValue(MetadataViewContent.PersonalValueWidthProperty);
                }
            }
        }

        private void ListView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            ListView listView = sender as ListView;

            if (listView != null)
            {
                ScrollViewer scrollViewer = listView.GetVisualChild<ScrollViewer>();
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(0);
                }
            }
        }

        private void ListView_ItemsChanged(ListView listView, ItemsChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((listView != null) && (e != null))
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (MetadataViewContent.lastFocused == this)
                        {
                            int newIndex = listView.Items.Count - 1;
                            listView.SelectedIndex = newIndex;
                            listView.ScrollIntoView(listView.Items[newIndex]);
                            this.setNextMetadatFocus = listView;
                        }
                        break;

                    default:
                        this.setNextMetadatFocus = null;
                        break;
                }
            }
        }

        private void ListView_StatusChanged(ListView listView, EventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((listView != null) && (e != null))
            {
                if (listView.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                {
                    if (this.setNextMetadatFocus == listView)
                    {
                        int newIndex = listView.SelectedIndex;
                        this.setNextMetadatFocus = null;

                        if (newIndex >= 0)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    FrameworkElement itemContainer = listView.ItemContainerGenerator.ContainerFromIndex(newIndex) as FrameworkElement;
                                    if (itemContainer != null)
                                    {
                                        TraversalRequest traversalRequest = new TraversalRequest(FocusNavigationDirection.Last);
                                        itemContainer.MoveFocus(traversalRequest);
                                    }
                                }));
                        }
                    }

                    if (!this.dragging)
                    {
                        if (listView == this.PublicMetadataItemsControl)
                        {
                            this.PublicKeyWidth = (double)this.GetValue(MetadataViewContent.PublicKeyWidthProperty);
                            this.PublicValueWidth = (double)this.GetValue(MetadataViewContent.PublicValueWidthProperty);
                        }
                        else if (listView == this.PersonalMetadataItemsControl)
                        {
                            this.PersonalKeyWidth = (double)this.GetValue(MetadataViewContent.PersonalKeyWidthProperty);
                            this.PersonalValueWidth = (double)this.GetValue(MetadataViewContent.PersonalValueWidthProperty);
                        }
                    }
                }
            };
        }

        private void HackSetWidth(ListView listView, DependencyProperty property, int indexFromEnd, double value)
        {
            Debug.Assert(listView != null);
            Debug.Assert(property != null);

            value = Math.Max(value, MetadataViewContent.minColumnWidth);

            this.SetValue(property, value);

            GridView gridView = listView.View as GridView;
            if ((gridView != null) && listView.IsLoaded)
            {
                int index = gridView.Columns.Count - 1 - indexFromEnd;
                if ((indexFromEnd >= 0) && (gridView.Columns.Count > index))
                {
                    gridView.Columns[index].Width = value;
                }
            }
        }

        private void MetadataItemsControl_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb thumb = e.OriginalSource as Thumb;
            if (thumb != null)
            {
                GridViewColumnHeader header = thumb.TemplatedParent as GridViewColumnHeader;
                if (header != null) 
                {
                    header.Column.Width = Math.Max(MetadataViewContent.minColumnWidth, header.Column.ActualWidth);
                }
            }
        }

        private void MetadataItemsControl_DragStarted(object sender, DragStartedEventArgs e)
        {
            Thumb thumb = e.OriginalSource as Thumb;
            if (thumb != null)
            {
                this.dragging = true;
            }
        }

        private void MetadataItemsControl_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            Thumb thumb = e.OriginalSource as Thumb;
            if (thumb != null)
            {
                dragging = false;

                GridViewColumnHeader header = thumb.TemplatedParent as GridViewColumnHeader;
                if (header != null)
                {
                    double width = header.Column.ActualWidth;

                    ListView listView = header.GetVisualParent<ListView>();
                    if (listView != null)
                    {
                        GridView gridView = listView.View as GridView;
                        if (gridView != null)
                        {
                            int indexFromEnd = gridView.Columns.Count - gridView.Columns.IndexOf(header.Column) - 1;
                            if (listView == this.PublicMetadataItemsControl)
                            {
                                if (indexFromEnd == 0)
                                {
                                    this.SetValue(MetadataViewContent.PublicValueWidthProperty, width);
                                }
                                else if (indexFromEnd == 1)
                                {
                                    this.SetValue(MetadataViewContent.PublicKeyWidthProperty, width);
                                }
                            }
                            else if (listView == this.PersonalMetadataItemsControl)
                            {
                                if (indexFromEnd == 0)
                                {
                                    this.SetValue(MetadataViewContent.PersonalValueWidthProperty, width);
                                }
                                else if (indexFromEnd == 1)
                                {
                                    this.SetValue(MetadataViewContent.PersonalKeyWidthProperty, width);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ListViewItem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                e.Handled = true;

                if (sender == e.OriginalSource)
                {
                    ListViewItem item = sender as ListViewItem;
                    if (item != null)
                    {
                        Control element = item.GetVisualChild<Control>(uie => uie.IsTabStop && uie.Focusable);
                        if (element != null)
                        {
                            Dispatcher.BeginInvoke(new Action(() => element.Focus()));
                        }
                    }
                }
            }
        }

        private const double minColumnWidth = 50.0;
        private ListBox setNextMetadatFocus = null;
        private readonly MetadataView view = null;
        private bool dragging = false;

        public static readonly DependencyProperty MetadataInfoProperty = DependencyProperty.Register("MetadataInfo", typeof(MetadataInfo), typeof(MetadataViewContent));
        public static readonly DependencyProperty PublicKeyWidthProperty = DependencyProperty.Register("PublicKeyWidth", typeof(double), typeof(MetadataViewContent), new PropertyMetadata(MetadataViewContent.minColumnWidth, null, CoerceColumnWidthValue));
        public static readonly DependencyProperty PublicValueWidthProperty = DependencyProperty.Register("PublicValueWidth", typeof(double), typeof(MetadataViewContent), new PropertyMetadata(MetadataViewContent.minColumnWidth, null, CoerceColumnWidthValue));
        public static readonly DependencyProperty PersonalKeyWidthProperty = DependencyProperty.Register("PersonalKeyWidth", typeof(double), typeof(MetadataViewContent), new PropertyMetadata(MetadataViewContent.minColumnWidth, null, CoerceColumnWidthValue));
        public static readonly DependencyProperty PersonalValueWidthProperty = DependencyProperty.Register("PersonalValueWidth", typeof(double), typeof(MetadataViewContent), new PropertyMetadata(MetadataViewContent.minColumnWidth, null, CoerceColumnWidthValue));

        private static object CoerceColumnWidthValue(DependencyObject d, object value)
        {
            if (value is double)
            {
                double width = (double)value;
                width = Math.Max(MetadataViewContent.minColumnWidth, width);
                value = width;
            }

            return value;
        }

        private static MetadataViewContent lastFocused = null;
        private static readonly MetadataInfo emptyMetadataInfo = new MetadataInfo(true, null, null, null, null);
    }
}
