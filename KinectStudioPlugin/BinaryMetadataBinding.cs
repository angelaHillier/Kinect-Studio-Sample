//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Windows.Data;
    using System.Windows.Markup;
    using KinectStudioUtility;

    public class BinaryMetadataBinding : MarkupExtension
    {
        public BindingMode BindingMode
        {
            get
            {
                return this.bindingMode.GetValueOrDefault(BindingMode.Default);
            }

            set
            {
                DebugHelper.AssertUIThread();

                // not dealing with changing these after initial set up
                if (this.bindingMode != null)
                {
                    throw new ArgumentException("BindingMode already set");
                }

                this.bindingMode = value;
            }
        }

        public string Path
        {
            get
            {
                return this.path;
            }

            set
            {
                DebugHelper.AssertUIThread();

                // not dealing with changing these after initial set up
                if (this.path != null)
                {
                    throw new ArgumentException("Path already set");
                }

                this.path = value;
            }
        }

        public IValueConverter Converter 
        {
            get
            {
                return this.converter;
            }

            set
            {
                DebugHelper.AssertUIThread();

                // not dealing with changing these after initial set up
                if (this.converter != null)
                {
                    throw new ArgumentException("Converter already set");
                }

                this.converter = value;
            }
        }

        public object ConverterParameter 
        { 
            get 
            {
                return this.converterParameter;
            }

            set
            {
                DebugHelper.AssertUIThread();

                // not dealing with changing these after initial set up
                if (this.converterParameter != null)
                {
                    throw new ArgumentException("ConverterParamenter already set");
                }

                this.converterParameter = value;
            }
        }

        public Type StructType
        {
            get
            {
                return this.structType;
            }

            set
            {
                DebugHelper.AssertUIThread();

                // not dealing with changing these after initial set up
                if (this.structType != null)
                {
                    throw new ArgumentException("StructType already set");
                }

                this.structType = value;
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            object value = null;

            Binding binding = new Binding("Value");
            BinaryMetadataConverter instanceConverter = new BinaryMetadataConverter(this.structType, this.path, this.converter, this.converterParameter);

            MultiBinding multiBinding = new MultiBinding()
                {
                    Converter = instanceConverter,
                    Mode = this.BindingMode,
                };
            multiBinding.Bindings.Add(binding);

            value = multiBinding.ProvideValue(serviceProvider);

            return value;
        }

        private BindingMode? bindingMode = null;
        private string path = null;
        private IValueConverter converter = null;
        private object converterParameter = null;
        private Type structType = null;
    }
}
