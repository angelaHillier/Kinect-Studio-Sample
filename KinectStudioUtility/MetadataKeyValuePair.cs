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
    using Microsoft.Kinect.Tools;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Diagnostics;

    public class MetadataKeyValuePair : INotifyPropertyChanged
    {
        public MetadataKeyValuePair(WritableMetadataProxy proxy, string key, object value)
        {
            DebugHelper.AssertUIThread();

            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            SetValue(value);

            this.proxy = proxy;
            this.key = key;
        }

        public WritableMetadataProxy Metadata
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.proxy;
            }
        }

        public string Key
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.key;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "value")]
        public object Value
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.value;
            }
            set
            {
                DebugHelper.AssertUIThread();
                Debug.Assert(this.proxy != null);

                if (this.value != value)
                {
                    this.proxy.SetMetadata(key, value);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public void GetStreamIds(out Guid dataTypeId, out Guid semanticId)
        {
            Debug.Assert(this.proxy != null);

            this.proxy.GetStreamIds(out dataTypeId, out semanticId);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "value")]
        public void SetValue(object value)
        {
            DebugHelper.AssertUIThread();

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (!(this.value is ValueType) || (this.value != value))
            {
                this.value = value;
                RaisePropertyChanged("Value");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "value")]
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = Volatile.Read(ref PropertyChanged);

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private readonly WritableMetadataProxy proxy;
        private readonly string key;
        private object value;
    }
}