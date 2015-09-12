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
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using KinectStudioUtility;

    public class Thumb2 : Thumb
    {
        public void ForceDrag(MouseButtonEventArgs e)
        {
            DebugHelper.AssertUIThread();

            OnMouseLeftButtonDown(e);
        }
    }

    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_DatePicker", Type = typeof(DatePicker))]
    public class DateTimePicker : ContentControl
    {
        public DateTimePicker()
        {
            this.Loaded += DateTimePicker_Loaded;
        }

        private void DateTimePicker_Loaded(object sender, RoutedEventArgs e)
        {
            DatePicker datePicker = this.GetTemplateChild("PART_DatePicker") as DatePicker;
            if (datePicker != null)
            {
                TextBox textBox = datePicker.GetVisualChild<TextBox>();
                if (textBox != null)
                {
                    textBox.Visibility = Visibility.Collapsed;
                }
            }
        }

        public DateTime SelectedDate
        {
            get
            {
                return (DateTime)this.GetValue(DateTimePicker.SelectedDateProperty);
            }
            set
            {
                this.SetValue(DateTimePicker.SelectedDateProperty, value);
            }
        }

        public DateTime SelectedDateTime
        {
            get
            {
                return (DateTime)this.GetValue(DateTimePicker.SelectedDateTimeProperty);
            }
            set
            {
                this.SetValue(DateTimePicker.SelectedDateTimeProperty, value);
            }
        }

        private void OnDateChanged(DateTime newDate)
        {
            DateTime time = this.SelectedDateTime;

            this.SelectedDateTime = new DateTime(newDate.Year, newDate.Month, newDate.Day, time.Hour, time.Minute, time.Second, time.Millisecond, DateTimeKind.Utc);
        }

        private void OnDateTimeChanged(DateTime newDate)
        {
            this.SelectedDate = newDate;
        }

        public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register("SelectedDate", typeof(DateTime), typeof(DateTimePicker), new PropertyMetadata(DateTime.UtcNow.Date, OnDateChanged));
        public static readonly DependencyProperty SelectedDateTimeProperty = DependencyProperty.Register("SelectedDateTime", typeof(DateTime), typeof(DateTimePicker), new PropertyMetadata(DateTime.UtcNow, OnDateTimeChanged));

        private static void OnDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            DateTimePicker picker = d as DateTimePicker;
            if (picker != null)
            {
                picker.OnDateChanged((DateTime)e.NewValue);
            }
        }

        private static void OnDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            DateTimePicker picker = d as DateTimePicker;
            if (picker != null)
            {
                picker.OnDateTimeChanged((DateTime)e.NewValue);
            }
        }
    }

    public class BooleanButton : Button
    {
        public BooleanButton()
        {
            this.Loaded += BooleanButton_Loaded;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public bool Value
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (bool)this.GetValue(BooleanButton.ValueProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(BooleanButton.ValueProperty, value);
            }
        }

        public DataTemplate TrueContentTemplate
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(BooleanButton.TrueContentTemplateProperty) as DataTemplate;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(BooleanButton.TrueContentTemplateProperty, value);
            }
        }

        public DataTemplate FalseContentTemplate
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(BooleanButton.FalseContentTemplateProperty) as DataTemplate;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(BooleanButton.FalseContentTemplateProperty, value);
            }
        }

        public object TrueToolTip
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(BooleanButton.TrueToolTipProperty) as string;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(BooleanButton.TrueToolTipProperty, value);
            }
        }

        public object FalseToolTip
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(BooleanButton.FalseToolTipProperty) as string;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(BooleanButton.FalseToolTipProperty, value);
            }
        }

        protected override void OnClick()
        {
            DebugHelper.AssertUIThread();

            base.OnClick();

            this.Value = !this.Value;
        }

        private void BooleanButton_Loaded(object sender, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.OnValueChanged();
        }

        private void OnValueChanged()
        {
            DebugHelper.AssertUIThread();

            if (this.Value)
            {
                this.ToolTip = this.TrueToolTip;
                this.ContentTemplate = this.TrueContentTemplate;
            }
            else
            {
                this.ToolTip = this.FalseToolTip;
                this.ContentTemplate = this.FalseContentTemplate;
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            BooleanButton button = d as BooleanButton;
            if (button != null)
            {
                button.OnValueChanged();
            }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(bool), typeof(BooleanButton), new PropertyMetadata(OnValueChanged));
        public static readonly DependencyProperty TrueContentTemplateProperty = DependencyProperty.Register("TrueContentTemplate", typeof(DataTemplate), typeof(BooleanButton));
        public static readonly DependencyProperty FalseContentTemplateProperty = DependencyProperty.Register("FalseContentTemplate", typeof(DataTemplate), typeof(BooleanButton));
        public static readonly DependencyProperty TrueToolTipProperty = DependencyProperty.Register("TrueToolTip", typeof(string), typeof(BooleanButton));
        public static readonly DependencyProperty FalseToolTipProperty = DependencyProperty.Register("FalseToolTip", typeof(string), typeof(BooleanButton));
    }
}
