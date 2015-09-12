//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    public partial class TargetFolderBrowserDialog : Window
    {
        public TargetFolderBrowserDialog()
        {
            DebugHelper.AssertUIThread();

            this.InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            TargetFolderBrowserData data = this.DataContext as TargetFolderBrowserData;
            if (data != null)
            {
                data.SaveSettings(this.DialogResult == true);
            }

            base.OnClosed(e);
        }

        private void Refresh_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            TargetFolderBrowserData data = this.DataContext as TargetFolderBrowserData;
            if (data != null)
            {
                using (WaitCursor waitCursor = new WaitCursor(this))
                {
                    e.Handled = true;

                    data.Reload();
                }
            }
        }

        private void GoUp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            TargetFolderBrowserData data = this.DataContext as TargetFolderBrowserData;
            if (data != null)
            {
                using (WaitCursor waitCursor = new WaitCursor(this))
                {
                    e.Handled = true;

                    data.GoUp();
                }
            }
        }

        private void GoUp_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            TargetFolderBrowserData data = this.DataContext as TargetFolderBrowserData;
            if (data != null)
            {
                e.Handled = true;

                e.CanExecute = data.ParentDirectory != null;
            }
        }

        private void Ok_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TargetFolderBrowserData data = this.DataContext as TargetFolderBrowserData;
            if (data != null)
            {
                e.Handled = true;

                this.DialogResult = true;
                this.Close();
            }
        }

        private void Ok_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            TargetFolderBrowserData data = this.DataContext as TargetFolderBrowserData;
            if (data != null)
            {
                e.Handled = true;
                e.CanExecute = true;
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = DoOpen();
        }

        private bool DoOpen()
        {
            DebugHelper.AssertUIThread();
            bool handled = false;

            TargetFolderBrowserData data = this.DataContext as TargetFolderBrowserData;
            if (data != null)
            {
                if (data.SelectedItem != null)
                {
                    handled = true;

                    data.Open(data.SelectedItem);
                }
            }

            return handled;
        }

        private void ListView_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            DebugHelper.AssertUIThread();

            DependencyObject listView = sender as ListView;
            if (listView != null)
            {
                ScrollViewer scrollViewer = listView.GetVisualChild<ScrollViewer>();
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToTop();
                }
            }
        }
    }
}
