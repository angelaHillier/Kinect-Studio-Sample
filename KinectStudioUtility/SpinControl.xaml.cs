//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    public partial class SpinControl : UserControl
    {
        public SpinControl()
        {
            this.InitializeComponent();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public uint Value
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (uint)GetValue(ValueProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                SetValue(ValueProperty, value);
            }
        }

        public uint Minimum
        {
            get
            {
                return (uint)this.GetValue(MinimumProperty);
            }
            set
            {
                this.SetValue(MinimumProperty, value);
            }
        }

        public uint Maximum
        {
            get
            {
                return (uint)this.GetValue(MaximumProperty);
            }
            set
            {
                this.SetValue(MaximumProperty, value);
            }
        }

        public string UpButtonToolTip
        {
            get
            {
                return this.GetValue(UpButtonToolTipProperty) as string;
            }
            set
            {
                this.SetValue(UpButtonToolTipProperty, value);
            }
        }

        public string DownButtonToolTip
        {
            get
            {
                return this.GetValue(DownButtonToolTipProperty) as string;
            }
            set
            {
                this.SetValue(DownButtonToolTipProperty, value);
            }
        }

        public event RoutedPropertyChangedEventHandler<uint> ValueChanged
        {
            add
            {
                this.AddHandler(ValueChangedEvent, value);
            }
            remove
            {
                this.RemoveHandler(ValueChangedEvent, value);
            }
        }

        private void ValueUp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            if (this.Value < this.Maximum)
            {
                this.Value++;
            }
        }

        private void ValueDown_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            if (this.Value > this.Minimum)
            {
                this.Value--;
            }
        }

        private void OnValueChanged(RoutedPropertyChangedEventArgs<uint> e)
        {
            this.RaiseEvent(e);
        }

        private void OnMinimumChanged(uint newMinimum)
        {
            if (this.Value < newMinimum)
            {
                this.Value = newMinimum;
            }
        }

        private void OnMaximumChanged(uint newMaximum)
        {
            if (this.Value > newMaximum)
            {
                this.Value = newMaximum;
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e != null)
            {
                foreach (char ch in e.Text)
                {
                    if (!Char.IsDigit(ch))
                    {
                        e.Handled = true;
                        break;
                    }
                }
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpinControl spinControl = d as SpinControl;

            if (spinControl != null)
            {
                uint value = (uint)e.NewValue;

                if (value < spinControl.Minimum)
                {
                    spinControl.Value = spinControl.Minimum;
                }
                else if (value > spinControl.Maximum)
                {
                    spinControl.Value = spinControl.Maximum;
                }
                else
                {
                    RoutedPropertyChangedEventArgs<uint> e2 = new RoutedPropertyChangedEventArgs<uint>((uint)e.OldValue, value, ValueChangedEvent);
                    spinControl.OnValueChanged(e2);
                }
            }
        }

        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpinControl spinControl = d as SpinControl;

            if (spinControl != null)
            {
                spinControl.OnMinimumChanged((uint)e.NewValue);
            }
        }

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpinControl spinControl = d as SpinControl;

            if (spinControl != null)
            {
                spinControl.OnMaximumChanged((uint)e.NewValue);
            }
        }

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<uint>), typeof(SpinControl));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(uint), typeof(SpinControl), new FrameworkPropertyMetadata(uint.MinValue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnValueChanged)));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(uint), typeof(SpinControl), new PropertyMetadata(uint.MinValue, new PropertyChangedCallback(OnMinimumChanged)));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(uint), typeof(SpinControl), new PropertyMetadata(uint.MaxValue, new PropertyChangedCallback(OnMaximumChanged)));
        public static readonly DependencyProperty UpButtonToolTipProperty = DependencyProperty.Register("UpButtonToolTip", typeof(string), typeof(SpinControl));
        public static readonly DependencyProperty DownButtonToolTipProperty = DependencyProperty.Register("DownButtonToolTip", typeof(string), typeof(SpinControl));
    }
}
