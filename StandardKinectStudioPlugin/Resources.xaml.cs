//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Windows;

    internal static class Resources
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static Resources()
        {
            Debug.Assert(Resources.resources == null);

            Uri uri = new Uri("/" + Assembly.GetExecutingAssembly().GetName().Name + ";component/Resources.xaml", UriKind.Relative);

            Resources.resources = new ResourceDictionary()
                {
                    Source = uri,
                };
        }

        public static object Get(string key)
        {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            return Resources.resources[key];
        }

        private static readonly ResourceDictionary resources = null;
    }
}
