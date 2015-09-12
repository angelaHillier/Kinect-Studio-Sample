//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Windows.Threading;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class MetadataArrayProxy<T> : IEnumerable<T> where T : struct
    {
        public MetadataArrayProxy(MetadataKeyValuePair keyValue, ReadOnlyCollection<T> data)
        {
            if (keyValue == null)
            {
                throw new ArgumentNullException("keyValue");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            this.keyValue = keyValue;

            this.valueArray = new T[data.Count];
            data.CopyTo(this.valueArray, 0);
        }

        public T this[int index]
        {
            get
            {
                DebugHelper.AssertUIThread();

                Debug.Assert(this.valueArray != null);

                return this.valueArray[index];
            }
            set
            {
                DebugHelper.AssertUIThread();

                Debug.Assert(this.keyValue != null);
                Debug.Assert(this.valueArray != null);

                if (!value.Equals(this.valueArray[index]))
                {
                    this.valueArray[index] = value;

                    MetadataKeyValuePair keyValue = this.keyValue;
                    T[] valueArray = this.valueArray;

                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                        {
                            keyValue.Value = valueArray;
                        }));
                }
            }
        }

        public int Count
        {
            get
            {
                Debug.Assert(this.valueArray != null);

                return this.valueArray.Length;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            Debug.Assert(this.valueArray != null);

            return ((IEnumerable<T>)this.valueArray).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            Debug.Assert(this.valueArray != null);

            return this.valueArray.GetEnumerator();
        }

        protected MetadataKeyValuePair KeyValue
        {
            get
            {
                DebugHelper.AssertUIThread();

                Debug.Assert(this.keyValue != null);

                return this.keyValue;
            }
        }

        protected T[] GetValueArray()
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(this.valueArray != null);

            return this.valueArray;
        }

        private MetadataKeyValuePair keyValue;
        private T[] valueArray;
    }
}
