//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ColorOrBrushToContrastingBlackWhiteConverter : IValueConverter
    {
        float Luminance(Color c)
        {
            return (c.ScR * 0.3f) + (c.ScG * 0.59f) + (c.ScB * 0.11f);
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color resultColor;
            float luminance = 0f;

            if (value is Color)
            {
                luminance = Luminance((Color)value);
            }
            else if (value is SolidColorBrush)
            {
                luminance = Luminance(((SolidColorBrush)value).Color);
            }

            resultColor = (luminance >= 0.5f) ? Colors.Black : Colors.White;

            if (targetType == typeof(Color))
            {
                return resultColor;
            }

            return new SolidColorBrush(resultColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToObjectConverter : IValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((value is bool) && (bool)value) ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var s = value as string;

            if (!string.IsNullOrEmpty(s))
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ErrorSeverityToImageSourceConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty InfoSourceProperty = DependencyProperty.Register(
            "InfoSource", typeof(ImageSource), typeof(ErrorSeverityToImageSourceConverter));

        public ImageSource ErrorSource { get; set; }
        public ImageSource WarningSource { get; set; }
        public ImageSource InfoSource
        {
            get { return (ImageSource)GetValue(InfoSourceProperty); }
            set { SetValue(InfoSourceProperty, value); }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ErrorSeverity)
            {
                switch ((ErrorSeverity)value)
                {
                    case ErrorSeverity.Error: return ErrorSource;
                    case ErrorSeverity.Warning: return WarningSource;
                    case ErrorSeverity.Info: return InfoSource;
                    default:
                        break;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LuminanceConverter : IValueConverter
    {
        public float TargetLuminance { get; set; }

        public LuminanceConverter()
        {
            this.TargetLuminance = 0.8f;
        }

        static float Lerp(float start, float end, float percent)
        {
            return start + ((end - start) * percent);
        }

        static Color LightenColor(Color c, float percent)
        {
            // Positive percent is lighten (toward white), Negative percent is darken (toward black)
            float to = 1f;

            if (percent < 0)
            {
                to = 0f;
                percent = -percent;
            }

            return Color.FromScRgb(1.0f, Lerp(c.ScR, to, percent), Lerp(c.ScG, to, percent), Lerp(c.ScB, to, percent));
        }

        static float Luminance(Color c)
        {
            return (c.ScR * 0.3f) + (c.ScG * 0.59f) + (c.ScB * 0.11f);
        }

        static float Intensity(Color c)
        {
            return (c.ScR + c.ScG + c.ScB) / 3;
        }

        public static Color WashColor(Color color, float percent)
        {
            return Color.FromScRgb(
                1.0f,
                color.ScR + ((1.0f - color.ScR) * percent),
                color.ScG + ((1.0f - color.ScG) * percent),
                color.ScB + ((1.0f - color.ScB) * percent));
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color inputColor;

            if (value is Color)
            {
                inputColor = (Color)value;
            }
            else if (value is SolidColorBrush)
            {
                inputColor = ((SolidColorBrush)value).Color;
            }
            else
            {
                return Colors.White;
            }

            float currentLuminance = Intensity(inputColor);
            //float currentLuminance = Luminance(inputColor);
            float delta = TargetLuminance - currentLuminance;
            //Color resultColor = WashColor(inputColor, delta);
            if (delta > 0)
            {
                delta = delta / (1 - currentLuminance);
            }
            else
            {
                delta = delta / currentLuminance;
            }

            Color resultColor = LightenColor(inputColor, delta);

            if (targetType == typeof(Color))
            {
                return resultColor;
            }
            else if (targetType == typeof(Brush))
            {
                return new SolidColorBrush(resultColor);
            }

            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PercentConverter : IMultiValueConverter
    {
        [SuppressMessage("Microsoft.Usage", "#pw26505")]
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double percent = (double)values[0];
            double width = (double)values[1];

            return width * percent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HeightToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new Thickness(-(double)value, 0, -(double)value, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TreeIndentLevelToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int)
            {
                return new Thickness((int)value * 12, 0, 4, 0);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimelineStatesToVisibilityConverter : IMultiValueConverter
    {
        [SuppressMessage("Microsoft.Usage", "#pw26505")]
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isLive = (bool)values[0];
            bool isScrolling = (bool)values[1];

            return (isLive && !isScrolling) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FactorConverter : IValueConverter
    {
        public double Factor { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                return (double)value * this.Factor;
            }

            if (value is int)
            {
                return (int)((int)value * this.Factor);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ScrollRangeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double && (double)value > 1)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleToLeftMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                return new Thickness((double)value, 0, 0, 0);
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ItemCountToPluralizedTextConverter : IValueConverter
    {
        public string SingularText { get; set; }
        public string PluralText { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int && ((int)value == 1))
            {
                return this.SingularText;
            }

            return this.PluralText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ChannelMinMaxConverter : IValueConverter
    {
        public byte Value { get; set; }
        public bool R { get; set; }
        public bool G { get; set; }
        public bool B { get; set; }
        public bool A { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is Color))
                return value;

            Color c = (Color)value;

            if (R) return Color.FromArgb(c.A, Value, c.G, c.B);
            if (G) return Color.FromArgb(c.A, c.R, Value, c.B);
            if (B) return Color.FromArgb(c.A, c.R, c.G, Value);
            return Color.FromArgb(Value, c.R, c.G, c.B);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DocumentNameAndWindowCountToTitleConverter : IMultiValueConverter
    {
        public string SingleWindowFormatString { get; set; }
        public string MultipleWindowMainFormatString { get; set; }
        public string MultipleWindowAuxFormatString { get; set; }

        public DocumentNameAndWindowCountToTitleConverter()
        {
            this.SingleWindowFormatString = "{0}";
            this.MultipleWindowMainFormatString = "{0} (Main Window)";
            this.MultipleWindowAuxFormatString = "{0} (Auxiliary Window)";
        }

        [SuppressMessage("Microsoft.Usage", "#pw26505")]
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((values.Length == 3) && (values[0] is string) && (values[1] is int) && (values[2] is bool))
            {
                if (((int)values[1]) == 1)
                {
                    return string.Format(this.SingleWindowFormatString, values[0], ToolsUIApplication.Instance.AppTitle);
                }

                if ((bool)values[2])
                {
                    return string.Format(this.MultipleWindowMainFormatString, values[0], ToolsUIApplication.Instance.AppTitle);
                }

                return string.Format(this.MultipleWindowAuxFormatString, values[0], ToolsUIApplication.Instance.AppTitle);
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // This converter does what Binding.StringFormat would do if the target type were String...
    public class StringFormatConverter : IValueConverter
    {
        public string StringFormat { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (string.IsNullOrEmpty(this.StringFormat))
            {
                return value;
            }

            return string.Format(CultureInfo.InvariantCulture, this.StringFormat, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public enum ByteScaleForm
    {
        Bytes,
        Kilobytes,
        Megabytes,
        Gigabytes
    }

    public class ByteScaleConverter : IValueConverter
    {
        const long Kilobyte = 1024;
        const long Megabyte = Kilobyte * Kilobyte;
        const long Gigabyte = Megabyte * Kilobyte;

        public ByteScaleForm Scale { get; set; }
        public int DecimalPlaces { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!((value is int) || (value is long) || (value is uint) || (value is ulong)))
                return value;

            var v = (ulong)value;
            string stringFormat = string.Format(CultureInfo.InvariantCulture, "N{0}", this.DecimalPlaces);

            switch (this.Scale)
            {
                case ByteScaleForm.Gigabytes:
                    return string.Format("{0} GB", ((double)v / Gigabyte).ToString(stringFormat));

                case ByteScaleForm.Megabytes:
                    return string.Format("{0} MB", ((double)v / Megabyte).ToString(stringFormat));

                case ByteScaleForm.Kilobytes:
                    return string.Format("{0} KB", ((double)v / Kilobyte).ToString(stringFormat));

                default:
                    return string.Format("{0} bytes", v.ToString(stringFormat));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DocumentCategoryCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value is int) && ((int)value == 1))
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
