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

    public class PercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            DebugHelper.AssertUIThread();

            object value = 0;

            if ((values != null) && (values.Length > 1))
            {
                if ((values[0] is uint) && (values[1] is uint))
                {
                    uint total = (uint)values[0];
                    uint current = (uint)values[1];

                    uint percentage = 0;

                    if (total > 0)
                    {
                        percentage = (uint)(Math.Floor(((double)current / total) * 100));
                    }

                    string format = parameter as string;
                    if (format == null)
                    {
                        value = percentage;
                    }
                    else
                    {
                        value = String.Format(CultureInfo.CurrentCulture, format, percentage);
                    }
                }
            }

            return value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }
}
