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
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class LayoutDefinition : DependencyObject
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header", typeof(string), typeof(LayoutDefinition), new FrameworkPropertyMetadata(OnHeaderChanged));

        public static readonly DependencyProperty DocumentFactoryNameProperty = DependencyProperty.Register(
            "DocumentFactoryName", typeof(string), typeof(LayoutDefinition));

        public static readonly DependencyProperty ShortcutKeyProperty = DependencyProperty.Register(
            "ShortcutKey", typeof(string), typeof(LayoutDefinition));

        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(
            "IsVisible", typeof(bool), typeof(LayoutDefinition), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty IsNewPlaceholderProperty = DependencyProperty.Register(
            "IsNewPlaceholder", typeof(bool), typeof(LayoutDefinition));

        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(
            "Id", typeof(Guid), typeof(LayoutDefinition));

        static TypeConverter gridLengthConverter;
        static int nextActivationIndex = 0;

        public Slot SlotDefinition { get; private set; }
        public ObservableCollection<ViewSource> ViewSources { get; private set; }
        public int ActivationIndex { get; private set; }
        int nextSlotName;
        int nextViewId;

        public LayoutDefinition()
        {
            this.ViewSources = new ObservableCollection<ViewSource>();
            this.SlotDefinition = new Slot { Name = (this.nextSlotName++).ToString() };
        }

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public string DocumentFactoryName
        {
            get { return (string)GetValue(DocumentFactoryNameProperty); }
            set { SetValue(DocumentFactoryNameProperty, value); }
        }

        public string ShortcutKey
        {
            get { return (string)GetValue(ShortcutKeyProperty); }
            set { SetValue(ShortcutKeyProperty, value); }
        }

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public bool IsNewPlaceholder
        {
            get { return (bool)GetValue(IsNewPlaceholderProperty); }
            set { SetValue(IsNewPlaceholderProperty, value); }
        }

        public Guid Id
        {
            get { return (Guid)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        public bool RevertedToDefault { get; set; }

        public event EventHandler HeaderChanged;
        public event EventHandler PlaceholderModified;

        public void SetVisibility(string documentFactoryName)
        {
            this.IsVisible = (this.DocumentFactoryName == null) || StringComparer.OrdinalIgnoreCase.Equals(documentFactoryName, this.DocumentFactoryName);
        }

        public XElement SaveState()
        {
            var state = new XElement("LayoutDefinition", new XAttribute("Name", this.Header), new XAttribute("Id", this.Id));

            if (this.DocumentFactoryName != null)
            {
                state.Add(new XAttribute("DocumentFactoryName", this.DocumentFactoryName));
            }

            WriteState(state);
            return state;
        }

        public void OnActivated()
        {
            this.ActivationIndex = ++nextActivationIndex;
        }

        public static LayoutDefinition LoadFromState(XElement state, IDictionary<string, IViewCreationCommand> viewCreators)
        {
            var page = new LayoutDefinition();

            page.Header = state.Attribute("Name").Value;
            page.ReadState(state, viewCreators);

            var docKindAttr = state.Attribute("DocumentFactoryName");

            if (docKindAttr != null)
            {
                page.DocumentFactoryName = docKindAttr.Value;
            }

            var idAttr = state.Attribute("Id");

            if (idAttr != null)
            {
                page.Id = Guid.Parse(idAttr.Value);
            }

            page.RecomputeViewShortcutKeys();
            return page;
        }

        void RecomputeViewShortcutKeys()
        {
            foreach (var viewSource in this.ViewSources)
            {
                viewSource.ShortcutKey = viewSource.ViewCreator.ShortcutKey;
            }
        }

        public ViewSource AddViewSource(IViewCreationCommand creator, Slot targetSlot, Dock? dock)
        {
            ViewSource source;

            if (creator == null)
            {
                return null;
            }

            if (this.ViewSources.Count == 0)
            {
                source = new ViewSource(this, this.nextViewId++, this.SlotDefinition.Name, creator);
                this.ViewSources.Add(source);

                var handler = this.PlaceholderModified;

                // Note that this is the only place where this event could fire (going from 0 to 1 sources)
                if (this.IsNewPlaceholder && handler != null)
                {
                    handler(this, EventArgs.Empty);
                }

                return source;
            }

            if (targetSlot == null)
            {
                return null;
            }

            Slot newSlot = targetSlot;

            if (dock.HasValue)
            {
                newSlot = SplitSlot(targetSlot, dock.Value);
            }

            source = new ViewSource(this, this.nextViewId++, newSlot.Name, creator);
            this.ViewSources.Add(source);

            RecomputeViewShortcutKeys();
            return source;
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public Slot RemoveViewSource(ViewSource source)
        {
            this.ViewSources.Remove(source);

            var slot = this.SlotDefinition.FindSlot(source.SlotName);

            if (slot == this.SlotDefinition)
            {
                // Never remove the root slot.
                slot = null;
            }

            if (this.ViewSources.Any(s => StringComparer.Ordinal.Equals(s.SlotName, source.SlotName)))
            {
                // There are still view sources in this slot, so leave it
                slot = null;
            }

            RecomputeViewShortcutKeys();
            return slot;
        }

        public static XElement BuildSlotElement(Slot slot)
        {
            return new XElement("Slot",
                new XAttribute("Name", slot.Name),
                new XAttribute("Length", slot.Length),
                new XAttribute("Orientation", slot.Orientation),
                slot.Children.Select(c => BuildSlotElement(c)));
        }

        public static Slot LoadSlotFromState(XElement slotElement)
        {
            if (gridLengthConverter == null)
            {
                gridLengthConverter = TypeDescriptor.GetConverter(typeof(GridLength));
            }

            Slot slot = new Slot();

            slot.Name = slotElement.Attribute("Name").Value;
            slot.Length = (GridLength)gridLengthConverter.ConvertFromString(null, CultureInfo.InvariantCulture, slotElement.Attribute("Length").Value);
            slot.Orientation = (Orientation)Enum.Parse(typeof(Orientation), slotElement.Attribute("Orientation").Value);

            foreach (var child in slotElement.Elements("Slot"))
            {
                slot.Children.Add(LoadSlotFromState(child));
            }

            return slot;
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public void WriteState(XElement element)
        {
            element.Add(BuildSlotElement(this.SlotDefinition),
                new XAttribute("NextSlotName", this.nextSlotName),
                new XAttribute("NextViewId", this.nextViewId),
                this.ViewSources.Select(p => new XElement("View",
                    new XAttribute("RegisteredName", p.ViewCreator.RegisteredName),
                    new XAttribute("SlotName", p.SlotName),
                    new XAttribute("Id", p.Id))));
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public void ReadState(XElement element, IDictionary<string, IViewCreationCommand> viewCreators)
        {
            this.nextSlotName = int.Parse(element.Attribute("NextSlotName").Value);
            this.nextViewId = int.Parse(element.Attribute("NextViewId").Value);

            this.SlotDefinition = LoadSlotFromState(element.Element("Slot"));
            foreach (var viewElement in element.Elements("View"))
            {
                var id = int.Parse(viewElement.Attribute("Id").Value);
                var slotName = viewElement.Attribute("SlotName").Value;
                IViewCreationCommand creator;

                if (viewCreators.TryGetValue(viewElement.Attribute("RegisteredName").Value, out creator))
                {
                    var viewSource = new ViewSource(this, id, slotName, creator);
                    this.ViewSources.Add(viewSource);
                }
            }
        }

        Slot SplitSlot(Slot targetSlot, Dock dock)
        {
            Orientation orientation = (dock == Dock.Left || dock == Dock.Right) ? Orientation.Horizontal : Orientation.Vertical;
            Slot newSlot = new Slot() { Name = (this.nextSlotName++).ToString() };

            // Splitting a slot by simply dividing the target slot can create an undesirable "split tree" effect, where slots with the same orientation
            // are in a tree rather than an array.  This makes the splitters behave incorrectly because the side of the tree with (grand)children is sized as a
            // single unit and the change is proportionally distributed.  Slot children that have the same orientation should all be parented by the same node.
            //
            // To avoid a "split tree", there are 3 cases to handle in a slot split.
            //      1:  The slot HAS a parent slot with the same orientation as the requested split.  The new slot is inserted into the parent slot's children.
            //      2:  The slot IS a parent slot with the same orientation as the requested split.  The new slot is appended/prepended to this slot's children.
            //      3:  The slot gets replaced by a new parent slot with two children, one being the existing slot and the other being a newly created/returned slot.
            if (targetSlot != this.SlotDefinition && targetSlot.Parent.Orientation == orientation)
            {
                // This is case 1.  Insert the new slot into the parent, either before or after the target slot based on dock.
                double newSize = targetSlot.Parent.Children.Average(n => n.Length.Value);
                newSlot.Length = new GridLength(newSize, GridUnitType.Star);

                int index = targetSlot.Parent.Children.IndexOf(targetSlot);

                if (dock == Dock.Right || dock == Dock.Bottom)
                {
                    index += 1;
                }

                // Note, adding child slots to a parent automatically assigns the child's Parent property
                targetSlot.Parent.Children.Insert(index, newSlot);
            }
            else if (targetSlot.Children.Count > 0 && targetSlot.Orientation == orientation)
            {
                // This is case 2.  Insert the new slot into the target slot (at the beginning or end based on dock).
                double newSize = targetSlot.Children.Average(n => n.Length.Value);
                newSlot.Length = new GridLength(newSize, GridUnitType.Star);

                if (dock == Dock.Left || dock == Dock.Top)
                {
                    targetSlot.Children.Insert(0, newSlot);
                }
                else
                {
                    targetSlot.Children.Add(newSlot);
                }
            }
            else
            {
                // This is case 3.  Theoretically, we replace targetSlot with a new slot with two children, one being targetSlot,
                // and the other being the new slot created by the split.  In practice, though, we don't actually *replace* targetSlot
                // because it may be the root.  So what we *really* do is create two new slots: A (the new slot) and B (the new targetSlot).
                // Then rename targetSlot and move its children (if any) to slot B, and name slot B with targetSlot's old name.
                Slot newSlotB = new Slot { Name = targetSlot.Name, Orientation = targetSlot.Orientation };

                // Add the children from targetSlot to newSlotB first, which re-assigns Parent for each of them...
                foreach (var child in targetSlot.Children)
                {
                    newSlotB.Children.Add(child);
                }

                // And then remove them one-at-a-time (so the slot sees each one and disconnects.  .Clear does a reset)
                while (targetSlot.Children.Count > 0)
                {
                    targetSlot.Children.RemoveAt(targetSlot.Children.Count - 1);
                }

                // Rename targetSlot and set its orientation
                targetSlot.Name = (this.nextSlotName++).ToString();
                targetSlot.Orientation = orientation;

                // Add the new children to targetSlot
                if (dock == Dock.Left || dock == Dock.Top)
                {
                    targetSlot.Children.Add(newSlot);
                    targetSlot.Children.Add(newSlotB);
                }
                else
                {
                    targetSlot.Children.Add(newSlotB);
                    targetSlot.Children.Add(newSlot);
                }
            }

            return newSlot;
        }

        static void OnHeaderChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            LayoutDefinition layoutDef = obj as LayoutDefinition;

            if (layoutDef != null)
            {
                var handler = layoutDef.HeaderChanged;
                if (handler != null)
                {
                    handler(layoutDef, EventArgs.Empty);
                }
            }
        }

    }
}
