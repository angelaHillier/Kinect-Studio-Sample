//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Data;
    using KinectStudioUtility;

    internal class BinaryMetadataConverter : IMultiValueConverter
    {
        public BinaryMetadataConverter(Type structType, string path, IValueConverter converter, object converterParameter)
        {
            if (structType == null)
            {
                throw new ArgumentNullException("structType");
            }

            if (!structType.IsValueType)
            {
                throw new ArgumentOutOfRangeException("structType");
            }

            if (String.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            this.structType = structType;
            this.path = path;
            this.converter = converter;
            this.converterParameter = converterParameter;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            DebugHelper.AssertUIThread();

            if ((values != null) && (values.Length > 0))
            {
                this.cachedValue = values[0];
            }

            object value = GetFieldValue();

            if (this.converter != null)
            {
                value = this.converter.Convert(value, targetType, this.converterParameter, culture);
            }

            if (value != null)
            {
                Type valueType = value.GetType();

                bool doConvert = (valueType != targetType) && !valueType.IsSubclassOf(targetType);

                if (doConvert)
                {
                    if ((targetType == null) || ((targetType.IsGenericType) && (targetType.GetGenericTypeDefinition() == typeof(Nullable<>)) && (targetType.GenericTypeArguments[0] == valueType)))
                    {
                        doConvert = false;
                    }
                }

                if (doConvert)
                {
                    TypeConverter typeConverter = TypeDescriptor.GetConverter(valueType);
                    if (typeConverter != null)
                    {
                        try
                        {
                            value = typeConverter.ConvertTo(value, targetType);
                        }
                        catch (NotSupportedException)
                        {
                            // ignore
                        }
                    }
                }
            }

            return value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            DebugHelper.AssertUIThread();

            if ((targetTypes != null) && (targetTypes.Length > 0))
            {
                if (this.converter != null)
                {
                    value = this.converter.ConvertBack(value, targetTypes[0], this.converterParameter, culture);
                }

                BinaryMetadataProxy foo = this.cachedValue as BinaryMetadataProxy;

                if ((this.structType != null) && (this.path != null) && (foo != null))
                {
                    PropertyInfo propertyInfo = this.structType.GetProperty(this.path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (propertyInfo != null)
                    {
                        object data = foo.ReadFromBuffer(this.structType);

                        if (data != null)
                        {
                            propertyInfo.SetValue(data, value);
                        }

                        foo.WriteToBuffer(this.structType, data);
                    }
                    else
                    {
                        FieldInfo fieldInfo = this.structType.GetField(this.path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                        if (fieldInfo != null)
                        {
                            object data = foo.ReadFromBuffer(this.structType);

                            if (data != null)
                            {
                                fieldInfo.SetValue(data, value);
                            }

                            foo.WriteToBuffer(this.structType, data);
                        }
                        else
                        {
                            throw new InvalidOperationException("Path not found");
                        }
                    }
                }
            }

            return new object[1] { this.cachedValue };
        }

        private object GetFieldValue()
        {
            Debug.Assert(this.structType != null);
            Debug.Assert(this.path != null);
            Debug.Assert(this.cachedValue != null);

            object objectValue = null;
            object fieldValue = null;

            ReadOnlyCollection<byte> bytes = this.cachedValue as ReadOnlyCollection<byte>;

            if (bytes != null)
            {
                if (Marshal.SizeOf(this.structType) > bytes.Count)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Struct '{0}' is bigger than the binary metadata.", this.structType.FullName));
                }

                byte[] array = new byte[bytes.Count];
                bytes.CopyTo(array, 0);

                unsafe
                {
                    fixed (byte* pBytes = array)
                    {
                        IntPtr ptrBytes = new IntPtr(pBytes);
                        objectValue = Marshal.PtrToStructure(ptrBytes, this.structType);
                    }
                }
            }
            else
            {
                BinaryMetadataProxy foo = this.cachedValue as BinaryMetadataProxy;
                if (foo != null)
                {
                    objectValue = foo.ReadFromBuffer(this.structType);
                }
            }

            if (objectValue != null)
            {
                PropertyInfo propertyInfo = this.structType.GetProperty(this.path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (propertyInfo != null)
                {
                    fieldValue = propertyInfo.GetValue(objectValue);
                }
                else
                {
                    FieldInfo fieldInfo = this.structType.GetField(this.path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (fieldInfo != null)
                    {
                        fieldValue = fieldInfo.GetValue(objectValue);
                    }
                    else
                    {
                        throw new InvalidOperationException("Path not found");
                    }
                }
            }

            return fieldValue;
        }

        private object cachedValue = null;
        private readonly Type structType;
        private readonly string path;
        private readonly IValueConverter converter;
        private readonly object converterParameter;
    }
}
