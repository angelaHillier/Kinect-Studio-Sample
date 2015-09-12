//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ThemeBindingExtension : MarkupExtension, IValueConverter
    {
        TypeConverter typeConverter;

        public ThemeBindingExtension()
        {
        }

        public ThemeBindingExtension(string path)
            : this()
        {
            Path = path;
        }

        public IValueConverter Converter { get; set; }
        public string Path { get; set; }
        public Type TargetType { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // If we have a converter, use it
                if (this.Converter != null)
                {
                    return this.Converter.Convert(value, targetType, parameter, culture);
                }

                // If the type is compatible, use it
                if (value != null && targetType.IsAssignableFrom(value.GetType()))
                {
                    return value;
                }

                // We only know how to convert from text...
                var text = value as string;

                if (text != null)
                {
                    if (this.typeConverter == null)
                    {
                        if (this.TargetType == typeof(string))
                            return text;

                        this.typeConverter = TypeDescriptor.GetConverter(this.TargetType);
                    }

                    return typeConverter.ConvertFromString(null, CultureInfo.InvariantCulture, text);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ThemeBinding exception:  {0}", ex.Message);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (this.Converter == null && this.TargetType == null)
            {
                var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
                if (target == null)
                {
                    return this;
                }

                var prop = target.TargetProperty as DependencyProperty;
                if (prop == null)
                {
                    return this;
                }

                this.TargetType = prop.PropertyType;
            }

            var binding = new Binding
            {
                Source = Theme.Instance,
                Path = new PropertyPath("Theme." + this.Path),
                Converter = this,
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
