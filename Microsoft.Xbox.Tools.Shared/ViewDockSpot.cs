//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ViewDockSpot : Control
    {
        public static readonly DependencyProperty DockProperty = DependencyProperty.Register(
            "Dock", typeof(Dock), typeof(ViewDockSpot));

        public static readonly DependencyProperty IsTabbedProperty = DependencyProperty.Register(
            "IsTabbed", typeof(bool), typeof(ViewDockSpot));

        public static readonly DependencyProperty DestinationSlotProperty = DependencyProperty.Register(
            "DestinationSlot", typeof(Slot), typeof(ViewDockSpot));

        public Slot DestinationSlot
        {
            get { return (Slot)GetValue(DestinationSlotProperty); }
            set { SetValue(DestinationSlotProperty, value); }
        }
        
        public bool IsTabbed
        {
            get { return (bool)GetValue(IsTabbedProperty); }
            set { SetValue(IsTabbedProperty, value); }
        }

        public Dock Dock
        {
            get { return (Dock)GetValue(DockProperty); }
            set { SetValue(DockProperty, value); }
        }
    }
}
