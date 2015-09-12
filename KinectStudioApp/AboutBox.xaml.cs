//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;

    public partial class AboutBox : ContentControl
    {
        public AboutBox()
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(EntryAssembly.Location);

            this.ProductVersion = GetVersionString(info);
            this.DataContext = this;

            this.InitializeComponent();
        }

        public string ProductVersion
        {
            get { return GetValue(ProductVersionProperty) as string; }
            set { SetValue(ProductVersionProperty, value); }
        }

        private static Assembly EntryAssembly
        {
            get
            {
                var entryAssembly = Assembly.GetEntryAssembly();

                if (entryAssembly == null)
                {
                    entryAssembly = Application.ResourceAssembly;
                }

                if (entryAssembly == null)
                {
                    throw new InvalidOperationException();
                }

                return entryAssembly;
            }
        }

        private static string GetVersionString(FileVersionInfo info)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
        }

        public static readonly DependencyProperty ProductVersionProperty = DependencyProperty.Register(
            "ProductVersion", typeof(string), typeof(AboutBox));
    }
}
