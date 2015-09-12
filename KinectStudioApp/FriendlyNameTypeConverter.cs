//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows.Data;

    public class FriendlyNameTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type type = value as Type;

            if (type != null)
            {
                string keyName = "MetadataValueType_";
                if (type.IsArray)
                {
                    keyName += type.GetElementType().Name + "Array";
                }
                else
                {
                    keyName += type.Name;
                }

                string temp = Strings.ResourceManager.GetString(keyName);

                if (String.IsNullOrEmpty(temp))
                {
                    value = type.Name;
                }
                else
                {
                    value = temp;
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }

    public class TitleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            object result = null;

            if ((values != null) && (values.Length >= 3))
            {
                string recordingFileName = values[0] as string;
                string playbackFileName = values[1] as string;
                bool isReadOnly = false;

                if (values[2] is bool)
                {
                    isReadOnly = (bool)values[2];
                }

                if (recordingFileName != null)
                {
                    recordingFileName = Path.GetFileName(recordingFileName);

                    if (this.WritableFileFormat != null)
                    {
                        result = String.Format(CultureInfo.CurrentCulture, this.WritableFileFormat, recordingFileName);
                    }
                }
                else if (playbackFileName != null)
                {
                    playbackFileName = Path.GetFileName(playbackFileName);

                    if (isReadOnly)
                    {
                        if (this.ReadOnlyFileFormat != null)
                        {
                            result = String.Format(CultureInfo.CurrentCulture, this.ReadOnlyFileFormat, playbackFileName);
                        }
                    }
                    else
                    {
                        if (this.WritableFileFormat != null)
                        {
                            result = String.Format(CultureInfo.CurrentCulture, this.WritableFileFormat, playbackFileName);
                        }
                    }
                }
            }

            if (result == null)
            {
                result = this.NoFileString;
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }

        public string NoFileString { get; set; }

        public string ReadOnlyFileFormat { get; set; }

        public string WritableFileFormat { get; set; }
    }
}
