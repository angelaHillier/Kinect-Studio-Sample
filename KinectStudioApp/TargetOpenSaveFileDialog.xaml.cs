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

    public partial class TargetOpenSaveFileDialog : Window
    {
        public TargetOpenSaveFileDialog()
        {
            DebugHelper.AssertUIThread();

            this.InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            TargetOpenSaveFileData data = this.DataContext as TargetOpenSaveFileData;
            if (data != null)
            {
                data.SaveSettings(this.DialogResult == true);
            }

            base.OnClosed(e);
        }

        private void Refresh_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            TargetOpenSaveFileData data = this.DataContext as TargetOpenSaveFileData;
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

            TargetOpenSaveFileData data = this.DataContext as TargetOpenSaveFileData;
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

            TargetOpenSaveFileData data = this.DataContext as TargetOpenSaveFileData;
            if (data != null)
            {
                e.Handled = true;

                e.CanExecute = data.ParentDirectory != null;
            }
        }

        private void Ok_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TargetOpenSaveFileData data = this.DataContext as TargetOpenSaveFileData;
            if (data != null)
            {
                if (data.IsSaveDialog)
                {
                    e.Handled = DoSave();
                }
                else
                {
                    e.Handled = DoOpen();
                }
            }
        }

        private void Ok_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            TargetOpenSaveFileData data = this.DataContext as TargetOpenSaveFileData;
            if (data != null)
            {
                e.Handled = true;

                if (data.IsSaveDialog)
                {
                    e.CanExecute = !String.IsNullOrWhiteSpace(data.FileName);
                }
                else
                {
                    e.CanExecute = data.SelectedItem != null;
                }
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

            TargetOpenSaveFileData data = this.DataContext as TargetOpenSaveFileData;
            if (data != null)
            {
                if (data.SelectedItem is string)
                {
                    handled = true;

                    data.Open((string)data.SelectedItem);
                }
                else if (data.SelectedItem is KStudioFileInfo)
                {
                    handled = true;

                    this.DialogResult = true;

                    Close();
                }
            }

            return handled;
        }

        private bool DoSave()
        {
            DebugHelper.AssertUIThread();
            bool handled = false;

            TargetOpenSaveFileData data = this.DataContext as TargetOpenSaveFileData;
            if (data != null)
            {
                if (data.IsValidFileName)
                {
                    handled = true;

                    this.DialogResult = true;

                    Close();
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
