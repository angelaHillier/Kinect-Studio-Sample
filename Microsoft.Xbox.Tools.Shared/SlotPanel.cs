//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Xbox.Tools.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public class SlotPanel : Panel
    {
        public static readonly DependencyProperty SlotDefinitionProperty = DependencyProperty.Register(
            "SlotDefinition", typeof(Slot), typeof(SlotPanel), new FrameworkPropertyMetadata(null, OnSlotDefinitionChanged));

        public static readonly DependencyProperty SlotSpacingProperty = DependencyProperty.Register(
            "SlotSpacing", typeof(double), typeof(SlotPanel), new FrameworkPropertyMetadata((double)4));

        public static readonly DependencyProperty SlotNameProperty = DependencyProperty.RegisterAttached(
            "SlotName", typeof(string), typeof(SlotPanel), new FrameworkPropertyMetadata(null, OnSlotNameChanged));

        Dictionary<string, SlotData> slotTable;
        SlotData topSlotData;
        bool slotStructureValid;
        List<SlotSizer> sizers = new List<SlotSizer>();
        int sizerCursor;

        public SlotPanel()
        {
            topSlotData = new SlotData() { Slot = new Slot() { Name = "" } };
        }

        public Slot SlotDefinition
        {
            get { return (Slot)GetValue(SlotDefinitionProperty); }
            set { SetValue(SlotDefinitionProperty, value); }
        }

        public double SlotSpacing
        {
            get { return (double)GetValue(SlotSpacingProperty); }
            set { SetValue(SlotSpacingProperty, value); }
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return base.VisualChildrenCount + sizerCursor;
            }
        }

        void AddSlotToTable(Slot slot)
        {
            slotTable[InternalGetSlotName(slot)] = new SlotData() { Slot = slot };
            foreach (var child in slot.Children)
            {
                AddSlotToTable(child);
            }
        }

        SlotSizer GetNextSizer()
        {
            if (sizerCursor == sizers.Count)
            {
                var sizer = new SlotSizer();
                sizers.Add(sizer);
                AddVisualChild(sizer);
            }
            return sizers[sizerCursor++];
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            sizerCursor = 0;
            ArrangeSlot(topSlotData, new Point(0, 0), finalSize);
            for (int i = sizerCursor; i < sizers.Count; i++)
            {
                RemoveVisualChild(sizers[i]);
            }
            if (sizerCursor < sizers.Count)
            {
                sizers.RemoveRange(sizerCursor, sizers.Count - sizerCursor);
            }
            return finalSize;
        }

        void ArrangeSlot(SlotData slotData, Point upperLeft, Size finalSize)
        {
            UVHelper uv = UVHelper.CreateInstance(slotData.Slot.Orientation);
            Point runningUpperLeft = upperLeft;
            double maxU = uv.U(runningUpperLeft) + uv.U(finalSize);
            bool addSizer = false;
            Slot previousSlot = null;

            foreach (var child in slotData.Slot.Children)
            {
                if (addSizer)
                {
                    SlotSizer sizer = GetNextSizer();
                    sizer.SizeDirection = slotData.Slot.Orientation;
                    sizer.Slot1 = previousSlot;
                    sizer.Slot2 = child;
                    sizer.Measure(uv.Size(SlotSpacing, uv.V(finalSize)));
                    sizer.Arrange(new Rect(uv.Point(uv.U(runningUpperLeft), uv.V(runningUpperLeft)), uv.Size(SlotSpacing, uv.V(finalSize))));
                    runningUpperLeft = uv.Point(Math.Min(maxU, uv.U(runningUpperLeft) + SlotSpacing), uv.V(runningUpperLeft));
                }

                var childSlot = slotTable[InternalGetSlotName(child)];
                var childFinalSize = uv.Size(Math.Min(uv.U(finalSize), uv.U(childSlot.FinalSize)), uv.V(finalSize));

                ArrangeSlot(childSlot, runningUpperLeft, childFinalSize);
                runningUpperLeft = uv.Point(Math.Min(maxU, uv.U(runningUpperLeft) + uv.U(childFinalSize)), uv.V(runningUpperLeft));
                addSizer = (SlotSpacing > 0);
                previousSlot = child;
            }

            if (slotData.Elements != null)
            {
                foreach (UIElement element in slotData.Elements)
                {
                    element.Arrange(new Rect(upperLeft, finalSize));
                }
            }

            slotData.Slot.ActualSize = finalSize;
            slotData.Slot.UpperLeft = upperLeft;
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
                         Justification = "API is only intended for UI elements, not DependencyObject")]
        public static string GetSlotName(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }

            return (string)element.GetValue(SlotNameProperty);
        }

        private static string InternalGetSlotName(Slot child)
        {
            return child.Name ?? "";
        }

        protected override Visual GetVisualChild(int index)
        {
            int sizerCount = this.sizers.Count;

            if (index < sizerCount)
            {
                return sizers[index];                
            }

            return base.GetVisualChild(index - sizerCount);
        }

        private static double MaxNonInfinite(double possiblyInfinite, double other)
        {
            return double.IsPositiveInfinity(possiblyInfinite) ? other : Math.Max(possiblyInfinite, other);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateSlotTable();
            MeasureSlot(topSlotData, availableSize);
            return topSlotData.DesiredSize;
        }

        void MeasureSlot(SlotData slotData, Size availableSize)
        {
            UVHelper uv = UVHelper.CreateInstance(slotData.Slot.Orientation);

            // Measure all 'auto' and 'absolute' children; add up the * values as we go.
            double totalU = 0, maxV = 0;
            double totalStars = 0;
            bool addPad = false;
            foreach (var child in slotData.Slot.Children)
            {
                var length = child.Length;
                var childSlot = slotTable[InternalGetSlotName(child)];

                if (addPad)
                {
                    totalU += SlotSpacing;
                }

                if (length.IsAuto)
                {
                    var childU = Math.Max(Math.Min(uv.U(availableSize), childSlot.Slot.MaxLength), childSlot.Slot.MinLength);
                    var childAvailableSize = uv.Size(childU, uv.V(availableSize));
                    MeasureSlot(childSlot, childAvailableSize);
                    childSlot.FinalSize = childSlot.DesiredSize;
                    totalU += uv.U(childSlot.FinalSize);
                    maxV = Math.Max(maxV, uv.V(childSlot.FinalSize));
                }
                else if (length.IsAbsolute)
                {
                    var childU = Math.Max(Math.Min(length.Value, childSlot.Slot.MaxLength), childSlot.Slot.MinLength);
                    MeasureSlot(childSlot, uv.Size(childU, uv.V(availableSize)));
                    childSlot.FinalSize = uv.Size(childU, uv.V(childSlot.DesiredSize));
                    totalU += childU;
                    maxV = Math.Max(maxV, uv.V(childSlot.FinalSize));
                }
                else
                {
                    totalStars += length.Value;
                }

                addPad = (SlotSpacing > 0);
            }

            // Now measure the * values.
            double spaceLeft = Math.Max(uv.U(availableSize) - totalU, 0);

            List<Slot> slots = (from child in slotData.Slot.Children where child.Length.IsStar select child).ToList();
            List<Slot> proportional = new List<Slot>();

            // Determined the size for stars that are over the min or max length
            int constrained;
            do
            {
                constrained = 0;

                for (int idx = 0; idx < slots.Count; idx++)
                {
                    Slot child = slots[idx];
                    var childSlot = slotTable[InternalGetSlotName(child)];
                    var childU = (spaceLeft / totalStars) * child.Length.Value;

                    if (childU < childSlot.Slot.MinLength)
                    {
                        spaceLeft -= childSlot.Slot.MinLength - childU;
                        childU = childSlot.Slot.MinLength;
                    }
                    else if (childU > childSlot.Slot.MaxLength)
                    {
                        spaceLeft += childSlot.Slot.MaxLength - childU;
                        childU = childSlot.Slot.MaxLength;
                    }
                    else
                    {
                        proportional.Add(child);
                        continue;
                    }

                    constrained++;
                    var childAvailableSize = uv.Size(Math.Max(0, childU), uv.V(availableSize));
                    MeasureSlot(childSlot, childAvailableSize);
                    childSlot.FinalSize = uv.Size(MaxNonInfinite(childU, uv.U(childSlot.DesiredSize)), uv.V(childSlot.DesiredSize));
                    totalU += uv.U(childSlot.FinalSize);
                    maxV = Math.Max(maxV, uv.V(childSlot.FinalSize));
                }

                slots = proportional;
                proportional = new List<Slot>();
            } while (slots.Count > 0 && constrained > 0);

            // Determine the size of remaining star children
            foreach (var child in slots)
            {
                var childSlot = slotTable[InternalGetSlotName(child)];
                var childU = (spaceLeft / totalStars) * child.Length.Value;
                var childAvailableSize = uv.Size(Math.Max(0, childU), uv.V(availableSize));
                MeasureSlot(childSlot, childAvailableSize);
                childSlot.FinalSize = uv.Size(MaxNonInfinite(childU, uv.U(childSlot.DesiredSize)), uv.V(childSlot.DesiredSize));
                totalU += uv.U(childSlot.FinalSize);
                maxV = Math.Max(maxV, uv.V(childSlot.FinalSize));
            }

            // Finally, measure any child elements we have
            if (slotData.Elements != null)
            {
                foreach (var e in slotData.Elements)
                {
                    e.Measure(availableSize);
                    totalU = Math.Max(totalU, uv.U(e.DesiredSize));
                    maxV = Math.Max(maxV, uv.V(e.DesiredSize));
                }
            }

            slotData.DesiredSize = uv.Size(totalU, maxV);
        }

        void OnSlotChanged(object sender, SlotChangedEventArgs e)
        {
            if (e.AffectsStructure)
            {
                slotStructureValid = false;
            }
            InvalidateMeasure();
        }

        static void OnSlotDefinitionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var panel = obj as SlotPanel;

            if (panel != null)
            {
                panel.slotStructureValid = false;
                panel.InvalidateMeasure();

                Slot oldSlot = e.OldValue as Slot;

                if (oldSlot != null)
                {
                    oldSlot.Changed -= panel.OnSlotChanged;
                }

                Slot newSlot = e.NewValue as Slot;

                if (newSlot != null)
                {
                    newSlot.Changed += panel.OnSlotChanged;
                }
            }
        }

        static void OnSlotNameChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var element = obj as UIElement;

            if (element != null)
            {
                var parent = VisualTreeHelper.GetParent(element) as SlotPanel;
                if (parent != null)
                {
                    parent.slotStructureValid = false;
                    parent.InvalidateMeasure();
                }
            }
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            slotStructureValid = false;
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters",
                         Justification = "API is only intended for UI elements, not DependencyObject")]
        public static void SetSlotName(UIElement element, string value)
        {
            if (element == null) { throw new ArgumentNullException("element"); }

            element.SetValue(SlotNameProperty, value);
        }

        void UpdateSlotTable()
        {
            if (slotTable == null || !slotStructureValid)
            {
                slotTable = new Dictionary<string, SlotData>();
                topSlotData.Slot.Children.Clear();
                if (SlotDefinition != null)
                {
                    topSlotData.Slot.Children.Add(SlotDefinition);
                    AddSlotToTable(SlotDefinition);
                }

                foreach (UIElement element in InternalChildren)
                {
                    if (element != null)
                    {
                        string name = SlotPanel.GetSlotName(element) ?? "";
                        SlotData data;

                        if (!slotTable.TryGetValue(name, out data))
                        {
                            data = topSlotData;
                        }

                        if (data.Elements == null)
                        {
                            data.Elements = new List<UIElement>();
                        }

                        data.Elements.Add(element);
                    }
                }

                topSlotData.Slot.ElementCount = (topSlotData.Elements != null) ? topSlotData.Elements.Count : 0;

                foreach (var slotData in slotTable.Values)
                {
                    slotData.Slot.ElementCount = (slotData.Elements != null) ? slotData.Elements.Count : 0;
                }

                slotStructureValid = true;
            }
        }

        class SlotData
        {
            public Slot Slot { get; set; }
            public List<UIElement> Elements { get; set; }
            public Size DesiredSize { get; set; }
            public Size FinalSize { get; set; }
        }

        internal abstract class UVHelper
        {
            public static UVHelper CreateInstance(Orientation o)
            {
                return (o == Orientation.Horizontal) ? (UVHelper)new Horz() : (UVHelper)new Vert();
            }

            public abstract Size Size(double u, double v);
            public abstract Point Point(double u, double v);
            public abstract double U(Size s);
            public abstract double V(Size s);
            public abstract double U(Point p);
            public abstract double V(Point p);

            class Horz : UVHelper
            {
                public override Size Size(double u, double v) { return new Size(u, v); }
                public override Point Point(double u, double v) { return new Point(u, v); }
                public override double U(Size s) { return s.Width; }
                public override double U(Point s) { return s.X; }
                public override double V(Size s) { return s.Height; }
                public override double V(Point s) { return s.Y; }
            }

            class Vert : UVHelper
            {
                public override Size Size(double u, double v) { return new Size(v, u); }
                public override Point Point(double u, double v) { return new Point(v, u); }
                public override double U(Size s) { return s.Height; }
                public override double U(Point s) { return s.Y; }
                public override double V(Size s) { return s.Width; }
                public override double V(Point s) { return s.X; }
            }
        }
    }
}

