//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Microsoft.Xbox.Tools.Shared
{
    [ContentProperty("Children")]
    public class Slot : DependencyObject
    {
        private static readonly DependencyPropertyKey actualSizePropertyKey = DependencyProperty.RegisterReadOnly(
            "ActualSize", typeof(Size), typeof(Slot), new FrameworkPropertyMetadata(new Size()));
        public static readonly DependencyProperty ActualSizeProperty = actualSizePropertyKey.DependencyProperty;

        public static readonly DependencyProperty UpperLeftProperty = DependencyProperty.Register(
            "UpperLeft", typeof(Point), typeof(Slot));

        private static readonly DependencyPropertyKey elementCountPropertyKey = DependencyProperty.RegisterReadOnly(
            "ElementCount", typeof(int), typeof(Slot), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty ElementCountProperty = elementCountPropertyKey.DependencyProperty;

        public static readonly DependencyProperty NameProperty = DependencyProperty.Register(
            "Name", typeof(string), typeof(Slot));

        public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(
            "Length", typeof(GridLength), typeof(Slot), new FrameworkPropertyMetadata(new GridLength(1, GridUnitType.Star)));

        public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(
            "MaxLength", typeof(double), typeof(Slot), new FrameworkPropertyMetadata(double.PositiveInfinity));

        public static readonly DependencyProperty MinLengthProperty = DependencyProperty.Register(
            "MinLength", typeof(double), typeof(Slot), new FrameworkPropertyMetadata((double)0));

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(Slot));

        public static readonly DependencyProperty ParentProperty = DependencyProperty.Register(
            "Parent", typeof(Slot), typeof(Slot));

        static int instance;
        static string instancePrefix = "";
        ObservableCollection<Slot> children = new ObservableCollection<Slot>();

        public Slot()
        {
            this.Name = string.Format(CultureInfo.InvariantCulture, "{0}{1}", instancePrefix, ++instance);
            if (instance == 0)
            {
                instancePrefix = instancePrefix + "_";
            }
            children.CollectionChanged += OnChildrenCollectionChanged;
        }

        public Size ActualSize
        {
            get { return (Size)GetValue(ActualSizeProperty); }
            internal set { SetValue(actualSizePropertyKey, value); }
        }

        public Point UpperLeft
        {
            get { return (Point)GetValue(UpperLeftProperty); }
            set { SetValue(UpperLeftProperty, value); }
        }

        public ObservableCollection<Slot> Children { get { return children; } }

        public int ElementCount
        {
            get { return (int)GetValue(ElementCountProperty); }
            internal set { SetValue(elementCountPropertyKey, value); }
        }

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        public GridLength Length
        {
            get { return (GridLength)GetValue(LengthProperty); }
            set { SetValue(LengthProperty, value); }
        }

        public double MaxLength
        {
            get { return (double)GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        public double MinLength
        {
            get { return (double)GetValue(MinLengthProperty); }
            set { SetValue(MinLengthProperty, value); }
        }

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public Slot Parent
        {
            get { return (Slot)GetValue(ParentProperty); }
            set { SetValue(ParentProperty, value); }
        }

        public Slot FindSlot(string name)
        {
            if (StringComparer.Ordinal.Equals(this.Name, name))
            {
                return this;
            }

            foreach (var child in this.children)
            {
                var found = child.FindSlot(name);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public Slot Clone()
        {
            var clone = new Slot
            {
                Name = this.Name,
                Length = this.Length,
                MinLength = this.MinLength,
                MaxLength = this.MaxLength,
                Orientation = this.Orientation,
            };

            foreach (var child in this.children)
            {
                clone.children.Add(child.Clone());
            }

            return clone;
        }

        void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (Slot slot in e.OldItems)
                {
                    slot.Changed -= OnChildSlotChanged;
                    if (slot.Parent == this)
                    {
                        slot.Parent = null;
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (Slot slot in e.NewItems)
                {
                    slot.Changed += OnChildSlotChanged;
                    slot.Parent = this;
                }
            }

            RaiseChangedEvent(new SlotChangedEventArgs(true));
        }

        void OnChildSlotChanged(object sender, SlotChangedEventArgs e)
        {
            RaiseChangedEvent(e);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == NameProperty || e.Property == LengthProperty || e.Property == OrientationProperty)
            {
                RaiseChangedEvent(new SlotChangedEventArgs(e.Property == NameProperty));
            }

            base.OnPropertyChanged(e);
        }

        void RaiseChangedEvent(SlotChangedEventArgs e)
        {
            if (Changed != null)
            {
                Changed(this, e);
            }
        }

        public event EventHandler<SlotChangedEventArgs> Changed;
    }
}
