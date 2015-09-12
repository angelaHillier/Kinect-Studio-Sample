//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class LayoutControl : Control
    {
        public static readonly DependencyProperty SlotDefinitionProperty = DependencyProperty.Register(
            "SlotDefinition", typeof(Slot), typeof(LayoutControl));

        public static readonly RoutedCommand ViewShortcutCommand = new RoutedCommand("ViewShortcut", typeof(LayoutControl)); 

        SlotPanel slotPanel;

        public LayoutControl(LayoutInstance layoutInstance)
        {
            this.LayoutInstance = layoutInstance;
            this.CommandBindings.Add(new CommandBinding(ViewShortcutCommand, OnViewShortcutExecuted));
        }

        public LayoutInstance LayoutInstance { get; private set; }

        public bool IsInEditMode { get { return this.LayoutInstance.IsInEditMode; } }

        public Slot SlotDefinition
        {
            get { return (Slot)GetValue(SlotDefinitionProperty); }
            set { SetValue(SlotDefinitionProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.slotPanel = GetTemplateChild("PART_SlotPanel") as SlotPanel;

            this.slotPanel.SetBinding(SlotPanel.SlotDefinitionProperty, new Binding { Source = this, Path = new PropertyPath(SlotDefinitionProperty) });
            this.LayoutInstance.EnsureSlotContentPopulation();
        }

        void OnViewShortcutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var site = e.Parameter as ViewSite;

            if (site != null && site.View != null)
            {
                site.View.Activate();

                var window = this.FindParent<ToolsUIWindow>();

                if (window != null)
                {
                    window.LeaveShortcutMode();
                }
            }
        }

        public void RemoveSlotPanelChild(FrameworkElement child)
        {
            if (this.slotPanel != null)
            {
                this.slotPanel.Children.Remove(child);
            }
        }

        public bool AddSlotPanelChild(FrameworkElement child, string slotName)
        {
            if (this.slotPanel != null)
            {
                SlotPanel.SetSlotName(child, slotName);
                this.slotPanel.Children.Add(child);
                return true;
            }

            return false;
        }

        public Slot GetSlotUnderPoint(Point pos)
        {
            if (this.slotPanel == null || this.LayoutInstance == null)
            {
                return null;
            }

            Slot slot;
            for (slot = this.slotPanel.SlotDefinition; slot != null && slot.Children.Count > 0; )
            {
                slot = slot.Children.FirstOrDefault(s => new Rect(s.UpperLeft, s.ActualSize).Contains(pos));
            }

            return slot;
        }

        public Rect GetSlotScreenRect(Slot slot)
        {
            if (this.slotPanel == null || slot == null)
            {
                return new Rect();
            }

            var upperLeft = this.slotPanel.PointToScreenIndependent(slot.UpperLeft);
            var lowerRight = this.slotPanel.PointToScreenIndependent(new Point(slot.UpperLeft.X + slot.ActualSize.Width, slot.UpperLeft.Y + slot.ActualSize.Height));

            return new Rect(upperLeft, lowerRight);
        }
    }
}
