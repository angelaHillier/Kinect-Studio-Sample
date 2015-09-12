//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows;
    using KinectStudioUtility;
    using Microsoft.Kinect.Tools;

    public class FileMetadataDataTemplateKey
    {
        public FileMetadataDataTemplateKey(Type valueType)
        {
            if (valueType == null)
            {
                throw new ArgumentNullException("valueType");
            }

            if (valueType.IsGenericType)
            {
                if (valueType.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>))
                {
                    valueType = valueType.GetGenericArguments()[0].MakeArrayType();

                    // no array of buffers
                    if (valueType == typeof(KStudioMetadataValueBuffer))
                    {
                        throw new ArgumentOutOfRangeException("valueType");
                    }
                }
            }

            if (!FileMetadataDataTemplateKey.validMetadataTypes.Contains(valueType))
            {
                throw new ArgumentOutOfRangeException("valueType");
            }

            this.valueType = valueType;
        }

        public FileMetadataDataTemplateKey(Type valueType, string keyName)
            : this(valueType)
        {
            if (String.IsNullOrWhiteSpace(keyName))
            {
                throw new ArgumentNullException("keyName");
            }

            this.keyName = keyName;
        }

        public override bool Equals(object obj)
        {
            bool result = false;

            FileMetadataDataTemplateKey other = obj as FileMetadataDataTemplateKey;
            if (other != null)
            {
                result = (this.valueType == other.valueType) &&
                         (this.keyName == other.keyName);
            }

            return result;
        }

        public override int GetHashCode()
        {
            int result = valueType.GetHashCode();
            if (keyName != null)
            {
                result ^= keyName.GetHashCode();
            }
            return result;
        }

        public override string ToString()
        {
            string value = "FileMetadataDataTemplateKey {" + this.ToStringFragment() + "}";

            return value;
        }

        protected virtual string ToStringFragment()
        {
            string value = this.valueType.Name;

            if (keyName != null)
            {
                value += ":" + keyName;
            }

            return value;
        }

        private readonly Type valueType;
        private readonly string keyName;

        private static readonly HashSet<Type> validMetadataTypes = new HashSet<Type>(new Type[] 
            {
                typeof(Byte),
                typeof(Int16),
                typeof(UInt16),
                typeof(Int32),
                typeof(UInt32),
                typeof(Int64),
                typeof(UInt64),
                typeof(Single),
                typeof(Double),
                typeof(Char),
                typeof(Boolean),
                typeof(DateTime),
                typeof(TimeSpan),
                typeof(Guid),
                typeof(Point),
                typeof(Size),
                typeof(Rect),
                typeof(String),
                typeof(KStudioMetadataValueBuffer),
                typeof(Byte[]),
                typeof(Int16[]),
                typeof(UInt16[]),
                typeof(Int32[]),
                typeof(UInt32[]),
                typeof(Int64[]),
                typeof(UInt64[]),
                typeof(Single[]),
                typeof(Double[]),
                typeof(Char[]),
                typeof(Boolean[]),
                typeof(DateTime[]),
                typeof(TimeSpan[]),
                typeof(Guid[]),
                typeof(Point[]),
                typeof(Size[]),
                typeof(Rect[]),
                typeof(String[]),
                typeof(KStudioInvalidMetadataValue),
            });
    }
}
