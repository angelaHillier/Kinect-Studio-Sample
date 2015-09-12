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

    public class FormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string value = null;

            if ((values != null) && (values.Length > 0) && (values[0] is String))
            {
                string fmt = (string)values[0];
                string fallback = null;

                int numArgs = values.Length - 1;

                if (values.Length > 2)
                {
                    fallback = values[values.Length - 1] as string;
                }

                if ((fallback != null) && (values.Length < 2) || (values[1] == null))
                {
                    value = fallback;
                }
                else
                {
                    switch (numArgs)
                    {
                        case 0:
                            value = String.Format(CultureInfo.InvariantCulture, fmt);
                            break;

                        case 1:
                            value = String.Format(CultureInfo.InvariantCulture, fmt, values[1]);
                            break;

                        case 2:
                            value = String.Format(CultureInfo.InvariantCulture, fmt, values[1], values[2]);
                            break;

                        default:
                            throw new InvalidOperationException();
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