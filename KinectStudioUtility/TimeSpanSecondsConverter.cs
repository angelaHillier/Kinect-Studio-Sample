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

    public class TimeSpanSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan)
            {
                value = ((TimeSpan)value).TotalSeconds;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;

            if (str != null)
            {
                double num;
                if (double.TryParse(str, out num))
                {
                    value = num;
                }
            }

            if (value is double)
            {
                // don't use TimeSpan.FromSeconds because it rounds to the nearest millisecond
                value = TimeSpan.FromTicks((long)(TimeSpan.TicksPerSecond * ((double)value)));
            }

            if (!(value is TimeSpan))
            {
                value = TimeSpan.Zero;
            }

            return value;
        }
    }
}
