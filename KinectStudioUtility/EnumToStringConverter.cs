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
    using System.Resources;
    using System.Windows;
    using System.Windows.Data;

    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DebugHelper.AssertUIThread();

            if (value != null)
            {
                string prefix = parameter as string;
                value = value.ToString();

                if (!String.IsNullOrWhiteSpace(prefix))
                {
                    if (this.resourceManager != null)
                    {
                        string temp = this.resourceManager.GetString(prefix + value, culture);

                        if (temp == null)
                        {
                            temp = this.resourceManager.GetString(prefix + "Default", culture);
                        }

                        if (temp != null)
                        {
                            value = temp;
                        }
                    }
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }

        public ResourceManager ResourceManager 
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.resourceManager;
            }

            set
            {
                DebugHelper.AssertUIThread();

                this.resourceManager = value;
            }
        }

        private ResourceManager resourceManager = null;
    }
}
