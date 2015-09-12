//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class ComparisonConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            object value = null;

            if ((values != null) && (values.Length > 1))
            {
                try
                {
                    switch ((string)parameter)
                    {
                        case "notlast":
                            if ((values[1] is int) && (values[0] is int))
                            {
                                if ((int)values[0] == -1)
                                {
                                    value = false;
                                }
                                else
                                {
                                    value = Comparer.Default.Compare(values[0], ((int)values[1]) - 1) < 0;
                                }
                            }
                            else if (values[1] == DependencyProperty.UnsetValue)
                            {
                                value = false;
                            }
                            break;

                        case "<":
                            value = Comparer.Default.Compare(values[0], values[1]) < 0;
                            break;

                        case "==":
                            value = Comparer.Default.Compare(values[0], values[1]) == 0;
                            break;

                        case "str==":
                            value = String.CompareOrdinal(values[0].ToString(), values[1].ToString()) == 0;
                            break;
                    }
                }
                catch (ArgumentException)
                {
                    // ignore
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
