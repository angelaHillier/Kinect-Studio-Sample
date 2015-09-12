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

    public class SizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DebugHelper.AssertUIThread();

            if (value is ulong)
            {
                ulong size = (ulong)value;

                // format matches the File:Open dialog
                size = (size + 1023) / 1024;
                value = String.Format(CultureInfo.CurrentCulture, this.format, size);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }

        public string Format
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.format;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.format = value;
            }
        }

        private string format = "{0} KB";
    }
}
