//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Windows.Threading;

    public class BinaryMetadataProxy : MetadataArrayProxy<byte>
    {
        public BinaryMetadataProxy(MetadataKeyValuePair keyValue, ReadOnlyCollection<byte> data) :
            base(keyValue, data)
        {
            DebugHelper.AssertUIThread();
        }

        public object ReadFromBuffer(Type structType)
        {
            DebugHelper.AssertUIThread();

            object value = null;

            if (structType != null)
            {
                if ((this.cachedStruct != null) && (this.cachedStruct.GetType() == structType))
                {
                    value = this.cachedStruct;
                }
                else
                {
                    MetadataKeyValuePair keyValue = this.KeyValue;
                    byte[] valueArray = this.GetValueArray();

                    Debug.Assert(keyValue != null);
                    Debug.Assert(valueArray != null);

                    if (Marshal.SizeOf(structType) > valueArray.Length)
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Struct '{0}' is bigger than the binary metadata for key '{1}'.", structType.FullName, keyValue.Key));
                    }

                    unsafe
                    {
                        fixed (byte* pValue = valueArray)
                        {
                            IntPtr ptr = new IntPtr(pValue);
                            value = Marshal.PtrToStructure(ptr, structType);
                            this.cachedStruct = value;
                        }
                    }
                }
            }

            return value;
        }

        public void WriteToBuffer(Type structType, object value)
        {
            DebugHelper.AssertUIThread();

            if (structType != null)
            {
                MetadataKeyValuePair keyValue = this.KeyValue;
                byte[] valueArray = this.GetValueArray();

                Debug.Assert(keyValue != null);
                Debug.Assert(valueArray != null);

                if (Marshal.SizeOf(structType) > valueArray.Length)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Struct '{0}' is bigger than the binary metadata for key '{1}'.", structType.FullName, keyValue.Key));
                }

                unsafe
                {
                    fixed (byte* pFoo = valueArray)
                    {
                        IntPtr ptr = new IntPtr(pFoo);
                        Marshal.StructureToPtr(value, ptr, true);
                    }
                }

                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                    {
                        keyValue.Value = valueArray;
                    }));
            }
        }

        public void GetBuffer(int bufferSize, IntPtr buffer, int startIndex)
        {
            byte[] valueArray = this.GetValueArray();

            Debug.Assert(valueArray != null);

            if (bufferSize > 0)
            {
                if (buffer == IntPtr.Zero)
                {
                    throw new ArgumentNullException("buffer");
                }

                Marshal.Copy(valueArray, startIndex, buffer, bufferSize);
            }
        }

        public void SetBuffer(int bufferSize, IntPtr buffer, int startIndex)
        {
            if (bufferSize > 0)
            {
                if (buffer == IntPtr.Zero)
                {
                    throw new ArgumentNullException("buffer");
                }

                MetadataKeyValuePair keyValue = this.KeyValue;
                byte[] valueArray = this.GetValueArray();

                Debug.Assert(keyValue != null);
                Debug.Assert(valueArray != null);

                Marshal.Copy(buffer, valueArray, startIndex, bufferSize);

                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    keyValue.Value = valueArray;
                }));
            }
        }

        private object cachedStruct = null;
    }
}
