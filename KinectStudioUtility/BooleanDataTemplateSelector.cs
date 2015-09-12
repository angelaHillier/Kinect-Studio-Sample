//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;

    public class BooleanDataTemplateSelector : DataTemplateSelector
    {
        public string PropertyName
        {
            get
            {
                return this.propertyName;
            }

            set
            {
                this.propertyName = value;
            }
        }

        public DataTemplate TrueTemplate
        {
            get
            {
                return this.trueTemplate;
            }

            set
            {
                this.trueTemplate = value;
            }
        }

        public DataTemplate FalseTemplate
        {
            get
            {
                return this.falseTemplate;
            }

            set
            {
                this.falseTemplate = value;
            }
        }

        public DataTemplate NullTemplate
        {
            get
            {
                return this.nullTemplate;
            }

            set
            {
                this.nullTemplate = value;
            }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate value = null;

            if (item == null)
            {
                value = this.nullTemplate;
            }
            else if (item is bool)
            {
                value = ((bool)item) ? this.trueTemplate : this.falseTemplate;
            }
            else if (item is bool?)
            {
                bool? boolValue = (bool?)item;
                Debug.Assert(boolValue.HasValue);

                value = boolValue.Value ? this.trueTemplate : this.falseTemplate;
            }
            else
            {
                if (String.IsNullOrWhiteSpace(this.propertyName))
                {
                    value = this.falseTemplate;
                }
                else
                {
                    value = this.trueTemplate;
                }
            }

            if (value == null)
            {
                value = base.SelectTemplate(item, container);
            }

            return value;
        }

        private string propertyName = null;
        private DataTemplate trueTemplate = null;
        private DataTemplate falseTemplate = null;
        private DataTemplate nullTemplate = null;
    }
}

