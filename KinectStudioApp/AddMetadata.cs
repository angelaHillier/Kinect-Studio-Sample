//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    public class AddMetadata : KStudioUserState, INotifyDataErrorInfo
    {
        public AddMetadata(WritableMetadataProxy metadata)
        {
            DebugHelper.AssertUIThread();

            if (metadata == null)
            {
                throw new ArgumentNullException("metadata");
            }

            this.metadata = metadata;
            this.key = Strings.Metadata_DefaultNewKey;

            if (this.metadata.ContainsKey(this.key))
            {
                string format = Strings.Metadata_DefaultNewKey_Format;
                int i = 2;

                while (true)
                {
                    this.key = String.Format(CultureInfo.CurrentCulture, format, i++);
                    if (!this.metadata.ContainsKey(this.key))
                    {
                        break;
                    }
                }
            }

            selectedValueType = typeof(String);
        }

        public string Key
        {
            get
            {
                return this.key;
            }

            set
            {
                DebugHelper.AssertUIThread();

                Debug.Assert(this.metadata != null);

                if (value == null)
                {
                    value = String.Empty;
                }
                else
                {
                    value = value.Trim();
                }

                this.key = value;

                RaisePropertyChanged("Key");

                Validate();
            }
        }

        public Type SelectedValueType
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.selectedValueType;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value != this.selectedValueType)
                {
                    this.selectedValueType = value;

                    RaisePropertyChanged("SelectedValueType");

                    Validate();
                }
            }
        }

        public object SelectedDefaultValue
        {
            get
            {
                DebugHelper.AssertUIThread();

                TypeDefault tdFound = AddMetadata.metadataTypes.FirstOrDefault(td => td.Type == this.selectedValueType);

                if (tdFound == null)
                {
                    return null;
                }

                return tdFound.DefaultValue;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public IEnumerable<Type> SupportedValueTypes
        {
            get
            {
                List<Type> types = new List<Type>();
                types.AddRange(AddMetadata.metadataTypes.Select(td => td.Type));
                return types;
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            DebugHelper.AssertUIThread();

            IEnumerable value = null;

            if (this.error != null)
            {
                value = new string[] { this.error };
            }

            return value;
        }

        public bool HasErrors
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.error != null;
            }
        }

        private void Validate()
        {
            DebugHelper.AssertUIThread();

            string oldError = this.error;
            this.error = null;

            if (String.IsNullOrWhiteSpace(this.key))
            {
                this.error = Strings.MetadataKey_Error_Blank;
            }

            if (this.metadata.ContainsKey(this.key))
            {
                this.error = Strings.MetadataKey_Error_Duplicate;
            }

            if ((this.error != null) || (oldError != null))
            {
                if (ErrorsChanged != null)
                {
                    ErrorsChanged(this, new DataErrorsChangedEventArgs("Key"));
                }
            }
        }

        private WritableMetadataProxy metadata;
        private string key;
        private Type selectedValueType;
        private string error = null;

        private class TypeDefault
        {
            public TypeDefault(Type type, object defaultValue)
            {
                Debug.Assert(type != null);
                Debug.Assert(defaultValue != null);

                this.Type = type;
                this.DefaultValue = defaultValue;
            }

            public Type Type { get; private set; }

            public object DefaultValue { get; private set; }
        }

        private static readonly TypeDefault[] metadataTypes = new TypeDefault[]
            { 
                // in order to display

                new TypeDefault(typeof(String), String.Empty),
                new TypeDefault(typeof(KStudioMetadataValueBuffer), new KStudioMetadataValueBuffer(new byte[] { 0 })),
                new TypeDefault(typeof(Byte), (Byte)0),
                new TypeDefault(typeof(Int16), (Int16)0),
                new TypeDefault(typeof(UInt16), (UInt16)0),
                new TypeDefault(typeof(Int32), (Int32)0),
                new TypeDefault(typeof(UInt32), (UInt32)0),
                new TypeDefault(typeof(Int64), (Int64)0),
                new TypeDefault(typeof(UInt64), (UInt64)0),
                new TypeDefault(typeof(Single), (Single)0.0),
                new TypeDefault(typeof(Double), (Double)0.0),
                new TypeDefault(typeof(Char), 'X'),
                new TypeDefault(typeof(Boolean), false),
                new TypeDefault(typeof(DateTime), DateTime.UtcNow.Date),
                new TypeDefault(typeof(TimeSpan), TimeSpan.Zero),
                new TypeDefault(typeof(Guid), Guid.Empty),
                new TypeDefault(typeof(Point), new Point(0.0, 0.0)),
                new TypeDefault(typeof(Size), new Size(0.0, 0.0)),
                new TypeDefault(typeof(Rect), new Rect(0.0, 0.0, 0.0, 0.0)),
                new TypeDefault(typeof(String[]), new String[] { String.Empty }),
                new TypeDefault(typeof(Byte[]), new Byte[] { 0 }),
                new TypeDefault(typeof(Int16[]), new Int16[] { 0 }),
                new TypeDefault(typeof(UInt16[]), new UInt16[] { 0 }),
                new TypeDefault(typeof(Int32[]), new Int32[] { 0 }),
                new TypeDefault(typeof(UInt32[]), new UInt32[] { 0 }),
                new TypeDefault(typeof(Int64[]), new Int64[] { 0 }),
                new TypeDefault(typeof(UInt64[]), new UInt64[] { 0 }),
                new TypeDefault(typeof(Single[]), new Single[] { 0.0f }),
                new TypeDefault(typeof(Double[]), new Double[] { 0.0 }),
                new TypeDefault(typeof(Char[]), new Char[] { 'X' }),
                new TypeDefault(typeof(Boolean[]), new Boolean[] { false }),
                new TypeDefault(typeof(DateTime[]), new DateTime[] { DateTime.UtcNow.Date }),
                new TypeDefault(typeof(TimeSpan[]), new TimeSpan[] { TimeSpan.Zero }),
                new TypeDefault(typeof(Guid[]), new Guid[] { Guid.Empty }),
                new TypeDefault(typeof(Point[]), new Point[] { new Point(0.0, 0.0) }),
                new TypeDefault(typeof(Size[]), new Size[] { new Size(0.0, 0.0) }),
                new TypeDefault(typeof(Rect[]), new Rect[] { new Rect(0.0, 0.0, 0.0, 0.0) }),
            };
    }
}
