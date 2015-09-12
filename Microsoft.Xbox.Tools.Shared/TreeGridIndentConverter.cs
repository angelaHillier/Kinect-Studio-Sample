//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Xbox.Tools.Shared
{
    using System;
    using System.Windows.Data;
    using System.Windows;

    /// <summary>
    /// Converts from nesting level to a proper indent for a tree node
    /// </summary>
    public class TreeGridIndentConverter : IValueConverter
    {
        private static double indentPerLevel = 16;
        
        /// <summary>
        /// Converts from nesting level to a proper indent for a tree node
        /// </summary>
        /// <param name="value">The source value to be converted, should be nested level of the tree node</param>
        /// <param name="targetType">The target type of the conversion, should be Thickness</param>
        /// <param name="parameter">the binding parameter, not used</param>
        /// <param name="culture">the culture information, not used</param>
        /// <returns>The converted Thickness object</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is int) || targetType != typeof(Thickness))
            {
                throw new NotSupportedException();
            }
            return new Thickness((int)value * indentPerLevel, 0, 6, 0);
        }

        /// <summary>
        /// Converts from indent value back to nesting level is not supported
        /// </summary>
        /// <param name="value">The source value to be converted, not used</param>
        /// <param name="targetType">The target type of the conversion, not used</param>
        /// <param name="parameter">The parameter of binding, not used</param>
        /// <param name="culture">The culture information, not used</param>
        /// <returns>this method does not return</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
