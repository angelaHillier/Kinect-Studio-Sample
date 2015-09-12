//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Xbox.Tools.Shared
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    public class SlotSizer : Control
    {
        public static readonly DependencyProperty Slot1Property = DependencyProperty.Register(
            "Slot1", typeof(Slot), typeof(SlotSizer));

        public static readonly DependencyProperty Slot2Property = DependencyProperty.Register(
            "Slot2", typeof(Slot), typeof(SlotSizer));

        public static readonly DependencyProperty SizeDirectionProperty = DependencyProperty.Register(
            "SizeDirection", typeof(Orientation), typeof(SlotSizer));

        SlotPanel parent;
        Point startPoint, minPoint, maxPoint;
        Size slot1ActualSizeStart, slot2ActualSizeStart;
        GridLength slot1LengthStart, slot2LengthStart;
        SlotPanel.UVHelper uv;


        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", 
                         Justification = "This initialization cannot be expressed inline")]
        static SlotSizer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SlotSizer), new FrameworkPropertyMetadata(typeof(SlotSizer)));
        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", 
                         Justification = "Reviewed for unintended consequences")]
        public SlotSizer()
        {
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
        }

        public Orientation SizeDirection
        {
            get { return (Orientation)GetValue(SizeDirectionProperty); }
            set { SetValue(SizeDirectionProperty, value); }
        }

        public Slot Slot1
        {
            get { return (Slot)GetValue(Slot1Property); }
            set { SetValue(Slot1Property, value); }
        }

        public Slot Slot2
        {
            get { return (Slot)GetValue(Slot2Property); }
            set { SetValue(Slot2Property, value); }
        }

        void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            this.MouseMove -= OnMouseMove;
            this.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            this.LostMouseCapture -= OnLostMouseCapture;
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            parent = this.FindParent<SlotPanel>();

            if (parent != null)
            {
                e.MouseDevice.Capture(this);
                startPoint = e.GetPosition(parent);
                slot1ActualSizeStart = Slot1.ActualSize;
                slot2ActualSizeStart = Slot2.ActualSize;
                slot1LengthStart = Slot1.Length;
                slot2LengthStart = Slot2.Length;

                uv = SlotPanel.UVHelper.CreateInstance(this.SizeDirection);

                double minU1 = Math.Max(Slot1.MinLength, parent.SlotSpacing);
                double minU2 = Math.Max(Slot2.MinLength, parent.SlotSpacing);
                double maxU1 = Slot1.MaxLength;
                double maxU2 = Slot2.MaxLength;
                double minU = Math.Max(uv.U(startPoint) - uv.U(slot1ActualSizeStart) + minU1, uv.U(startPoint) + uv.U(slot2ActualSizeStart) - maxU2);
                double maxU = Math.Min(uv.U(startPoint) + uv.U(slot2ActualSizeStart) - minU2, uv.U(startPoint) - uv.U(slot1ActualSizeStart) + maxU1);
                minPoint = uv.Point(minU, 0);
                maxPoint = uv.Point(maxU, 0);

                this.MouseMove += OnMouseMove;
                this.MouseLeftButtonUp += OnMouseLeftButtonUp;
                this.LostMouseCapture += OnLostMouseCapture;
            }
            e.Handled = true;
        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(null);
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point pt = e.GetPosition(parent);
            pt.X = Math.Max(minPoint.X, Math.Min(maxPoint.X, pt.X)) - startPoint.X;
            pt.Y = Math.Max(minPoint.Y, Math.Min(maxPoint.Y, pt.Y)) - startPoint.Y;

            Slot1.Length = RecomputeLength(slot1LengthStart, Math.Max(1, uv.U(slot1ActualSizeStart)), Math.Max(1, uv.U(slot1ActualSizeStart) + uv.U(pt)));
            Slot2.Length = RecomputeLength(slot2LengthStart, Math.Max(1, uv.U(slot2ActualSizeStart)), Math.Max(1, uv.U(slot2ActualSizeStart) - uv.U(pt)));
            e.Handled = true;
        }

        static GridLength RecomputeLength(GridLength current, double oldPixel, double newPixel)
        {
            if (current.IsStar)
            {
                double oldStar = current.Value;
                double newStar = (oldStar * newPixel) / oldPixel;
                return new GridLength(newStar, GridUnitType.Star);
            }
            else
            {
                return new GridLength(newPixel, GridUnitType.Pixel);
            }
        }

    }
}
