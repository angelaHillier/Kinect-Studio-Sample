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
    using System.Collections.Generic;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using Microsoft.Kinect.Tools;

    public abstract class BaseStringObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public abstract object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
    }

    public abstract class BaseStringArrayObjectConverter <T>: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                StringBuilder sb = new StringBuilder();

                bool first = true;

                foreach (object o in enumerable)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append("; ");
                    }

                    if (o != null)
                    {
                        IConvertible convertible = o as IConvertible;

                        if (convertible != null)
                        {
                            sb.Append(convertible.ToString(CultureInfo.CurrentCulture));
                        }
                        else
                        {
                            sb.Append(o.ToString());
                        }
                    }
                }

                value = sb.ToString();
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = (string)value;

            string[] strs = null;

            if (str != null)
            {
                strs = str.Split(new char[] { ';' });
            }

            if ((strs == null) || (strs.Length == 0))
            {
                throw new ArgumentNullException("value");
            }

            T[] values = new T[strs.Length];

            for (int i = 0; i < strs.Length; ++i)
            {
                string temp = strs[i].Trim();
                values[i] = Parse(temp);
            }

            return values;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "s")]
        protected abstract T Parse(string s);
    }

    public class StringByteConverter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Byte.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringInt16Converter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Int16.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringUInt16Converter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return UInt16.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringInt32Converter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Int32.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringUInt32Converter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return UInt32.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringInt64Converter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Int64.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringUInt64Converter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return UInt64.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringSingleConverter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Single.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringDoubleConverter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Double.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringCharConverter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Char.Parse((string)value);
        }
    }

    public class StringGuidConverter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Guid.Parse((string)value);
        }
    }

    public class StringDateTimeConverter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DateTime.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringTimeSpanConverter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TimeSpan.Parse((string)value, CultureInfo.CurrentCulture);
        }
    }

    public class StringPointConverter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Point.Parse((string)value);
        }
    }

    public class StringSizeConverter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Size.Parse((string)value);
        }
    }

    public class StringRectConverter : BaseStringObjectConverter
    {
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Rect.Parse((string)value);
        }
    }

    public class StringObjectArrayConverter : BaseStringArrayObjectConverter<Object>
    {
        protected override Object Parse(string s)
        {
            throw new InvalidOperationException();
        }
    }

    public class StringByteArrayConverter : BaseStringArrayObjectConverter<Byte>
    {
        protected override Byte Parse(string s)
        {
            return Byte.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringInt16ArrayConverter : BaseStringArrayObjectConverter<Int16>
    {
        protected override Int16 Parse(string s)
        {
            return Int16.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringUInt16ArrayConverter : BaseStringArrayObjectConverter<UInt16>
    {
        protected override UInt16 Parse(string s)
        {
            return UInt16.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringInt32ArrayConverter : BaseStringArrayObjectConverter<Int32>
    {
        protected override Int32 Parse(string s)
        {
            return Int32.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringUInt32ArrayConverter : BaseStringArrayObjectConverter<UInt32>
    {
        protected override UInt32 Parse(string s)
        {
            return UInt32.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringInt64ArrayConverter : BaseStringArrayObjectConverter<Int64>
    {
        protected override Int64 Parse(string s)
        {
            return Int64.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringUInt64ArrayConverter : BaseStringArrayObjectConverter<UInt64>
    {
        protected override UInt64 Parse(string s)
        {
            return UInt64.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringSingleArrayConverter : BaseStringArrayObjectConverter<Single>
    {
        protected override Single Parse(string s)
        {
            return Single.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringDoubleArrayConverter : BaseStringArrayObjectConverter<Double>
    {
        protected override Double Parse(string s)
        {
            return Double.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringCharArrayConverter : BaseStringArrayObjectConverter<Char>
    {
        protected override Char Parse(string s)
        {
            return Char.Parse(s);
        }
    }

    public class StringBooleanArrayConverter : BaseStringArrayObjectConverter<Boolean>
    {
        protected override Boolean Parse(string s)
        {
            return Boolean.Parse(s);
        }
    }

    public class StringGuidArrayConverter : BaseStringArrayObjectConverter<Guid>
    {
        protected override Guid Parse(string s)
        {
            return Guid.Parse(s);
        }
    }

    public class StringDateTimeArrayConverter : BaseStringArrayObjectConverter<DateTime>
    {
        protected override DateTime Parse(string s)
        {
            return DateTime.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringTimeSpanArrayConverter : BaseStringArrayObjectConverter<TimeSpan>
    {
        protected override TimeSpan Parse(string s)
        {
            return TimeSpan.Parse(s, CultureInfo.CurrentCulture);
        }
    }

    public class StringPointArrayConverter : BaseStringArrayObjectConverter<Point>
    {
        protected override Point Parse(string s)
        {
            return Point.Parse(s);
        }
    }

    public class StringSizeArrayConverter : BaseStringArrayObjectConverter<Size>
    {
        protected override Size Parse(string s)
        {
            return Size.Parse(s);
        }
    }

    public class StringRectArrayConverter : BaseStringArrayObjectConverter<Rect>
    {
        protected override Rect Parse(string s)
        {
            return Rect.Parse(s);
        }
    }

    public class StringStringArrayConverter : BaseStringArrayObjectConverter<String>
    {
        protected override String Parse(string s)
        {
            return s;
        }
    }

    public class StringBufferConverter : IValueConverter
    {
        public unsafe object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            KStudioMetadataValueBuffer buffer = value as KStudioMetadataValueBuffer;

            StringBuilder sb = new StringBuilder();

            if (buffer != null)
            {
                uint size;
                GCHandle handle;
                buffer.AccessUnderlyingDataBuffer(out size, out handle);
                IntPtr ptr = handle.AddrOfPinnedObject();
                byte* p = (byte*)ptr.ToPointer();

                for (uint i = 0; i < size; ++i)
                {
                    sb.Append(p[i].ToString("X2", CultureInfo.InvariantCulture));
                    sb.Append(" ");
                }
            }
            else
            {
                KStudioInvalidMetadataValue invalid = value as KStudioInvalidMetadataValue;

                if (invalid != null)
                {
                    foreach (byte b in invalid.Data)
                    {
                        sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
                        sb.Append(" ");
                    }
                }
            }

            value = sb.ToString();

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;
            value = null;

            if (str != null)
            {
                str = str + "\n";

                byte[] temp = new byte[str.Length];

                int i = 0;
                char? firstByte = null;

                foreach (char ch in str)
                {
                    char ch2 = Char.ToLowerInvariant(ch);

                    if (Char.IsDigit(ch2) || (('a' <= ch2) && ('f' >= ch2)))
                    {
                        if (firstByte.HasValue)
                        {
                            byte b;
                            if (byte.TryParse(firstByte.Value.ToString() + ch2, NumberStyles.AllowHexSpecifier, null, out b))
                            {
                                temp[i] = b;
                                ++i;
                                firstByte = null;
                            }
                        }
                        else
                        {
                            firstByte = ch;
                        }
                    }
                    else
                    {
                        if (firstByte.HasValue)
                        {
                            byte b;
                            if (byte.TryParse(firstByte.Value.ToString(), NumberStyles.AllowHexSpecifier, null, out b))
                            {
                                temp[i] = b;
                                ++i;
                                firstByte = null;
                            }
                        }
                    }
                }

                if (i > 0)
                {
                    byte[] final = new byte[i];
                    Array.Copy(temp, final, i);
                    value = new KStudioMetadataValueBuffer(final);
                }
            }

            if (value == null)
            {
                // no empty data
                value = new KStudioMetadataValueBuffer(new byte[] { 0 });
            }

            return value;
        }
    }
}
