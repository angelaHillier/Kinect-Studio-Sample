//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((targetType == typeof(bool?) || (targetType == typeof(bool))) && (value != null) && (parameter != null))
            {
                value = String.CompareOrdinal(value.ToString(), parameter.ToString()) == 0;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value is bool) && (parameter != null))
            {
                string targetValue = parameter.ToString();
                if ((bool)value)
                {
                    value = Enum.Parse(targetType, targetValue);
                }
                else
                {
                    value = null;
                }
            }

            return value;
        }
    }
}
