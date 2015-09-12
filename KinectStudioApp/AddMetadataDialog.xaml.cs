//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System.Windows;
    using KinectStudioUtility;

    public partial class AddMetadataDialog : Window
    {
        public AddMetadataDialog()
        {
            DebugHelper.AssertUIThread();

            this.InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            DebugHelper.AssertUIThread();

            base.OnApplyTemplate();

            this.KeyTextBox.SelectAll();
        }

        private void Button_Click_OK(object sender, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.DialogResult = true;

            Close();
        }

        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            Close();
        }
    }
}
