//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;
    using KinectStudioUtility;

    public class TypeValidationRule : ValidationRule
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            DebugHelper.AssertUIThread();

            string str = value as string;

            if (String.IsNullOrWhiteSpace(str))
            {
                return new ValidationResult(false, Strings.Validation_NoValue_Error); 
            }

            if (this.converter != null)
            {
                try
                {
                    this.converter.ConvertBack(value, typeof(Object), null, CultureInfo.CurrentCulture);
                }
                catch (Exception)
                {
                    return new ValidationResult(false, Strings.Validate_InvalidData_Error); 
                }
            }

            return new ValidationResult(true, null);
        }

        public IValueConverter Converter
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.converter;
            }

            set
            {
                DebugHelper.AssertUIThread();

                this.converter = value;
            }
        }

        private IValueConverter converter = null;
    }
}
