//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class SplitTabsControl : Control, IActivationSite
    {
        private static int nextActivationIndex = 0;

        private SlotPanel host;
        private TabNode activeTabNode;
        private TabNode rootTabNode;
        private TabNode previouslyActiveTabNode;
        private int nextSlotName;
        private SplitTabsControl masterControl;
        private List<FloatingWindow> floatingWindows;
        private ActivatableTabControl lastActiveChildSite;

        public static RoutedCommand CloseViewCommand = new RoutedCommand("CloseView", typeof(SplitTabsControl));
        public static RoutedCommand NewHorizontalTabGroupCommand = new RoutedCommand("NewHorizontalTabGroup", typeof(SplitTabsControl));
        public static RoutedCommand NewVerticalTabGroupCommand = new RoutedCommand("NewVerticalTabGroup", typeof(SplitTabsControl));

        public TabNode RootNode { get { return rootTabNode; } }

        public SplitTabsControl()
        {
            this.AddHandler(ActivatableTabItem.ActivatedEvent, new EventHandler<RoutedEventArgs>(OnTabActivated));
            this.AddHandler(ActivatableTabItem.ClosedEvent, new EventHandler<RoutedEventArgs>(OnTabClosed));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.host = this.Template.FindName("PART_Host", this) as SlotPanel;
            if (this.host == null)
                throw new InvalidOperationException();      // This is a style authoring issue; make sure the template has the appropriate part in it

            if (this.rootTabNode == null)
            {
                // If our template is applied before we're asked to load state, then we create a default state.  
                // This is wasteful if state actually gets loaded... so be sure to load any state before this control is visualized.
                this.rootTabNode = new TabNode { Slot = new Slot { Name = GetNextSlotName() }, TabControl = new ActivatableTabControl() };
                this.rootTabNode.TabControl.ParentSite = this;
                SlotPanel.SetSlotName(this.rootTabNode.TabControl, this.rootTabNode.Slot.Name);
                this.activeTabNode = this.rootTabNode;
                this.activeTabNode.TabControl.IsActive = true;
                this.rootTabNode.TabControl.TabNode = this.rootTabNode;
                this.host.Children.Add(this.rootTabNode.TabControl);
            }
            else
            {
                // Walk the tree and add all tab controls to the host
                AddTabControls(this.rootTabNode);
            }

            this.host.SlotDefinition = this.rootTabNode.Slot;
        }

        void ActivateLastActiveTabNode()
        {
            if (this.previouslyActiveTabNode != null)
            {
                this.activeTabNode = this.previouslyActiveTabNode;
            }
            else
            {
                for (this.activeTabNode = this.rootTabNode; this.activeTabNode.Children != null; this.activeTabNode = this.activeTabNode.Children[0])
                    ;
            }

            this.previouslyActiveTabNode = null;

            if (this.activeTabNode != null && this.activeTabNode.TabControl != null)
            {
                this.activeTabNode.TabControl.IsActive = true;
            }
        }

        void ActivateTabNode(TabNode node)
        {
            if (this.activeTabNode != node)
            {
                if (this.activeTabNode != null && this.activeTabNode.TabControl != null)
                {
                    this.activeTabNode.TabControl.IsActive = false;
                }

                if (node.TabControl != null && node.TabControl.FindParent<SplitTabsControl>() == this)
                {
                    this.previouslyActiveTabNode = this.activeTabNode;
                }
                else
                {
                    this.previouslyActiveTabNode = null;
                }

                this.activeTabNode = node;
                node.TabControl.IsActive = true;
            }
        }

        void AddTabControls(TabNode node)
        {
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    AddTabControls(child);
                }
            }
            else
            {
                Debug.Assert(node.TabControl != null);
                this.host.Children.Add(node.TabControl);
                node.TabControl.ParentSite = this;
            }
        }

        string GetNextSlotName()
        {
            return (nextSlotName++).ToString();
        }

        public void LoadViewState(XElement state, IDictionary<string, IViewCreationCommand> viewCreators, IServiceProvider serviceProvider)
        {
            var mainWindow = state.Element("MainContent");

            LoadSplitTabsControlState(mainWindow, viewCreators, serviceProvider, focus: true);

            if (this.floatingWindows != null)
            {
                // NOTE, ToArray() is necessary; floating windows remove themselves from this.floatingWindows when they close.
                foreach (var floatingWindow in this.floatingWindows.ToArray())
                {
                    floatingWindow.Close();
                }
            }

            foreach (var floatingWindowState in state.Elements("FloatingContent"))
            {
                LoadFloatingWindow(floatingWindowState, viewCreators, serviceProvider);
            }
        }

        void LoadSplitTabsControlState(XElement parentState, IDictionary<string, IViewCreationCommand> viewCreators, IServiceProvider serviceProvider, bool focus)
        {
            this.rootTabNode = LoadTabNode(parentState.Element("TabNode"), viewCreators, serviceProvider);
            if (this.activeTabNode == null)
            {
                // In case there wasn't one in the state that was marked as active, activate the previously active one (which will be null,
                // and fall back on the first child node)
                ActivateLastActiveTabNode();
            }

            if (this.activeTabNode != null && this.activeTabNode.TabControl != null && focus)
            {
                // Make sure the active tab in the active tab control is focused
                var activeItem = this.activeTabNode.TabControl.SelectedItem as ActivatableTabItem;

                if (activeItem != null && !activeItem.Focus())
                {
                    RoutedEventHandler focusGrabber = null;

                    focusGrabber = (o, s) =>
                    {
                        activeItem.Focus();
                        activeItem.Loaded -= focusGrabber;
                    };
                    activeItem.Loaded += focusGrabber;
                }
            }

            // If our template has already been applied, we'll know about our host.  Which means we populated it with
            // a default state.  Wipe it, and use what we loaded.  
            if (this.host != null)
            {
                this.host.Children.Clear();
                AddTabControls(this.rootTabNode);
                this.host.SlotDefinition = this.rootTabNode.Slot;
            }
        }

        void LoadFloatingWindow(XElement state, IDictionary<string, IViewCreationCommand> viewCreators, IServiceProvider serviceProvider)
        {
            var window = this.CreateFloatingHost(false);

            window.SetLocation(
                double.Parse(state.Attribute("Left").Value),
                double.Parse(state.Attribute("Top").Value),
                double.Parse(state.Attribute("Width").Value),
                double.Parse(state.Attribute("Height").Value));

            bool isMaximized = bool.Parse(state.Attribute("IsMaximized").Value);

            window.SplitTabsControl.LoadSplitTabsControlState(state, viewCreators, serviceProvider, false);

            if (isMaximized)
            {
                RoutedEventHandler handler = null;

                // Can't set maximized here; need to do it after the window is loaded so that it maximizes on the correct monitor
                handler = (s, e) => { window.WindowState = WindowState.Maximized; window.Loaded -= handler; };
                window.Loaded += handler;
            }

            var parent = this.FindParent<Window>();
            parent.Loaded += (s, e) =>
            {
                window.Show();
            };
        }

        TabNode LoadTabNode(XElement nodeElement, IDictionary<string, IViewCreationCommand> viewCreators, IServiceProvider serviceProvider)
        {
            XElement children = nodeElement.Element("Children");
            TabNode node = new TabNode { Slot = new Slot { Name = GetNextSlotName() } };

            if (children != null)
            {
                node.Slot.Orientation = (Orientation)Enum.Parse(typeof(Orientation), nodeElement.Attribute("Orientation").Value);
                node.Children = new List<TabNode>();

                foreach (var childElement in children.Elements("TabNode"))
                {
                    TabNode childNode = LoadTabNode(childElement, viewCreators, serviceProvider);
                    double size = double.Parse(childElement.Attribute("Length").Value, CultureInfo.InvariantCulture);

                    childNode.Slot.Length = new GridLength(size, GridUnitType.Star);
                    childNode.Parent = node;
                    node.Children.Add(childNode);
                    node.Slot.Children.Add(childNode.Slot);
                }
            }
            else
            {
                XElement tabs = nodeElement.Element("Tabs");
                bool isActive = bool.Parse(nodeElement.Attribute("IsActive").Value);

                node.TabControl = new ActivatableTabControl();
                SlotPanel.SetSlotName(node.TabControl, node.Slot.Name);
                node.TabControl.TabNode = node;

                foreach (var tab in tabs.Elements("Tab"))
                {
                    string registeredName = tab.Attribute("RegisteredName").Value;
                    bool isSelected = bool.Parse(tab.Attribute("IsSelected").Value);
                    IViewCreationCommand creator;

                    if (!viewCreators.TryGetValue(registeredName, out creator))
                    {
                        // If the view is bogus, just skip it.
                        continue;
                    }

                    ActivatableTabItem item = CreateTabItem(creator, serviceProvider);
                    XElement privateState = tab.Element("PrivateState");

                    if (privateState != null && privateState.HasElements)
                    {
                        item.View.LoadViewState(privateState.Elements().First());
                    }

                    node.TabControl.Items.Add(item);
                    if (isSelected)
                    {
                        node.TabControl.SelectedItem = item;
                        if (isActive)
                        {
                            this.activeTabNode = node;
                            node.TabControl.IsActive = true;
                        }
                    }
                }
            }

            return node;
        }

        XElement CreateFloatingContentElement(FloatingWindow floatingWindow)
        {
            return new XElement("FloatingContent",
                new XAttribute("Left", floatingWindow.NormalRect.Left),
                new XAttribute("Top", floatingWindow.NormalRect.Top),
                new XAttribute("Width", floatingWindow.NormalRect.Width),
                new XAttribute("Height", floatingWindow.NormalRect.Height),
                new XAttribute("IsMaximized", (floatingWindow.WindowState == WindowState.Maximized)),
                CreateTabNodeElement(floatingWindow.SplitTabsControl.rootTabNode));
        }

        public XElement SaveViewState()
        {
            return new XElement("ViewState",
                new XElement("MainContent", CreateTabNodeElement(this.rootTabNode)),
                this.FloatingWindows.Select(fw => CreateFloatingContentElement(fw)));
        }

        XElement CreateTabNodeElement(TabNode node)
        {
            var nodeElement = new XElement("TabNode",
                new XAttribute("Length", node.Slot.Length.Value));

            if (node.Children != null)
            {
                nodeElement.Add(
                    new XAttribute("Orientation", node.Slot.Orientation),
                    new XElement("Children",
                        node.Children.Select(child => CreateTabNodeElement(child))));
            }
            else
            {
                nodeElement.Add(
                    new XAttribute("IsActive", node.TabControl.IsActive),
                    new XElement("Tabs",
                        node.TabControl.Items.OfType<ActivatableTabItem>().Select(item => CreateTabElement(item))));
            }

            return nodeElement;
        }

        XElement CreateTabElement(ActivatableTabItem item)
        {
            var element = new XElement("Tab",
                new XAttribute("RegisteredName", item.ViewCreator.RegisteredName),
                new XAttribute("IsSelected", item.IsSelected));

            var view = item.View;

            if (view != null)
            {
                var state = view.GetViewState();

                if (state != null)
                {
                    element.Add(new XElement("PrivateState", view.GetViewState()));
                }
            }

            return element;
        }

        ActivatableTabItem CreateTabItem(IViewCreationCommand viewCreator, IServiceProvider serviceProvider)
        {
            View view = viewCreator.CreateView(serviceProvider);

            view.Title = viewCreator.DisplayName;
            var item = new ActivatableTabItem()
            {
                ViewCreator = viewCreator,
                View = view
            };

            ActivatableTabItem.SetTabItem(view, item);
            view.Site = item;
            item.SetBinding(ActivatableTabItem.ContentProperty, new Binding { Source = view, Path = new PropertyPath("ViewContent") });
            item.SetBinding(ActivatableTabItem.HeaderProperty, new Binding { Source = view, Path = new PropertyPath("Title") });
            AutomationProperties.SetAutomationId(item, viewCreator.RegisteredName);

            view.Closed += OnViewClosed;

            return item;
        }

        public ActivatableTabItem CreateView(IViewCreationCommand viewCreator, IServiceProvider serviceProvider, TabNode destinationNode = null)
        {
            ActivatableTabItem item = CreateTabItem(viewCreator, serviceProvider);

            if (destinationNode == null)
            {
                destinationNode = this.activeTabNode;
            }

            AddItemToControl(item, destinationNode.TabControl, true);
            return item;
        }

        public void AddItemToControl(ActivatableTabItem item, ActivatableTabControl tabControl, bool giveFocus)
        {
            Debug.Assert(tabControl.FindParent<SplitTabsControl>() == this, "Can't add item to a tab control not owned by this split tabs control!");

            if (giveFocus)
            {
                RoutedEventHandler loadedHandler = null;

                loadedHandler = (s, e2) =>
                {
                    item.Focus();
                    item.Loaded -= loadedHandler;
                };
                item.Loaded += loadedHandler;
            }

            tabControl.ParentSite = this;
            tabControl.Items.Add(item);
        }

        void OnTabActivated(object sender, RoutedEventArgs e)
        {
            ActivatableTabItem item = e.OriginalSource as ActivatableTabItem;

            if (item == null)
                return;

            if (item.View != null)
            {
                item.View.HandleActivation();
            }

            TabNode node = item.TabControlParent.TabNode;

            if (node != null)
            {
                ActivateTabNode(node);
            }
        }

        public void RemoveItemFromControl(ActivatableTabItem item)
        {
            var control = item.FindParent<ActivatableTabControl>();

            if (control == null)
            {
                Debug.Fail("Can't find parent tab control!");
                return;
            }

            var node = control.TabNode;

            if (node == null)
            {
                Debug.Fail("Tab control does not have a tab node set!");
                return;
            }

            node.TabControl.Items.Remove(item);
            if (node.TabControl.Items.Count == 0)
            {
                bool wasActive = (node == this.activeTabNode);

                // No more tabs in this control.
                if (node.Parent != null)
                {
                    // This node is no longer needed.  Remove the tab control and fix the tree.
                    this.host.Children.Remove(node.TabControl);

                    node.Parent.Children.Remove(node);
                    node.Parent.Slot.Children.Remove(node.Slot);

                    TabNode lastChild = node.Parent.Children[0];

                    if (node.Parent.Children.Count == 1)
                    {
                        // The parent now only has a single child. That means the last node (lastChild) is no longer
                        // necessary; its contents should collapse up into the parent. Note that this could "bleed up"
                        // into the grandparent, as in this case:
                        //
                        // ------------------------
                        // |      |   |   |       |
                        // |      |   |   |       |
                        // |      |   |   |       |
                        // |      |-------|       |
                        // |      |       |       |
                        // |      |   X   |       |
                        // |      |       |       |
                        // ------------------------
                        // 
                        // Closing the X node leaves a single node in its parent, so that node replaces the parent.
                        // That node contains two children, and has horizontal orientation like it's *new* parent
                        // (the grandparent).  In these cases, the parent node also becomes unnecessary, and should 
                        // be replaced in the grandparent node with all of its children.
                        //
                        // Note that programmatically, it is possible to create a node tree such that node has children
                        // (with grandchildren) that have the same orientation as the parent.  However, using the drag
                        // manager, this should never happen; the logic in SplitNode detects such cases and inserts
                        // nodes appropriately such that orientation always flips as you go down the tree.  So, any time
                        // the remaining child (lastChild) has children, it should always result in this "bleed up"
                        // effect. But we check just in case.
                        if (lastChild.Children != null)
                        {
                            var grandparent = node.Parent.Parent;

                            if (grandparent != null && lastChild.Slot.Orientation == grandparent.Slot.Orientation)
                            {
                                // Here's the "bleed up" -- skip right past parent
                                int index = grandparent.Children.IndexOf(node.Parent);
                                double totalSizeInLastChild = lastChild.Slot.Children.Sum(s => s.Length.Value);
                                double totalSizeInGrandParent = grandparent.Slot.Children.Sum(s => s.Length.Value);

                                grandparent.Children.RemoveAt(index);
                                grandparent.Slot.Children.RemoveAt(index);
                                foreach (var child in lastChild.Children)
                                {
                                    grandparent.Children.Insert(index, child);
                                    child.Parent = grandparent;
                                    grandparent.Slot.Children.Insert(index, child.Slot);
                                    child.Slot.Length = new GridLength((child.Slot.Length.Value / totalSizeInLastChild) * totalSizeInGrandParent, GridUnitType.Star);
                                    index += 1;
                                }
                            }
                            else
                            {
                                // Just put lastChild's children into the parent, replacing itself.  This case should only
                                // get hit if the parent is the root, unless someone does programmatic manipulation of the slots.
                                node.Parent.Children.Clear();
                                node.Parent.Slot.Children.Clear();
                                foreach (var child in lastChild.Children)
                                {
                                    node.Parent.Children.Add(child);
                                    child.Parent = node.Parent;
                                    node.Parent.Slot.Children.Add(child.Slot);
                                }
                                node.Parent.Slot.Orientation = lastChild.Slot.Orientation;
                            }
                        }
                        else
                        {
                            // Just put lastChild's tab control into the parent, removing itself
                            node.Parent.Children = null;
                            node.Parent.Slot.Children.Clear();
                            node.Parent.TabControl = lastChild.TabControl;
                            SlotPanel.SetSlotName(node.Parent.TabControl, node.Parent.Slot.Name);
                            node.Parent.TabControl.TabNode = node.Parent;
                        }
                    }

                    if (wasActive)
                    {
                        ActivateLastActiveTabNode();
                    }
                }
                else
                {
                    // We're now empty, completely.  If we're a floating host, go away.
                    if (this.masterControl != null)
                    {
                        var parent = this.FindParent<FloatingWindow>();

                        if (parent != null)
                            parent.Close();
                    }
                }
            }
        }

        public ActivatableTabItem FindView(IViewCreationCommand creator)
        {
            return FindViewInTabNode(this.rootTabNode, i => i.ViewCreator == creator);
        }

        public ActivatableTabItem FindView(object view)
        {
            return FindViewInTabNode(this.rootTabNode, i => i.Content == view);
        }

        public ActivatableTabItem FindView(Func<ActivatableTabItem, bool> predicate)
        {
            return FindViewInTabNode(this.rootTabNode, predicate);
        }

        ActivatableTabItem FindViewInTabNode(TabNode node, Func<ActivatableTabItem, bool> predicate)
        {
            if (node.Children != null && node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    var item = FindViewInTabNode(child, predicate);
                    if (item != null)
                        return item;
                }

                return null;
            }

            if (node.TabControl != null)
            {
                return node.TabControl.Items.OfType<ActivatableTabItem>().FirstOrDefault(predicate);
            }

            return null;
        }

        void OnTabClosed(object sender, RoutedEventArgs e)
        {
            ActivatableTabItem item = e.OriginalSource as ActivatableTabItem;

            if (item != null)
            {
                var view = item.View;

                if (view != null)
                {
                    // Note that "tab closed" is different than "view closed"
                    // This allows programmatic closing of a view by calling Close on the actual View
                    // object, and having it have the same effect as clicking the close box on a tab.
                    view.Close();
                }
            }
        }

        // NOTE:  This method is static, because the actual SplitTabsControl that created the view may not be the one in which
        // that view lives at the time it is closed.  Being static forces the method to use the sender to obtain the appropriate
        // parent control.
        static void OnViewClosed(object sender, EventArgs e)
        {
            View view = sender as View;

            if (view == null)
                return;

            ActivatableTabItem item = ActivatableTabItem.GetTabItem(view);
            ActivatableTabControl tabControl = item.TabControlParent;
            SplitTabsControl splitTabsControl = tabControl.FindParent<SplitTabsControl>();
            TabNode node = tabControl.TabNode;
            bool switchFocus = tabControl.IsKeyboardFocusWithin;

            splitTabsControl.RemoveItemFromControl(item);

            if (switchFocus)
            {
                if (node.TabControl.SelectedItem is ActivatableTabItem)
                {
                    ((ActivatableTabItem)node.TabControl.SelectedItem).Focus();
                }
                else if (node == splitTabsControl.activeTabNode)
                {
                    // We just closed the last view in the active node.  Need to switch the active
                    // tab node to the last active one.  
                    splitTabsControl.ActivateLastActiveTabNode();
                }
            }

            view.Closed -= OnViewClosed;
        }

        void OnNewHorizontalTabGroupExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.CreateNewTabGroup(Orientation.Horizontal);
        }

        void OnNewTabGroupCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.activeTabNode.TabControl.Items.Count > 1 && this.activeTabNode.TabControl.SelectedItem != null;
        }

        void OnNewVerticalTabGroupExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.CreateNewTabGroup(Orientation.Vertical);
        }

        void CreateNewTabGroup(Orientation orientation)
        {
            TabNode newTab = new TabNode { Slot = new Slot { Name = GetNextSlotName() }, TabControl = new ActivatableTabControl() };
            ActivatableTabControl originTabControl = this.activeTabNode.TabControl;
            TabNode nodeToSplit;
            double starSize;
            int newIndex;

            SlotPanel.SetSlotName(newTab.TabControl, newTab.Slot.Name);
            host.Children.Add(newTab.TabControl);
            newTab.TabControl.TabNode = newTab;

            if (this.activeTabNode.Parent != null && this.activeTabNode.Parent.Slot.Orientation == orientation)
            {
                // We're further splitting the parent in the same orientation, so we are only adding the one node
                nodeToSplit = this.activeTabNode.Parent;
                newTab.Parent = this.activeTabNode.Parent;
                newIndex = nodeToSplit.Children.IndexOf(this.activeTabNode) + 1;

                starSize = 0d;
                foreach (var d in nodeToSplit.Slot.Children)
                    starSize += d.Length.Value;

                starSize = starSize / nodeToSplit.Slot.Children.Count;

                // The currently active tab doesn't get altered, but the parent gets a new child.
                nodeToSplit.Children.Insert(newIndex, newTab);
                nodeToSplit.Slot.Children.Insert(newIndex, newTab.Slot);
            }
            else
            {
                // This is a new split of the active node, so we need to add a pair of child nodes
                TabNode top = new TabNode { Parent = this.activeTabNode, Slot = new Slot { Name = GetNextSlotName() }, TabControl = originTabControl };

                nodeToSplit = this.activeTabNode;
                newTab.Parent = this.activeTabNode;

                SlotPanel.SetSlotName(top.TabControl, top.Slot.Name);
                top.TabControl.TabNode = top;

                nodeToSplit.Slot.Children.Add(top.Slot);
                nodeToSplit.Slot.Children.Add(newTab.Slot);
                nodeToSplit.Slot.Orientation = orientation;
            }

            // Move the active tab to the newly created tab control
            var item = originTabControl.SelectedItem as ActivatableTabItem;

            if (item != null)
            {
                if (!item.IsKeyboardFocusWithin)
                    item.Focus();

                var focusedElement = Keyboard.FocusedElement as FrameworkElement;

                originTabControl.Items.Remove(item);
                newTab.TabControl.Items.Add(item);

                // This may not work, depending on template behavior (recreation of elements?)
                if (focusedElement != null)
                {
                    RoutedEventHandler reacquireFocus = null;
                    reacquireFocus = (s, e) =>
                    {
                        focusedElement.Focus();
                        focusedElement.Loaded -= reacquireFocus;
                    };
                    focusedElement.Loaded += reacquireFocus;
                }
            }
            else
            {
                Debug.Fail("Only ActivatableTabItem objects should be added to ActivatableTabControls");
            }

            ActivateTabNode(newTab);
        }

        public TabNode SplitNode(TabNode nodeToSplit, Dock newSideDock)
        {
            Orientation orientation = (newSideDock == Dock.Left || newSideDock == Dock.Right) ? Orientation.Horizontal : Orientation.Vertical;
            TabNode newNode = new TabNode { Slot = new Slot { Name = GetNextSlotName() }, TabControl = new ActivatableTabControl() };

            SlotPanel.SetSlotName(newNode.TabControl, newNode.Slot.Name);
            host.Children.Add(newNode.TabControl);
            newNode.TabControl.TabNode = newNode;

            // There are different ways a node can be "split".
            //      1:  The node HAS a parent node with the same orientation as the requested split.  The new node is inserted into the parent node's children.
            //      2:  The node IS a parent node with the same orientation as the requested split.  The new node is appended/prepended to this node's children.
            //      3:  The node "splits", one child being the new node and the other having the original tab control / children.

            if (nodeToSplit.Parent != null && nodeToSplit.Parent.Slot.Orientation == orientation)
            {
                // This is case 1.  Insert the new node into the parent, either before or after this node based on dock.
                double newSize = nodeToSplit.Parent.Children.Average(n => n.Slot.Length.Value);
                newNode.Slot.Length = new GridLength(newSize, GridUnitType.Star);

                int index = nodeToSplit.Parent.Children.IndexOf(nodeToSplit);

                if (newSideDock == Dock.Right || newSideDock == Dock.Bottom)
                {
                    index += 1;
                }

                nodeToSplit.Parent.Children.Insert(index, newNode);
                nodeToSplit.Parent.Slot.Children.Insert(index, newNode.Slot);

                newNode.Parent = nodeToSplit.Parent;
            }
            else if (nodeToSplit.Children != null && nodeToSplit.Slot.Orientation == orientation)
            {
                double newSize = nodeToSplit.Children.Average(n => n.Slot.Length.Value);
                newNode.Slot.Length = new GridLength(newSize, GridUnitType.Star);

                // This is case 2.  Insert the new node into this node (at the beginning or end based on dock).
                if (newSideDock == Dock.Left || newSideDock == Dock.Top)
                {
                    nodeToSplit.Children.Insert(0, newNode);
                    nodeToSplit.Slot.Children.Insert(0, newNode.Slot);
                }
                else
                {
                    nodeToSplit.Children.Add(newNode);
                    nodeToSplit.Slot.Children.Add(newNode.Slot);
                }

                newNode.Parent = nodeToSplit;
            }
            else
            {
                // This is case 3.  Create another new node containing nodeToSplit's content, and make nodeToSplit contain both new nodes.
                TabNode otherNewNode = new TabNode { Slot = new Slot { Name = GetNextSlotName() } };

                if (nodeToSplit.Children != null)
                {
                    otherNewNode.Children = nodeToSplit.Children;
                    foreach (var child in otherNewNode.Children)
                        child.Parent = otherNewNode;
                    nodeToSplit.Children = null;
                    foreach (var slot in nodeToSplit.Slot.Children)
                        otherNewNode.Slot.Children.Add(slot);
                    nodeToSplit.Slot.Children.Clear();
                    otherNewNode.Slot.Orientation = nodeToSplit.Slot.Orientation;
                }
                else
                {
                    otherNewNode.TabControl = nodeToSplit.TabControl;
                    otherNewNode.TabControl.TabNode = otherNewNode;
                    SlotPanel.SetSlotName(otherNewNode.TabControl, otherNewNode.Slot.Name);
                }

                nodeToSplit.Children = new List<TabNode>(2);
                nodeToSplit.Slot.Orientation = orientation;
                if (newSideDock == Dock.Left || newSideDock == Dock.Top)
                {
                    nodeToSplit.Slot.Children.Add(newNode.Slot);
                    nodeToSplit.Children.Add(newNode);
                    nodeToSplit.Slot.Children.Add(otherNewNode.Slot);
                    nodeToSplit.Children.Add(otherNewNode);
                }
                else
                {
                    nodeToSplit.Slot.Children.Add(otherNewNode.Slot);
                    nodeToSplit.Children.Add(otherNewNode);
                    nodeToSplit.Slot.Children.Add(newNode.Slot);
                    nodeToSplit.Children.Add(newNode);
                }

                newNode.Parent = otherNewNode.Parent = nodeToSplit;
            }

            return newNode;
        }

        public FloatingWindow CreateFloatingHost()
        {
            return CreateFloatingHost(true);
        }

        FloatingWindow CreateFloatingHost(bool activateRoot)
        {
            // Only the "master" control creates floating hosts, and manages the list
            if (this.masterControl != null)
            {
                return this.masterControl.CreateFloatingHost(activateRoot);
            }

            var window = new FloatingWindow(new SplitTabsControl { masterControl = this });

            window.Loaded += (s, e) =>
            {
                // Some things must be delayed until the window is shown.  Note that we "steal"
                // command bindings from our parent window (which is the app's main window) so
                // key bindings continue to work.
                window.Owner = this.FindParent<Window>();
                window.CommandBindings.AddRange(window.Owner.CommandBindings);

                // The activateRoot flag is set to false if we're creating this floating window as
                // part of window state loading, in which case the correct tab will be made active
                // as specified in the stored state.
                if (activateRoot)
                {
                    window.SplitTabsControl.ActivateTabNode(window.SplitTabsControl.rootTabNode);
                }
            };
            window.Closed += (s, e) =>
            {
                this.floatingWindows.Remove(window);
            };
            window.Activated += (s, e) =>
            {
                window.ActivationIndex = ++nextActivationIndex;
            };

            if (this.floatingWindows == null)
            {
                this.floatingWindows = new List<FloatingWindow>();
            }

            this.floatingWindows.Add(window);
            return window;
        }

        public IEnumerable<FloatingWindow> FloatingWindows
        {
            get
            {
                if (this.masterControl != null)
                {
                    return this.masterControl.FloatingWindows;
                }

                if (this.floatingWindows == null)
                {
                    return Enumerable.Empty<FloatingWindow>();
                }

                return this.floatingWindows.OrderBy(w => w.ActivationIndex);
            }
        }

        public IActivationSite ParentSite { get; set; }

        void IActivationSite.BubbleActivation(object child)
        {
            var tabControl = child as ActivatableTabControl;

            if ((tabControl != null) && (this.ParentSite != null))
            {
                this.lastActiveChildSite = tabControl;
                this.ParentSite.BubbleActivation(this);
            }
        }

        void IActivationSite.TunnelActivation()
        {
            if (this.lastActiveChildSite == null)
            {
                if (this.host != null)
                {
                    // We need to pick one.  Pick the biggest one by area.
                    var slotTable = new Dictionary<string, ActivatableTabControl>();

                    foreach (var child in this.host.Children.OfType<ActivatableTabControl>())
                    {
                        slotTable[SlotPanel.GetSlotName(child)] = child;
                    }

                    var pair = slotTable.OrderBy(kvp => kvp.Value.ActualHeight * kvp.Value.ActualWidth).FirstOrDefault();

                    this.lastActiveChildSite = pair.Value;
                }
            }

            if (this.lastActiveChildSite != null)
            {
                this.lastActiveChildSite.TunnelActivation();
            }
        }

        void IActivationSite.NotifyActivation(object child)
        {
            var tabControl = child as ActivatableTabControl;

            if ((tabControl != null) && (this.ParentSite != null))
            {
                this.lastActiveChildSite = tabControl;
                this.ParentSite.NotifyActivation(this);
            }
        }
    }
}
