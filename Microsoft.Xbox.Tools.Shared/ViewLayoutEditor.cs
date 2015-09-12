//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ViewLayoutEditor : Control
    {
        public static readonly DependencyProperty ViewsProperty = DependencyProperty.Register(
            "Views", typeof(List<AffinitizedViewCreator>), typeof(ViewLayoutEditor));

        public static readonly DependencyProperty SelectedDocumentCategoryProperty = DependencyProperty.Register(
            "SelectedDocumentCategory", typeof(DocumentCategory), typeof(ViewLayoutEditor), new FrameworkPropertyMetadata(OnSelectedDocumentCategoryChanged));

        public static readonly DependencyProperty IsLayoutVisibleProperty = DependencyProperty.RegisterAttached(
            "IsLayoutVisible", typeof(bool), typeof(ViewLayoutEditor), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty ShowInternalViewsProperty = DependencyProperty.Register(
            "ShowInternalViews", typeof(bool), typeof(ViewLayoutEditor), new FrameworkPropertyMetadata(OnShowInternalViewsChanged));

        public static readonly DependencyProperty CurrentLayoutDefinitionForEditProperty = DependencyProperty.Register(
            "CurrentLayoutDefinitionForEdit", typeof(LayoutDefinition), typeof(ViewLayoutEditor), new FrameworkPropertyMetadata(OnCurrentLayoutDefinitionForEditChanged));

        public static readonly DependencyProperty IsCategorySelectedProperty = DependencyProperty.RegisterAttached(
            "IsCategorySelected", typeof(bool), typeof(ViewLayoutEditor), new FrameworkPropertyMetadata(false));

        public static readonly RoutedCommand RemovePrimitiveCommand = new RoutedCommand("RemovePrimitive", typeof(ViewLayoutEditor));
        public static readonly RoutedCommand CreateNewWindowCommand = new RoutedCommand("CreateNewWindow", typeof(ViewLayoutEditor));
        public static readonly RoutedCommand DeleteLayoutPageCommand = new RoutedCommand("DeleteLayoutPage", typeof(ViewLayoutEditor));

        LayoutTabControl tabControl;
        ViewListBox viewList;
        ListBoxItem capturedItem;
        AffinitizedViewCreator creatorBeingDragged;
        Point dragStart;
        Point originalScreenPos;
        ViewDragGhostWindow dragGhostWindow;
        ViewDropTargetWindow dockTargetWindow;
        ViewDockSpot hitTestSpot;
        Slot dockTargetSlot;
        LayoutControl layout;
        List<IViewCreationCommand> sortedViewList;
        bool tabHitTesting;
        TabItem hitTestTabItem;
        bool ignoreLayoutDefinitionCollectionChanges;

        public ViewLayoutEditor()
        {
            this.CommandBindings.Add(new CommandBinding(RemovePrimitiveCommand, OnRemovePrimitiveExecuted));
            this.CommandBindings.Add(new CommandBinding(CreateNewWindowCommand, OnCreateNewWindowExecuted));
            this.CommandBindings.Add(new CommandBinding(DeleteLayoutPageCommand, OnDeleteLayoutPageExecuted, OnDeleteLayoutPageCanExecute));

            this.AddHandler(ViewListGroupHeader.HeaderSelectedEvent, new RoutedEventHandler(OnViewListGroupHeaderSelected));

            this.sortedViewList = ToolsUIApplication.Instance.ExtensionManager.BuildListOfViewCommands().OrderBy(c => c.DisplayName).ToList();
            this.EditableLayoutDefinitions = new ObservableCollection<LayoutDefinition>();

            this.DocumentCategories = ToolsUIApplication.Instance.DocumentCategories;
            this.SelectedDocumentCategory = this.DocumentCategories.FirstOrDefault(c => c.DocumentFactoryName == null);

            ToolsUIApplication.Instance.LayoutDefinitions.CollectionChanged += OnLayoutDefinitionCollectionChanged;
            RebuildEditableLayoutsList();

            RebuildViewList();
        }

        public IEnumerable<DocumentCategory> DocumentCategories { get; private set; }
        public ObservableCollection<LayoutDefinition> EditableLayoutDefinitions { get; private set; }

        public List<AffinitizedViewCreator> Views
        {
            get { return (List<AffinitizedViewCreator>)GetValue(ViewsProperty); }
            set { SetValue(ViewsProperty, value); }
        }

        public DocumentCategory SelectedDocumentCategory
        {
            get { return (DocumentCategory)GetValue(SelectedDocumentCategoryProperty); }
            set { SetValue(SelectedDocumentCategoryProperty, value); }
        }

        public bool ShowInternalViews
        {
            get { return (bool)GetValue(ShowInternalViewsProperty); }
            set { SetValue(ShowInternalViewsProperty, value); }
        }

        LayoutControl CurrentLayout
        {
            get
            {
                if (this.tabControl != null)
                {
                    return this.tabControl.SelectedContent as LayoutControl;
                }

                return null;
            }
        }

        public LayoutDefinition CurrentLayoutDefinitionForEdit
        {
            get { return (LayoutDefinition)GetValue(CurrentLayoutDefinitionForEditProperty); }
            set { SetValue(CurrentLayoutDefinitionForEditProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.tabControl = GetTemplateChild("PART_LayoutTabControl") as LayoutTabControl;
            this.viewList = GetTemplateChild("PART_ViewListBox") as ViewListBox;

            if (this.tabControl == null || this.viewList == null)
            {
                throw new InvalidOperationException("ViewLayoutEditor template is not correct.");
            }

            this.viewList.ListBoxButtonDownAction = OnListBoxItemLeftButtonDown;
            this.Dispatcher.BeginInvoke((Action)(() => { OnCurrentLayoutDefinitionForEditChanged(); }), DispatcherPriority.Background);

            var window = this.FindParent<ToolsUIWindow>();

            if (window != null)
            {
                this.SetBinding(CurrentLayoutDefinitionForEditProperty, new Binding { Source = window, Path = new PropertyPath(ToolsUIWindow.CurrentLayoutDefinitionForEditProperty) });
            }
        }

        void OnViewListGroupHeaderSelected(object sender, RoutedEventArgs e)
        {
            var header = e.OriginalSource as ViewListGroupHeader;

            if (header != null)
            {
                var g = header.Content as CollectionViewGroup;

                if (g != null && g.ItemCount > 0)
                {
                    var creator = g.Items[0] as AffinitizedViewCreator;

                    if (creator != null)
                    {
                        this.SelectedDocumentCategory = creator.Category;
                    }
                }
            }

            e.Handled = true;
        }

        void OnLayoutDefinitionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!this.ignoreLayoutDefinitionCollectionChanges)
            {
                RebuildEditableLayoutsList();
            }
        }

        void OnRemovePrimitiveExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var viewSource = e.Parameter as ViewSource;
            var layout = this.CurrentLayout;

            if (layout != null)
            {
                layout.LayoutInstance.RemoveViewSource(viewSource);
            }
        }

        void OnCreateNewWindowExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var newWindow = ToolsUIApplication.Instance.CreateWindow();
            newWindow.Show();
        }

        void OnDeleteLayoutPageExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var layout = e.Parameter as LayoutControl;

            if (layout != null)
            {
                var notifyService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(IUserNotificationService)) as IUserNotificationService;

                if (notifyService != null)
                {
                    var result = notifyService.ShowMessageBox(StringResources.ConfirmDeleteViewLayout, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                    if (result == MessageBoxResult.Yes)
                    {
                        ToolsUIApplication.Instance.LayoutDefinitions.Remove(layout.LayoutInstance.LayoutDefinition);
                    }
                }
            }
        }

        void OnDeleteLayoutPageCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is LayoutControl;
            e.Handled = true;
        }

        void OnListBoxItemLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ReleaseCapturedItem();

            var item = sender as ListBoxItem;

            if (item != null)
            {
                var creator = item.Content as AffinitizedViewCreator;

                if (creator != null)
                {
                    if (creator.Category != null && creator.Category.DocumentFactoryName != null)
                    {
                        // Make sure the selected document category matches the category of the clicked-on item.  Note
                        // that we do NOT do this for the "Any" category, since those views may be placed on any layout,
                        // not just the non-affinitized layouts.
                        this.SelectedDocumentCategory = creator.Category;
                    }

                    this.creatorBeingDragged = creator;
                }

                e.MouseDevice.Capture(item);
                this.capturedItem = item;
                this.dragStart = e.GetPosition(this);
                this.originalScreenPos = this.capturedItem.PointToScreenIndependent(new Point(0, 0));
                this.dragGhostWindow = null;
                item.Cursor = Cursors.SizeAll;
                item.MouseMove += OnListBoxItemMouseMove;
                item.MouseLeftButtonUp += OnListBoxItemLeftButtonUp;
                item.LostMouseCapture += OnListBoxItemLostCapture;

                this.dragGhostWindow = new ViewDragGhostWindow
                {
                    ViewName = ((AffinitizedViewCreator)item.Content).Command.DisplayName,
                };

                this.dragGhostWindow.Show();
                UpdateHitState(this.originalScreenPos);
                e.Handled = true;
            }
        }

        void OnListBoxItemMouseMove(object sender, MouseEventArgs e)
        {
            var delta = e.GetPosition(this) - this.dragStart;

            if (this.dragGhostWindow != null)
            {
                UpdateHitState(this.originalScreenPos + delta);
            }
        }

        void OnListBoxItemLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.layout != null && this.dockTargetWindow != null && this.dockTargetSlot != null)
            {
                Dock? dock = null;
                Slot targetSlot = this.dockTargetSlot;

                if (this.hitTestSpot != null && !this.hitTestSpot.IsTabbed)
                {
                    dock = this.hitTestSpot.Dock;
                    targetSlot = this.hitTestSpot.DestinationSlot;
                }

                var command = ((AffinitizedViewCreator)this.capturedItem.Content).Command;
                layout.LayoutInstance.LayoutDefinition.AddViewSource(command, targetSlot, dock);
            }
            e.MouseDevice.Capture(null);
        }

        void OnListBoxItemLostCapture(object sender, MouseEventArgs e)
        {
            ReleaseCapturedItem();
        }

        void ReleaseCapturedItem()
        {
            if (this.capturedItem != null)
            {
                this.capturedItem.Cursor = null;
                this.capturedItem.MouseMove -= OnListBoxItemMouseMove;
                this.capturedItem.MouseLeftButtonUp -= OnListBoxItemLeftButtonUp;
                this.capturedItem.LostMouseCapture -= OnListBoxItemLostCapture;
                this.capturedItem = null;
                this.creatorBeingDragged = null;

                if (this.dragGhostWindow != null)
                {
                    this.dragGhostWindow.Close();
                    this.dragGhostWindow = null;
                }

                if (this.dockTargetWindow != null)
                {
                    this.dockTargetWindow.Close();
                    this.dockTargetWindow = null;
                }
            }
        }

        void UpdateHitState(Point pos)
        {
            LayoutControl targetLayout = this.CurrentLayout;
            bool ghostPositionSet = false;

            this.dockTargetSlot = null;
            this.dragGhostWindow.CancelReason = "To place the view, drag over an empty layout, an existing view, or a docking target.";

            if (targetLayout != null)
            {
                var posInLayout = Mouse.GetPosition(targetLayout);
                var rect = new Rect(0, 0, targetLayout.ActualWidth, targetLayout.ActualHeight);

                if (rect.Contains(posInLayout))
                {
                    this.dockTargetSlot = targetLayout.GetSlotUnderPoint(Mouse.GetPosition(targetLayout));
                }
                else
                {
                    targetLayout = null;
                }

                if (this.dockTargetSlot != null)
                {
                    if (this.creatorBeingDragged != null)
                    {
                        if (this.creatorBeingDragged.Category.DocumentFactoryName != null &&
                            targetLayout.LayoutInstance.LayoutDefinition.DocumentFactoryName != this.creatorBeingDragged.Category.DocumentFactoryName)
                        {
                            // The creator being dragged is affinitized with a particular document type which is not the same type as the active
                            // layout.  Disallow the drop.
                            this.dockTargetSlot = null;
                            this.dragGhostWindow.CancelReason = string.Format(CultureInfo.InvariantCulture, "This view can only be placed in a {0} layout.", this.creatorBeingDragged.Category.DisplayName);
                        }
                        else if (this.creatorBeingDragged.Command.IsSingleInstancePerLayout &&
                            (targetLayout.LayoutInstance.LayoutDefinition.ViewSources.Any(vs => vs.ViewCreator == this.creatorBeingDragged.Command)))
                        {
                            // This is a single-instance-per-layout view that already exists in the layout.  No dice.
                            this.dockTargetSlot = null;
                            this.dragGhostWindow.CancelReason = "Only one instance of this view type can exist in a layout.";
                        }
                        else if (this.creatorBeingDragged.Command.IsSingleInstance)
                        {
                            var existingSource = ToolsUIApplication.Instance.LayoutDefinitions.SelectMany(d => d.ViewSources).Where(vs => vs.ViewCreator == this.creatorBeingDragged.Command).FirstOrDefault();

                            if (existingSource != null)
                            {
                                // This is a single-instance view that already exists in a layout (perhaps not even this one).
                                this.dockTargetSlot = null;
                                this.dragGhostWindow.CancelReason = string.Format(CultureInfo.InvariantCulture,
                                    "This view already exists in the '{0}' layout.  Only one instance of this view type can be created.", existingSource.Parent.Header);
                            }
                        }
                    }
                }
            }

            if (this.dockTargetSlot != null)
            {
                this.layout = targetLayout;
                if (this.dockTargetWindow == null)
                {
                    this.dockTargetWindow = new ViewDropTargetWindow();
                }

                var rect = targetLayout.GetSlotScreenRect(this.dockTargetSlot);

                this.dockTargetWindow.Left = rect.X;
                this.dockTargetWindow.Top = rect.Y;
                this.dockTargetWindow.Width = rect.Width;
                this.dockTargetWindow.Height = rect.Height;
                this.dockTargetWindow.IsTabbedSpotVisible = true;
                this.dockTargetWindow.RootSlot = targetLayout.LayoutInstance.LayoutDefinition.SlotDefinition;
                this.dockTargetWindow.TargetSlot = this.dockTargetSlot;
                this.dockTargetWindow.AreDockSpotsVisible = targetLayout.LayoutInstance.LayoutDefinition.ViewSources.Count > 0;
                this.dockTargetWindow.Show();

                this.hitTestSpot = null;
                this.tabHitTesting = false;
                VisualTreeHelper.HitTest(this.dockTargetWindow, this.HitTestFilter, this.HitTestResult, new PointHitTestParameters(Mouse.GetPosition(this.dockTargetWindow)));
                if (this.hitTestSpot != null)
                {
                    var spot = this.hitTestSpot;

                    if (spot != null && spot.DestinationSlot != null)
                    {
                        rect = targetLayout.GetSlotScreenRect(spot.DestinationSlot);

                        this.dragGhostWindow.CancelMode = false;
                        this.dragGhostWindow.CancelReason = null;

                        if (spot.IsTabbed)
                        {
                            this.dragGhostWindow.Left = rect.X;
                            this.dragGhostWindow.Top = rect.Y;
                            this.dragGhostWindow.Width = rect.Width;
                            this.dragGhostWindow.Height = rect.Height;
                        }
                        else
                        {
                            switch (spot.Dock)
                            {
                                case Dock.Top:
                                    this.dragGhostWindow.Left = rect.X;
                                    this.dragGhostWindow.Top = rect.Y;
                                    this.dragGhostWindow.Width = rect.Width;
                                    this.dragGhostWindow.Height = rect.Height / 2;
                                    break;
                                case Dock.Bottom:
                                    this.dragGhostWindow.Width = rect.Width;
                                    this.dragGhostWindow.Height = rect.Height / 2;
                                    this.dragGhostWindow.Left = rect.X;
                                    this.dragGhostWindow.Top = rect.Y + this.dragGhostWindow.Height;
                                    break;
                                case Dock.Left:
                                    this.dragGhostWindow.Left = rect.X;
                                    this.dragGhostWindow.Top = rect.Y;
                                    this.dragGhostWindow.Width = rect.Width / 2;
                                    this.dragGhostWindow.Height = rect.Height;
                                    break;
                                case Dock.Right:
                                    this.dragGhostWindow.Width = rect.Width / 2;
                                    this.dragGhostWindow.Height = rect.Height;
                                    this.dragGhostWindow.Left = rect.X + this.dragGhostWindow.Width;
                                    this.dragGhostWindow.Top = rect.Y;
                                    break;
                            }
                        }

                        ghostPositionSet = true;
                    }
                }
                else
                {
                    // Didn't even hit the tabbed spot (the transparent rect covering the whole slot), so must be in
                    // the margin.  Tear down the dock target window.  (Won't flicker because it fades in...)
                    this.dockTargetWindow.Close();
                    this.dockTargetWindow = null;
                    this.dockTargetSlot = null;
                }
            }
            else
            {
                if (this.dockTargetWindow != null)
                {
                    this.dockTargetWindow.Close();
                    this.dockTargetWindow = null;
                }

                this.layout = null;

                // See if the mouse is over a layout tab.  If so, select it.
                this.tabHitTesting = true;
                this.hitTestTabItem = null;
                VisualTreeHelper.HitTest(this.tabControl, this.HitTestFilter, this.HitTestResult, new PointHitTestParameters(Mouse.GetPosition(this.tabControl)));

                if (this.hitTestTabItem != null)
                {
                    var layoutDef = this.hitTestTabItem.DataContext as LayoutDefinition;

                    if (layoutDef != null)
                    {
                        this.tabControl.SelectedItem = layoutDef;
                    }
                }
            }

            if (!ghostPositionSet)
            {
                this.dragGhostWindow.Width = 200;
                this.dragGhostWindow.Height = 200;
                this.dragGhostWindow.Left = pos.X;
                this.dragGhostWindow.Top = pos.Y;
                this.dragGhostWindow.CancelMode = true;
            }
        }

        HitTestFilterBehavior HitTestFilter(DependencyObject potentialHit)
        {
            if (this.tabHitTesting)
            {
                TabItem item = potentialHit as TabItem;

                if (item != null && item.Visibility == Visibility.Visible && item.DataContext is LayoutDefinition)
                {
                    this.hitTestTabItem = item;
                    return HitTestFilterBehavior.Stop;
                }

                return HitTestFilterBehavior.Continue;
            }
            else
            {
                ViewDockSpot spot = potentialHit as ViewDockSpot;

                if (spot != null && spot.IsHitTestVisible)
                {
                    this.hitTestSpot = spot;
                    return HitTestFilterBehavior.Stop;
                }

                return HitTestFilterBehavior.Continue;
            }
        }

        HitTestResultBehavior HitTestResult(HitTestResult result)
        {
            return HitTestResultBehavior.Continue;
        }

        bool IsViewAffinitized(IViewCreationCommand viewCommand, DocumentCategory category)
        {
            if (viewCommand.IsInternalOnly && !this.ShowInternalViews)
            {
                // No internal views...
                return false;
            }

            if (viewCommand.DocumentFactoryAffinities.Count() == 0)
            {
                // This view has no declared affinity, so it can go anywhere
                return true;
            }

            if ((category == null) || (category.DocumentFactoryName == null))
            {
                // This view has declared affinity(ies), so it can't go in the 'Any' layouts
                return false;
            }

            // This view is affinitized if it has declared affinity with the active document factory
            return viewCommand.DocumentFactoryAffinities.Contains(category.DocumentFactoryName);
        }

        int FindInsertIndexForDocumentFactory(ObservableCollection<LayoutDefinition> list, string documentFactoryName)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].DocumentFactoryName == documentFactoryName || list[i].DocumentFactoryName == null)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        void InsertLayoutDefinition(ObservableCollection<LayoutDefinition> list, LayoutDefinition layoutDef)
        {
            list.Insert(FindInsertIndexForDocumentFactory(list, layoutDef.DocumentFactoryName), layoutDef);
        }

        void AddPlaceholder(string documentFactoryName)
        {
            var layoutDef = new LayoutDefinition
            {
                Header = "",
                Id = Guid.NewGuid(),
                DocumentFactoryName = documentFactoryName,
                IsNewPlaceholder = true
            };

            layoutDef.PlaceholderModified += OnPlaceholderModified;
            InsertLayoutDefinition(this.EditableLayoutDefinitions, layoutDef);
        }

        void RebuildEditableLayoutsList()
        {
            foreach (var layoutDef in this.EditableLayoutDefinitions)
            {
                layoutDef.PlaceholderModified -= this.OnPlaceholderModified;
            }

            this.EditableLayoutDefinitions.Clear();

            foreach (var layoutDef in ToolsUIApplication.Instance.LayoutDefinitions)
            {
                this.EditableLayoutDefinitions.Add(layoutDef);
            }

            foreach (var cat in this.DocumentCategories)
            {
                AddPlaceholder(cat.DocumentFactoryName);
            }

            ResetLayoutDefinitionVisibility();
        }

        void OnPlaceholderModified(object sender,  EventArgs e)
        {
            var layoutDef = sender as LayoutDefinition;

            if (layoutDef != null)
            {
                layoutDef.IsNewPlaceholder = false;
                layoutDef.PlaceholderModified -= OnPlaceholderModified;
                layoutDef.Header = "UNTITLED";
                this.ignoreLayoutDefinitionCollectionChanges = true;
                try
                {
                    InsertLayoutDefinition(ToolsUIApplication.Instance.LayoutDefinitions, layoutDef);
                    AddPlaceholder(layoutDef.DocumentFactoryName);
                }
                finally
                {
                    this.ignoreLayoutDefinitionCollectionChanges = false;
                }
            }
        }

        void RebuildViewList()
        {
            this.Views = new List<AffinitizedViewCreator>();

            foreach (var category in this.DocumentCategories.Where(c => c.DocumentFactoryName == null)
                                            .Concat(this.DocumentCategories.Where(c => c.DocumentFactoryName != null)).OrderBy(c => c.DisplayName))
            {
                this.Views.AddRange(this.sortedViewList.Where(viewCommand => IsViewAffinitized(viewCommand, category) && (category.DocumentFactoryName == null || !IsViewAffinitized(viewCommand, null)))
                    .Select(viewCommand => new AffinitizedViewCreator
                    {
                        Command = viewCommand,
                        Title = category.DisplayName,
                        Category = category
                    }));
            }

            CollectionViewSource.GetDefaultView(this.Views).GroupDescriptions.Add(new PropertyGroupDescription("Title"));
        }

        void ResetLayoutDefinitionVisibility()
        {
            LayoutDefinition mostRecentlyActiveLayout = null;

            foreach (var layoutDef in this.EditableLayoutDefinitions)
            {
                bool isVisible = layoutDef.DocumentFactoryName == null || StringComparer.OrdinalIgnoreCase.Equals(layoutDef.DocumentFactoryName, this.SelectedDocumentCategory.DocumentFactoryName);

                ViewLayoutEditor.SetIsLayoutVisible(layoutDef, isVisible);
                if (isVisible)
                {
                    if (mostRecentlyActiveLayout == null || mostRecentlyActiveLayout.ActivationIndex < layoutDef.ActivationIndex)
                    {
                        mostRecentlyActiveLayout = layoutDef;
                    }
                }
            }

            if ((mostRecentlyActiveLayout == null) && (this.tabControl != null))
            {
                this.tabControl.SelectedIndex = -1;
            }
            else if (this.tabControl != null)
            {
                this.tabControl.SelectedItem = mostRecentlyActiveLayout;
            }

            foreach (var category in this.DocumentCategories)
            {
                SetIsCategorySelected(category, category == this.SelectedDocumentCategory);
            }
        }

        void OnCurrentLayoutDefinitionForEditChanged()
        {
            if (this.CurrentLayoutDefinitionForEdit != null && this.tabControl != null)
            {
                this.SelectedDocumentCategory = ToolsUIApplication.Instance.DocumentCategories.FirstOrDefault(c => c.DocumentFactoryName == this.CurrentLayoutDefinitionForEdit.DocumentFactoryName);
                this.tabControl.SelectedIndex = this.EditableLayoutDefinitions.IndexOf(this.CurrentLayoutDefinitionForEdit);
            }
        }

        static void OnSelectedDocumentCategoryChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ViewLayoutEditor editor = obj as ViewLayoutEditor;

            if (editor != null)
            {
                editor.ResetLayoutDefinitionVisibility();
            }
        }

        static void OnShowInternalViewsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ViewLayoutEditor editor = obj as ViewLayoutEditor;

            if (editor != null)
            {
                editor.RebuildViewList();
            }
        }

        static void OnCurrentLayoutDefinitionForEditChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ViewLayoutEditor editor = obj as ViewLayoutEditor;

            if (editor != null)
            {
                editor.OnCurrentLayoutDefinitionForEditChanged();
            }
        }

        public static bool GetIsLayoutVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsLayoutVisibleProperty);
        }

        public static void SetIsLayoutVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(IsLayoutVisibleProperty, value);
        }

        public static bool GetIsCategorySelected(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsCategorySelectedProperty);
        }

        public static void SetIsCategorySelected(DependencyObject obj, bool value)
        {
            obj.SetValue(IsCategorySelectedProperty, value);
        }
    }

    public class AffinitizedViewCreator : DependencyObject
    {
        public IViewCreationCommand Command { get; set; }
        public string Title { get; set; }
        public DocumentCategory Category { get; set; }
    }

    public class ViewListGroupHeader : ContentControl
    {
        public static readonly RoutedEvent HeaderSelectedEvent = EventManager.RegisterRoutedEvent("HeaderSelected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ViewListGroupHeader));

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(HeaderSelectedEvent, this));
            base.OnMouseDown(e);
        }
    }
}
