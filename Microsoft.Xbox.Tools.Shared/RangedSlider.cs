//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.Xbox.Tools.Shared
{
    public class RangedSlider : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(int), typeof(RangedSlider), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum", typeof(int), typeof(RangedSlider), new FrameworkPropertyMetadata(0, OnMinOrMaxChanged));

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(int), typeof(RangedSlider), new FrameworkPropertyMetadata(100, OnMinOrMaxChanged));

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            "Label", typeof(string), typeof(RangedSlider));

        TextBox textBox;

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.textBox = GetTemplateChild("PART_TextBox") as TextBox;
            SetTextBoxBinding();
        }

        void SetTextBoxBinding()
        {
            if (this.textBox != null)
            {
                var binding = new Binding { Source = this, Path = new PropertyPath(ValueProperty), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, Mode = BindingMode.TwoWay };
                binding.ValidatesOnExceptions = true;
                binding.ValidationRules.Add(new RangeValidationRule(this));
                this.textBox.SetBinding(TextBox.TextProperty, binding);
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RangedSliderAutomationPeer(this);
        }

        static void OnMinOrMaxChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            RangedSlider ctrl = obj as RangedSlider;

            if (ctrl != null)
            {
                ctrl.SetTextBoxBinding();
            }
        }

        class RangeValidationRule : ValidationRule
        {
            RangedSlider slider;

            public RangeValidationRule(RangedSlider slider)
            {
                this.slider = slider;
            }

            public override ValidationResult Validate(object value, CultureInfo cultureInfo)
            {
                if (value == null)
                {
                    return new ValidationResult(false, "Value cannot be null.");
                }

                int intValue;

                if (!int.TryParse((string)value, out intValue))
                {
                    return new ValidationResult(false, string.Format(CultureInfo.CurrentUICulture, "Unable to convert '{0}' to an integer value.", value));
                }

                if (intValue >= slider.Minimum && intValue <= slider.Maximum)
                {
                    return ValidationResult.ValidResult;
                }

                return new ValidationResult(false, string.Format(CultureInfo.CurrentUICulture, "Must be between {0} and {1} inclusively", slider.Minimum, slider.Maximum));
            }
        }

        class RangedSliderAutomationPeer : FrameworkElementAutomationPeer
        {
            RangedSlider slider;
            static string[] automationChildrenNames = { "PART_TextBox", "PART_Slider" };

            public RangedSliderAutomationPeer(RangedSlider slider) : base(slider)
            {
                this.slider = slider;
            }

            protected override List<AutomationPeer> GetChildrenCore()
            {
                var list = base.GetChildrenCore();

                foreach (var name in automationChildrenNames)
                {
                    var element = this.slider.Template.FindName(name, this.slider) as UIElement;

                    if (element != null)
                    {
                        list.Add(UIElementAutomationPeer.CreatePeerForElement(element));
                    }
                }

                return list;
            }
        }
    }
}
