//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Xbox.Tools.Shared
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Collections;
    using System.Windows.Input;

    /// <summary>
    /// Subclass ListViewItem to add tree-like behaviors
    /// </summary>
    public class TreeGridViewItem : ListViewItem
    {
        public static readonly RoutedCommand ExpandCommand = new RoutedCommand("Expand", typeof(TreeGridViewItem));
        public static readonly RoutedCommand ExpandFullyCommand = new RoutedCommand("ExpandFully", typeof(TreeGridViewItem));
        public static readonly RoutedCommand ExpandAllCommand = new RoutedCommand("ExpandAll", typeof(TreeGridViewItem));

        #region Dependency Properties
        /// <summary>
        /// Dependency property for a boolean value indicating whether this item is expanded or not
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(
            "IsExpanded",
            typeof(bool),
            typeof(TreeGridViewItem),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsExpandedChanged)));

        /// <summary>
        /// Dependency property for a boolean value indicating whether this item has child items
        /// </summary>
        public static readonly DependencyProperty HasChildrenProperty =
            DependencyProperty.Register(
            "HasChildren",
            typeof(bool),
            typeof(TreeGridViewItem),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Dependency property for an integer value indicating the nesting level this item is at in the tree
        /// </summary>
        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register(
            "Level",
            typeof(int),
            typeof(TreeGridViewItem),
            new FrameworkPropertyMetadata(0));

        #endregion

        public TreeGridViewItem()
        {
            this.CommandBindings.Add(new CommandBinding(ExpandCommand, OnExpandExecuted, OnExpandCanExecute));
            this.CommandBindings.Add(new CommandBinding(ExpandFullyCommand, OnExpandFullyExecuted, OnExpandFullyCanExecute));
        }

        #region Properties

        #region Dependency Property Wrappers

        /// <summary>
        /// Gets or sets a value indicating whether the item is expanded or not.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return (bool)this.GetValue(TreeGridViewItem.IsExpandedProperty);
            }
            set
            {
                this.SetValue(TreeGridViewItem.IsExpandedProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the item has child items or not
        /// </summary>
        public bool HasChildren
        {
            get
            {
                return (bool)this.GetValue(TreeGridViewItem.HasChildrenProperty);
            }
            set
            {
                this.SetValue(TreeGridViewItem.HasChildrenProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the nesting level this item is at in the tree
        /// </summary>
        public int Level
        {
            get
            {
                return (int)this.GetValue(TreeGridViewItem.LevelProperty);
            }
            set
            {
                this.SetValue(TreeGridViewItem.LevelProperty, value);
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the parent TreeGridView control hosting this item
        /// </summary>
        internal TreeGridView ParentView
        {
            get;
            set;
        }
        #endregion

        void OnExpandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.HasChildren && !this.IsExpanded;
            e.Handled = true;
        }

        void OnExpandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            this.IsExpanded = true;
            e.Handled = true;
        }

        void ExpandFully()
        {
            if (this.ParentView != null)
            {
                TreeGridViewItemInfo info = this.DataContext as TreeGridViewItemInfo;
                if (info != null)
                {
                    this.ParentView.ExpandItemRecursive(info.Data);
                    this.IsExpanded = true;
                }
            }
        }

        void OnExpandFullyCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.HasChildren;
            e.Handled = true;
        }

        void OnExpandFullyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ExpandFully();
            e.Handled = true;
        }


        #region input
        /// <summary>
        /// Handles double click to toggle expansion state
        /// </summary>
        /// <param name="e">Mouse button event args</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (IsEnabled && (e.ClickCount % 2) == 0)
            {
                this.IsExpanded = !this.IsExpanded;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the keyboard navigation inside the tree
        /// </summary>
        /// <param name="e">Keyboard input event args</param>
        /// <remarks> 
        /// The standard keyboard navigation that is supported in TreeView is:
        /// +/-: expand/collapse the focused item
        /// left: if focused item is expanded, collapse it; otherwise move focus to parent item
        /// right: if focused item is collapsed, expanded it; otherwise move focus to first child item of focused item 
        /// up/down: move focus up/down an item; this is natively supported by ListView
        /// *: Expand recursively
        /// </remarks>
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            // the code is pretty much copied from WPF TreeViewItem.OnKeyDown
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                switch (e.Key)
                {
                    case Key.Add:
                        // expand item if not already expanded
                        if (IsEnabled && this.HasChildren && !this.IsExpanded)
                        {
                            this.IsExpanded = true;
                            e.Handled = true;
                        }
                        break;

                    case Key.Subtract:
                        // collapse item if not already collapsed
                        if (IsEnabled && this.IsExpanded)
                        {
                            this.IsExpanded = false;
                            e.Handled = true;
                        }
                        break;

                    case Key.Left:
                    case Key.Right:
                        if (this.LogicalLeft(e.Key))
                        {
                            // collapse item if expanded; otherwise move focus to parent item
                            if (!this.IsControlKeyDown() && IsEnabled)
                            {
                                if (!IsFocused)
                                {
                                    this.Focus();
                                }
                                else if (this.IsExpanded)
                                {
                                    this.IsExpanded = false;
                                }
                                else
                                {
                                    this.FocusParent();
                                }
                                e.Handled = true;
                            }

                        }
                        else
                        {
                            // expand item if collapsed, otherwise move focus to first child item
                            if (!this.IsControlKeyDown() && IsEnabled)
                            {
                                if (this.HasChildren && !this.IsExpanded)
                                {
                                    this.IsExpanded = true;
                                }
                                else if (this.HasChildren && this.IsExpanded)
                                {
                                    this.FocusFirstChild();
                                }
                                e.Handled = true;
                            }
                        }
                        break;
                    case Key.Multiply:
                        this.ExpandFully();
                        e.Handled = true;
                        break;
                } // switch
            }
        }

        /// <summary>
        /// Notifies parent view when the item gets focus
        /// </summary>
        /// <param name="e">The event args</param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            if (this.ParentView != null)
            {
                this.ParentView.NotifyItemGotFocus(this);
            }
        }

        #endregion

        #region DP changed callbacks

        // Callback function for IsExpanded dependency property
        // all it does is to ask parent view to expand/collapse the item accordingly
        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TreeGridViewItem container = (TreeGridViewItem)d;
            var info = container.DataContext as TreeGridViewItemInfo;

            bool oldValue = (bool)e.OldValue;
            bool newValue = (bool)e.NewValue;

            if (oldValue == false && newValue == true)
            {
                if (container.ParentView != null && info != null)
                {
                    container.ParentView.ExpandItem(info.Data);
                }
            }
            else if (oldValue == true && newValue == false)
            {
                if (container.ParentView != null && info != null)
                {
                    container.ParentView.CollapseItem(info.Data);
                }
            }
        }
        #endregion

        #region Input Helper
        private bool FocusFirstChild()
        {
            // we know the first child is one node down
            return this.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
        }

        private bool FocusParent()
        {
            // we have to ask the view to find parent item
            if (this.ParentView != null)
            {
                return this.ParentView.FocusToParentItem(this);
            }
            return false;
        }

        // determine what left/right key really means, on a right-to-left layout they mean opposite direction
        private bool LogicalLeft(Key key)
        {
            bool invert = (FlowDirection == FlowDirection.RightToLeft);
            return (!invert && (key == Key.Left)) || (invert && (key == Key.Right));
        }

        private bool IsControlKeyDown()
        {
            return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        }
        #endregion

    }
}
