//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Xbox.Tools.Shared
{
    class TabItemDragManager
    {
        ActivatableTabItem draggedItem;
        ActivatableTabControl sourceControl;
        ActivatableTabControl controlUnderCursor;
        SplitTabsControl sourceSplitTabsControl;
        Window dragShadow;
        TabDropTargetWindow dropTarget;
        Point offset;
        bool preMoving;
        DependencyObject focusedElement;
        TabDockSpot hitTestSpot;
        Size dragWindowSize;

        public static void BeginDrag(ActivatableTabItem item, MouseButtonEventArgs e)
        {
            new TabItemDragManager().OnMouseLeftButtonDown(item, e);
        }

        private void OnMouseLeftButtonDown(ActivatableTabItem item, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(item);

            this.draggedItem = item;
            this.sourceControl = item.FindParent<ActivatableTabControl>();
            this.sourceSplitTabsControl = this.sourceControl.FindParent<SplitTabsControl>();

            this.offset = e.GetPosition(item);
            this.preMoving = true;

            item.LostMouseCapture += this.OnLostMouseCapture;
            item.MouseLeftButtonUp += this.OnMouseLeftButtonUp;
            item.MouseMove += this.OnMouseMove;
        }

        void StartMoving()
        {
            var content = this.draggedItem.Content as FrameworkElement;

            if (content != null)
            {
                this.dragWindowSize = new Size(content.ActualWidth, content.ActualHeight);
            }
            else
            {
                this.dragWindowSize = new Size(500, 300);
            }

            this.draggedItem.Cursor = Cursors.SizeAll;
            this.dragShadow = new Window
            {
                AllowsTransparency = true,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                Width = dragWindowSize.Width,
                Height = dragWindowSize.Height,
                Background = new SolidColorBrush(Color.FromArgb(0x70, 0xbf, 0xdf, 0xff)),
                Topmost = true,
                ShowActivated = false
            };

            var dragImage = new ActivatableTabControl() { Background = Brushes.Transparent, Opacity = 0.7 };
            dragImage.Items.Add(new ActivatableTabItem { Header = draggedItem.Header });
            this.dragShadow.Content = dragImage;
            this.dragShadow.Show();

            var pos = this.draggedItem.PointToScreenIndependent(this.offset);
            this.dragShadow.Left = pos.X - offset.X;
            this.dragShadow.Top = pos.Y - offset.Y;
            this.preMoving = false;

            this.focusedElement = Keyboard.FocusedElement as DependencyObject;
            if (this.focusedElement != null)
            {
                Keyboard.AddPreviewKeyDownHandler(this.focusedElement, new KeyEventHandler(OnPreviewKeyDown));
                Keyboard.AddPreviewKeyUpHandler(this.focusedElement, new KeyEventHandler(OnPreviewKeyUp));
            }
        }

        void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Mouse.Capture(null);
            }
            else if (e.Key == Key.RightCtrl || e.Key == Key.LeftCtrl)
            {
                UpdateHitState();
            }
        }

        void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.RightCtrl || e.Key == Key.LeftCtrl)
            {
                UpdateHitState();
            }
        }


        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(null);
            if (!this.preMoving)
            {
                MoveDraggedItemToNewControl(e);
            }
        }

        void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            this.draggedItem.LostMouseCapture -= this.OnLostMouseCapture;
            this.draggedItem.MouseLeftButtonUp -= this.OnMouseLeftButtonUp;
            this.draggedItem.MouseMove -= this.OnMouseMove;
            this.draggedItem.Cursor = null;

            if (this.dropTarget != null)
            {
                this.dropTarget.Close();
            }

            if (this.dragShadow != null)
            {
                this.dragShadow.Close();
            }

            if (this.focusedElement != null)
            {
                Keyboard.RemovePreviewKeyDownHandler(this.focusedElement, new KeyEventHandler(OnPreviewKeyDown));
                Keyboard.RemovePreviewKeyUpHandler(this.focusedElement, new KeyEventHandler(OnPreviewKeyUp));
            }
        }

        void OnMouseMove(object sender, MouseEventArgs exx)
        {
            UpdateHitState();
        }

        void UpdateHitState()
        {
            var pos = Mouse.GetPosition(this.draggedItem);

            if (this.preMoving)
            {
                var vector = pos - this.offset;
                if ((Math.Abs(vector.X) >= SystemParameters.MinimumHorizontalDragDistance) ||
                    (Math.Abs(vector.Y) >= SystemParameters.MinimumVerticalDragDistance))
                {
                    this.StartMoving();
                }
                else
                {
                    return;
                }
            }

            bool positionSet = false;
            var result = VisualTreeHelper.HitTest(Application.Current.MainWindow, Mouse.GetPosition(Application.Current.MainWindow));
            var newTargetControl = (result != null && result.VisualHit != null) ? result.VisualHit.FindParent<ActivatableTabControl>() : null;

            foreach (var floater in this.sourceSplitTabsControl.FloatingWindows)
            {
                result = VisualTreeHelper.HitTest(floater, Mouse.GetPosition(floater));
                if (result != null && result.VisualHit != null)
                {
                    newTargetControl = result.VisualHit.FindParent<ActivatableTabControl>() ?? newTargetControl;
                }
            }

            SetNewDropTarget(newTargetControl);

            if (this.dropTarget.IsVisible)
            {
                this.hitTestSpot = null;
                VisualTreeHelper.HitTest(this.dropTarget, this.HitTestFilter, this.HitTestResult, new PointHitTestParameters(Mouse.GetPosition(this.dropTarget)));
                if (this.hitTestSpot != null)
                {
                    var spot = this.hitTestSpot;

                    if (spot != null)
                    {
                        var rect = spot.DestinationNode.GetScreenRect();

                        if (spot.IsTabbed)
                        {
                            this.dragShadow.Left = rect.X;
                            this.dragShadow.Top = rect.Y;
                            this.dragShadow.Width = rect.Width;
                            this.dragShadow.Height = rect.Height;
                            positionSet = true;
                        }
                        else
                        {
                            switch (spot.Dock)
                            {
                                case Dock.Top:
                                    this.dragShadow.Left = rect.X;
                                    this.dragShadow.Top = rect.Y;
                                    this.dragShadow.Width = rect.Width;
                                    this.dragShadow.Height = rect.Height / 2;
                                    positionSet = true;
                                    break;
                                case Dock.Bottom:
                                    this.dragShadow.Width = rect.Width;
                                    this.dragShadow.Height = rect.Height / 2;
                                    this.dragShadow.Left = rect.X;
                                    this.dragShadow.Top = rect.Y + this.dragShadow.Height;
                                    positionSet = true;
                                    break;
                                case Dock.Left:
                                    this.dragShadow.Left = rect.X;
                                    this.dragShadow.Top = rect.Y;
                                    this.dragShadow.Width = rect.Width / 2;
                                    this.dragShadow.Height = rect.Height;
                                    positionSet = true;
                                    break;
                                case Dock.Right:
                                    this.dragShadow.Width = rect.Width / 2;
                                    this.dragShadow.Height = rect.Height;
                                    this.dragShadow.Left = rect.X + this.dragShadow.Width;
                                    this.dragShadow.Top = rect.Y;
                                    positionSet = true;
                                    break;
                            }
                        }
                    }
                }
            }

            if (!positionSet)
            {
                pos = this.draggedItem.PointToScreenIndependent(pos);
                this.dragShadow.Left = pos.X - offset.X;
                this.dragShadow.Top = pos.Y - offset.Y;
                this.dragShadow.Width = dragWindowSize.Width;
                this.dragShadow.Height = dragWindowSize.Height;
            }
        }

        HitTestFilterBehavior HitTestFilter(DependencyObject potentialHit)
        {
            TabDockSpot spot = potentialHit as TabDockSpot;

            if (spot != null && spot.IsHitTestVisible)
            {
                this.hitTestSpot = spot;
                return HitTestFilterBehavior.Stop;
            }

            return HitTestFilterBehavior.Continue;
        }

        HitTestResultBehavior HitTestResult(HitTestResult result)
        {
            return HitTestResultBehavior.Continue;
        }

        void SetNewDropTarget(ActivatableTabControl newTargetControl)
        {
            RebuildDropTargetWindow(newTargetControl);
            this.controlUnderCursor = newTargetControl;
        }

        void RebuildDropTargetWindow(ActivatableTabControl targetControl)
        {
            if (this.dropTarget == null)
            {
                this.dropTarget = new TabDropTargetWindow()
                {
                    AllowsTransparency = true,      // It would be nice to be able to set these in the style...
                    WindowStyle = WindowStyle.None,
                };
            }

            bool controlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            if (targetControl != null && !controlDown)
            {
                TabNode node = targetControl.TabNode;

                if (!this.dropTarget.IsVisible || this.dropTarget.TargetNode != node)
                {
                    this.dropTarget.Hide();
                    this.dropTarget.UpdateLayout();

                    var pos = new Point(0, 0);
                    pos = targetControl.PointToScreenIndependent(pos);

                    this.dropTarget.Left = pos.X;
                    this.dropTarget.Top = pos.Y;
                    this.dropTarget.Width = targetControl.ActualWidth;
                    this.dropTarget.Height = targetControl.ActualHeight;

                    var parent = node.Parent;
                    var grandparent = parent == null ? null : parent.Parent;

                    this.dropTarget.VerticalParentNode = parent;
                    this.dropTarget.HorizontalParentNode = parent;

                    if (grandparent != null && parent.Slot.Orientation == Orientation.Vertical)
                    {
                        this.dropTarget.VerticalParentNode = grandparent;
                    }
                    else if (grandparent != null && parent.Slot.Orientation == Orientation.Horizontal)
                    {
                        this.dropTarget.HorizontalParentNode = grandparent;
                    }

                    this.dropTarget.TargetNode = node;
                    this.dropTarget.IsTabbedSpotVisible = true;
                    this.dropTarget.AreDockSpotsVisible = (targetControl != this.sourceControl || this.sourceControl.Items.Count > 1);
                    this.dropTarget.Show();
                    this.dropTarget.UpdateLayout();
                }
            }
            else
            {
                this.dropTarget.Hide();
            }
        }

        void MoveDraggedItemToNewControl(MouseButtonEventArgs e)
        {
            bool ctrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            if (ctrlDown || this.controlUnderCursor == null || this.hitTestSpot == null)
            {
                var pos = draggedItem.PointToScreenIndependent(e.GetPosition(draggedItem));
                var originalContent = draggedItem.Content as FrameworkElement;
                Size size = new Size(300, 200);

                if (originalContent != null)
                {
                    size = new Size(originalContent.ActualWidth, originalContent.ActualHeight);
                }

                var w = sourceSplitTabsControl.CreateFloatingHost();
                w.Left = pos.X;
                w.Top = pos.Y;
                w.Width = size.Width;
                w.Height = size.Height;

                w.SplitTabsControl.Loaded += (s, e2) =>
                {
                    sourceSplitTabsControl.RemoveItemFromControl(this.draggedItem);
                    w.SplitTabsControl.AddItemToControl(this.draggedItem, w.SplitTabsControl.RootNode.TabControl, true);
                };
                w.Show();

                return;
            }

            SplitTabsControl destSplitTabsControl = this.controlUnderCursor.FindParent<SplitTabsControl>();

            if (sourceSplitTabsControl == null || destSplitTabsControl == null)
            {
                Debug.Fail("How does a tab control not live in a split tabs control parent?");
                return;
            }

            // Before removing the dragged item (which presumably has focus), set focus on the destination
            // tab control.  Otherwise we end up removing the focused element from the tree, which tends to
            // confuse the WPF focus manager.
            destSplitTabsControl.Focus();

            if (this.hitTestSpot.IsTabbed)
            {
                if (this.controlUnderCursor != this.sourceControl)
                {
                    sourceSplitTabsControl.RemoveItemFromControl(this.draggedItem);
                    destSplitTabsControl.AddItemToControl(this.draggedItem, this.controlUnderCursor, true);
                }

                return;
            }

            var newNode = destSplitTabsControl.SplitNode(this.hitTestSpot.DestinationNode, this.hitTestSpot.Dock);

            if (newNode != null)
            {
                sourceSplitTabsControl.RemoveItemFromControl(this.draggedItem);
                destSplitTabsControl.AddItemToControl(this.draggedItem, newNode.TabControl, true);
            }

        }
    }
}
