//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using Microsoft.Win32;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using Microsoft.Xbox.Tools.Shared;
    using Microsoft.Kinect.Tools;
    using KinectStudioPlugin;
    using KinectStudioUtility;
    using System.Windows.Threading;

#if false

    public partial class MainWindow : Window
    {
        protected override void OnSourceInitialized(EventArgs e) 
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(this.JumpResizeProc));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT 
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private const int WM_SIZING = 0x0214;

        private const int WMSZ_LEFT = 1;
        private const int WMSZ_RIGHT = 2;
        private const int WMSZ_TOP = 3;
        private const int WMSZ_TOPLEFT = 4;
        private const int WMSZ_TOPRIGHT = 5;
        private const int WMSZ_BOTTOM = 6;
        private const int WMSZ_BOTTOMLEFT = 7;
        private const int WMSZ_BOTTOMRIGHT = 8;

        private const int cJump = 50;

        private IntPtr JumpResizeProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) 
        {
            switch (msg) 
            {
                case WM_SIZING:
                    RECT rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));

                    double width = rect.right - rect.left;
                    double height = rect.bottom - rect.top;

                    bool doTop = false;
                    bool doBottom = false;
                    bool doLeft = false;
                    bool doRight = false;

                    switch (wParam.ToInt32()) {
                        case WMSZ_LEFT:
                            doLeft = true;
                            break;

                        case WMSZ_RIGHT:
                            doRight = true;
                            break;

                        case WMSZ_TOP:
                            doTop = true;
                            break;

                        case WMSZ_TOPLEFT:
                            doTop = true;
                            doLeft = true;
                            break;

                        case WMSZ_TOPRIGHT:
                            doTop = true;
                            doRight = true;
                            break;

                        case WMSZ_BOTTOM:
                            doBottom = true;
                            break;

                        case WMSZ_BOTTOMLEFT:
                            doBottom = true;
                            doLeft = true;
                            break;

                        case WMSZ_BOTTOMRIGHT:
                            doBottom = true;
                            doRight = true;
                            break;
                    }

                    Debug.Assert(!(doLeft && doRight));
                    Debug.Assert(!(doTop && doBottom));

                    if (doTop)
                    {
                        rect.top = rect.bottom - (int)(Math.Floor(height / cJump) * cJump);
                    }
                    else if (doBottom)
                    {
                        rect.bottom = rect.top + (int)(Math.Floor(height / cJump) * cJump);
                    }

                    if (doLeft)
                    {
                        rect.left = rect.right - (int)(Math.Floor(width / cJump) * cJump);
                    }
                    else if (doRight)
                    {
                        rect.right = rect.left + (int)(Math.Floor(width / cJump) * cJump);
                    }

                    Marshal.StructureToPtr(rect, lParam, false);
                    break;
            }

            return IntPtr.Zero;
        }
#endif

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public partial class MainWindow : ToolsUIWindow
    {
        private void SwitchToView(string viewName)
        {
            DebugHelper.AssertUIThread();

            if (String.IsNullOrWhiteSpace(viewName))
            {
                throw new ArgumentNullException("viewName");
            }

            LayoutInstance currentLayout = null;
            if (this.LayoutTabControl.SelectedIndex >= 0)
            {
                currentLayout = this.LayoutTabControl.Items[this.LayoutTabControl.SelectedIndex] as LayoutInstance;
            }

            if ((currentLayout == null) || (currentLayout.FindView(viewName) == null))
            {
                foreach (LayoutInstance layout in this.LayoutTabControl.Items)
                {
                    if (layout != currentLayout)
                    {
                        View view = layout.FindView(viewName);

                        if (view != null)
                        {
                            view.Activate();
                            break;
                        }
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void BrowseLocalFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();
            
            if (e != null)
            {
                e.Handled = true;
            }

            if (this.kstudioService != null)
            {
                string currentPath = this.kstudioService.Settings.TargetFilePath;
                string fileSpec = "*.xef";

                IMostRecentlyUsedService mruService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(IMostRecentlyUsedService)) as IMostRecentlyUsedService;
                if (mruService != null)
                {
                    mruService.GetLocalFileDialogSettings(ref currentPath, ref fileSpec);
                }

                int filterIndex = 0;
                switch (fileSpec) // note: 1 based
                {
                    case "*.xef": filterIndex = 1; break;
                    case "*.xrf": filterIndex = 2; break;
                    case "*.*":   filterIndex = 3; break;
                }

                bool readOnly = true;
                if (e.Parameter != null)
                {
                    bool temp;
                    if (bool.TryParse(e.Parameter as string, out temp))
                    {
                        readOnly = temp;
                    }
                }

                OpenFileDialog openFileDialog = new OpenFileDialog()
                    {
                        Filter = Strings.FileOpenSave_FileSpec_AllOptions,
                        FilterIndex = filterIndex,
                        InitialDirectory = currentPath,
                        CheckPathExists = true,
                    };
                {
                    if (openFileDialog.ShowDialog() == true)
                    {
                        string filePath = openFileDialog.FileName;
                        switch (openFileDialog.FilterIndex) // note: 1 based
                        {
                            case 1: fileSpec = "*.xef"; break;
                            case 2: fileSpec = "*.xrf"; break;
                            default: fileSpec = "*.*"; break;
                        }

                        if (mruService != null)
                        {
                            mruService.SetLocalFileDialogSettings(Path.GetDirectoryName(filePath), fileSpec);
                        }

                        this.IsFileTabOpen = false;
                        this.OpenFile(filePath, readOnly);
                    }
                }
            }
        }

        private void BrowseLocalFileCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = this.kstudioService != null;
            }
        }

        private void BrowseTargetFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(false);
        }

        private void BrowseTargetFileCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = true;
            }
        }

        // weird StyleCop error even though FolderBrowserDialog is being disposed
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void BrowseTargetFilePathCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            if (this.kstudioService != null)
            {
                using (System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog()
                    {
                        SelectedPath = this.kstudioService.Settings.TargetFilePath
                    })
                {
                    if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        this.kstudioService.Settings.TargetFilePath = folderBrowser.SelectedPath;
                    }
                }
            }
        }

        private void BrowseTargetFilePathCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
                e.CanExecute = this.kstudioService != null;
            }
        }

        private void TargetConnectCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    {
                        string currentFileName = this.kstudioService.PlaybackFilePath;
                        bool readOnly = this.kstudioService.IsPlaybackFileReadOnly;

                        this.kstudioService.ConnectToTarget(this.kstudioService.TargetAddress, this.kstudioService.TargetAlias);

                        if (!String.IsNullOrWhiteSpace(currentFileName))
                        {
                            this.suppressAutoSwitch = true;
                            this.kstudioService.ClosePlayback();
                            this.kstudioService.OpenTargetPlayback(currentFileName, readOnly);
                        }
                    }
                }
            }
        }

        private void TargetConnectCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && !this.kstudioService.IsTargetConnected;
            }
        }

        private void TargetDisconnectCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    string currentFileName = this.kstudioService.PlaybackFilePath;
                    bool readOnly = this.kstudioService.IsPlaybackFileReadOnly;

                    this.kstudioService.DisconnectFromTarget();

                    if (!String.IsNullOrWhiteSpace(currentFileName))
                    {
                        this.suppressAutoSwitch = true;
                        this.kstudioService.OpenLocalPlayback(currentFileName, readOnly);
                    }
                }
            }
        }

        private void TargetDisconnectCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && this.kstudioService.IsTargetConnected;
            }
        }

        private void RecordStartCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.StartRecording();
                }
            }
        }

        private void RecordStartCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && this.kstudioService.CanRecord;
            }
        }

        private void RecordStopCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.StopRecording();
                }
            }
        }

        private void RecordStopCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && (this.kstudioService.RecordingState == KStudioRecordingState.Recording);
            }
        }

        private void RecordCloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.IsFileTabOpen = false;

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.CloseRecording(true);
                }
            }
        }

        private void RecordCloseCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && this.kstudioService.HasRecording && (this.kstudioService.RecordingState != KStudioRecordingState.Recording);
            }
        }

        private void RecordUnselectAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if ((this.kstudioService != null) && this.kstudioService.IsTargetConnected && !this.kstudioService.HasRecording)
                {
                    foreach (KStudioEventStream eventStream in this.kstudioService.TargetRecordableStreams)
                    {
                        EventStreamState eventStreamState = eventStream.UserState as EventStreamState;
                        if (eventStreamState != null)
                        {
                            eventStreamState.IsSelectedForTargetRecordingNoCheck = false;
                            eventStreamState.IsEnabledForTargetRecording = true;
                        }
                    }

                    this.kstudioService.DoTargetRecordingDance(null, false);
                }
            }
        }

        private void RecordUnselectAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) &&
                    this.kstudioService.IsTargetConnected && 
                    !this.kstudioService.HasRecording;
            }
        }

        private void PlaybackOpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            this.IsFileTabOpen = true;

            this.ActivateFileTabPage("PART_OpenFile");
        }

        private void PlaybackOpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null);
            }
        }

        private void PlaybackCloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.IsFileTabOpen = false;

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.ClosePlayback();
                }
            }
        }

        private void PlaybackCloseCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && this.kstudioService.HasPlaybackFile;
            }
        }

        private void PlaybackPlayCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.PlayPlayback();
                }
            }
        }

        private void PlaybackPlayCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && 
                    ((this.kstudioService.HasPlayback &&
                     ((this.kstudioService.PlaybackState == KStudioPlaybackState.Idle) ||
                      (this.kstudioService.PlaybackState == KStudioPlaybackState.Paused) ||
                      (this.kstudioService.PlaybackState == KStudioPlaybackState.Stopped))) ||
                    (!this.kstudioService.HasPlayback && this.kstudioService.CanPlayback));
            }
        }

        private void PlaybackPauseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.PausePlayback();
                }
            }
        }

        private void PlaybackPauseCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && (this.kstudioService.PlaybackState == KStudioPlaybackState.Playing);
            }
        }

        private void PlaybackStopCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.StopPlayback();
                }
            }
        }

        private void PlaybackStopCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && 
                    ((this.kstudioService.PlaybackState == KStudioPlaybackState.Playing) ||
                     (this.kstudioService.PlaybackState == KStudioPlaybackState.Paused) ||
                     (this.kstudioService.PlaybackState == KStudioPlaybackState.Idle));
            }
        }

        private void PlaybackStepCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.StepPlayback();
                }
            }
        }

        private void PlaybackStepCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) &&
                    ((this.kstudioService.HasPlayback &&
                     ((this.kstudioService.PlaybackState == KStudioPlaybackState.Idle) ||
                      (this.kstudioService.PlaybackState == KStudioPlaybackState.Paused) ||
                      (this.kstudioService.PlaybackState == KStudioPlaybackState.Stopped))) ||
                    (!this.kstudioService.HasPlayback && this.kstudioService.CanPlayback));
            }
        }

        private void PlaybackToggleCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    if (this.kstudioService.HasPlayback || this.kstudioService.CanPlayback)
                    {
                        if (this.kstudioService.PlaybackState == KStudioPlaybackState.Playing)
                        {
                            this.kstudioService.PausePlayback();
                        }
                        else
                        {
                            this.kstudioService.PlayPlayback();
                        }
                    }
                }
            }
        }

        private void PlaybackToggleCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) &&
                    (this.kstudioService.HasPlayback || this.kstudioService.CanPlayback);
            }
        }

        private void PlaybackUnselectAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if ((this.kstudioService != null) && (this.kstudioService.PlaybackableFileStreams != null))
                {
                    foreach (KStudioEventStream eventStream in this.kstudioService.PlaybackableFileStreams)
                    {
                        EventStreamState eventStreamState = eventStream.UserState as EventStreamState;
                        if ((eventStreamState != null) && (eventStreamState.SelectedLivePlaybackStream != null))
                        {
                            EventStreamState liveEventStreamState = eventStreamState.SelectedLivePlaybackStream.UserState as EventStreamState;
                            liveEventStreamState.IsSelectedForTargetPlaybackNoCheck = false;
                            liveEventStreamState.IsEnabledForTargetPlayback = true;
                        }
                    }

                    this.kstudioService.DoTargetPlaybackDance(null, false);
                }
            }
        }

        private void PlaybackUnselectAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) &&
                    this.kstudioService.HasPlaybackFile &&
                    this.kstudioService.IsPlaybackFileOnTarget &&
                    !this.kstudioService.HasPlayback;
            }
        }

        private void PlaybackDisableAllPausePointsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    TimelinePausePointsCollection pausePoints = this.kstudioService.PlaybackPausePoints;
                    if (pausePoints != null)
                    {
                        pausePoints.DisableAll();
                    }
                }
            }
        }

        private void PlaybackDisableAllPausePointsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) &&
                    (this.kstudioService.PlaybackPausePoints != null) &&
                    this.kstudioService.PlaybackPausePoints.HasEnabled;
            }
        }

        private void PlaybackEnableAllPausePointsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    TimelinePausePointsCollection pausePoints = this.kstudioService.PlaybackPausePoints;
                    if (pausePoints != null)
                    {
                        pausePoints.EnableAll();
                    }
                }
            }
        }

        private void PlaybackEnableAllPausePointsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) &&
                    (this.kstudioService.PlaybackPausePoints != null) &&
                    this.kstudioService.PlaybackPausePoints.HasDisabled;
            }
        }

        private void PlaybackRemoveAllPausePointsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            MessageBoxResult result = MessageBoxResult.Yes;
            if (this.notificationService != null)
            {
                result = this.notificationService.ShowMessageBox(Strings.ConfirmPausePointRemoveAll_Text, 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            }

            if (result == MessageBoxResult.Yes)
            {
                using (WaitCursor waitCursor = new WaitCursor(this))
                {
                    if (this.kstudioService != null)
                    {
                        TimelinePausePointsCollection pausePoints = this.kstudioService.PlaybackPausePoints;
                        if (pausePoints != null)
                        {
                            pausePoints.RemoveAll();
                        }
                    }
                }
            }
        }

        private void PlaybackRemoveAllPausePointsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) &&
                    (this.kstudioService.PlaybackPausePoints != null) &&
                    (this.kstudioService.PlaybackPausePoints.Count > 0);
            }
        }

        private void MonitorStartCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.StartMonitor();
                }
            }
        }

        private void MonitorStartCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && this.kstudioService.CanMonitor;
            }
        }

        private void MonitorStopCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.StopMonitor();
                }
            }
        }

        private void MonitorStopCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && (this.kstudioService.MonitorState == KStudioMonitorState.Monitoring);
            }
        }

        private void MonitorUnselectAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if ((this.kstudioService != null) && this.kstudioService.IsTargetConnected && !this.kstudioService.HasMonitor)
                {
                    foreach (KStudioEventStream eventStream in this.kstudioService.TargetMonitorableStreams)
                    {
                        EventStreamState eventStreamState = eventStream.UserState as EventStreamState;
                        if (eventStreamState != null)
                        {
                            eventStreamState.IsSelectedForTargetMonitorNoCheck = false;
                            eventStreamState.IsEnabledForTargetMonitor = true;
                        }
                    }

                    this.kstudioService.DoTargetMonitorDance(null, false);
                }
            }
        }

        private void MonitorUnselectAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) &&
                    this.kstudioService.IsTargetConnected &&
                    !this.kstudioService.HasMonitor;
            }
        }

        private void SetInOutPointsByTimelineRangeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                Timeline timeline = e.Parameter as Timeline;

                ulong start;
                ulong end;

                if ((timeline != null) && (this.kstudioService != null) && timeline.GetSelectionRange(out start, out end))
                {
                    TimelineInOutPointsCollection inOutPoints = this.kstudioService.PlaybackInOutPoints;

                    if ((inOutPoints != null) && inOutPoints.IsEnabled)
                    {
                        this.kstudioService.PlaybackInOutPoints.InPoint = TimeSpan.Zero;
                        this.kstudioService.PlaybackInOutPoints.OutPoint = TimeSpan.Zero;

                        TimeSpan startTime = TimeSpan.FromTicks((long)(start / 100));
                        TimeSpan endTime = TimeSpan.FromTicks((long)((end + 99) / 100));

                        this.kstudioService.PlaybackInOutPoints.OutPoint = endTime;
                        this.kstudioService.PlaybackInOutPoints.InPoint = startTime;
                    }
                }
            }
        }

        private void SetInOutPointsByTimelineRangeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                Timeline timeline = e.Parameter as Timeline;

                ulong start;
                ulong end;

                if ((timeline != null) && (this.kstudioService != null) && timeline.GetSelectionRange(out start, out end))
                {
                    TimelineInOutPointsCollection inOutPoints = this.kstudioService.PlaybackInOutPoints;

                    e.CanExecute = (inOutPoints != null) && inOutPoints.IsEnabled;
                }
            }
        }

        private void EditBufferSizeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            if (this.kstudioService != null)
            {
                EditUInt32Dialog dialog = new EditUInt32Dialog()
                    {
                        Owner = this,
                        Title = Strings.EditBufferSize_Title,
                        Prompt = Strings.EditBufferSize_Prompt,
                        Minimum = 2,
                        Maximum = (UInt32)((App)System.Windows.Application.Current).PhysicalMemoryMB,
                        Value = this.kstudioService.Settings.RecordingBufferSizeMB,
                    };

                if (dialog.ShowDialog() == true)
                {
                    this.kstudioService.Settings.RecordingBufferSizeMB = dialog.Value;
                }
            }
        }

        private void EditBufferSizeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null);
            }
        }

        private void SetDefaultBufferSizeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            if (this.kstudioService != null)
            {
                this.kstudioService.Settings.RecordingBufferSizeMB = 1024; // 1 gigabyte
            }
        }

        private void SetDefaultBufferSizeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null);
            }
        }

        private void EditPluginMetadataSettingsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            if (this.pluginService != null)
            {
                if (this.pluginService.ShowMetadataPlugins(this))
                {
                    if (this.metadataViewService != null)
                    {
                        this.metadataViewService.UpdateMetadataControls();
                    }
                }
            }

            this.IsFileTabOpen = false;
        }

        private void EditPluginMetadataSettingsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.pluginService != null);
            }
        }

        private void SelectMetadataViewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.IsFileTabOpen = false;

            if ((this.metadataViewService != null) && (e != null))
            {
                e.Handled = true;

                Button button = e.OriginalSource as Button;
                MetadataInfo metadataInfo = e.Parameter as MetadataInfo;
                if ((button != null) && (metadataInfo != null))
                {
                    IEnumerable<MetadataView> metadataViews = this.metadataViewService.GetMetadataViews(this);

                    if (metadataViews != null)
                    {
                        switch (metadataViews.Count())
                        {
                            case 0:
                                {
                                    if (this.notificationService != null)
                                    {
                                        this.notificationService.ShowMessageBox(this, Strings.ErrorNoMetadataView_Text, /*Strings.ErrorNoMetadataView_Title,*/
                                            MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
                                    }
                                }
                                break;

                            case 1:
                                {
                                    this.ShowMetadata(metadataViews.First(), metadataInfo);
                                }
                                break;

                            default:
                                {
                                    button.ContextMenu = new ContextMenu()
                                        {
                                            DataContext = metadataInfo,
                                            ItemContainerStyle = System.Windows.Application.Current.Resources["MetadataViewSelectorMenuItemStyle"] as Style,
                                            ItemsSource = metadataViews,
                                            PlacementTarget = button,
                                            IsOpen = true,
                                        };
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void SelectMetadataViewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (e.Parameter is MetadataInfo) && (this.metadataViewService != null);
            }
        }

        private void MetadataCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.IsFileTabOpen = false;

            if (e != null)
            {
                e.Handled = true;
            }

            object[] parameters = e.Parameter as object[];

            if ((parameters != null) && (parameters.Length >= 2))
            {
                MetadataInfo metadataInfo = parameters[0] as MetadataInfo;
                MetadataView view = parameters[1] as MetadataView;

                this.ShowMetadata(view, metadataInfo);
            }
        }

        private void ShowMetadata(MetadataView view, MetadataInfo metadataInfo)
        {
            DebugHelper.AssertUIThread();

            this.IsFileTabOpen = false;

            if ((metadataInfo != null) && (view != null))
            {
                MetadataViewContent viewContent = view.ViewContent as MetadataViewContent;
                if (viewContent != null)
                {
                    using (WaitCursor waitCursor = new WaitCursor(this))
                    {
                        viewContent.MetadataInfo = metadataInfo;
                    }
                }
            }
        }

        private void MetadataCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = true;
            }
        }

        private void AddMetadataCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            WritableMetadataProxy metadata = e.Parameter as WritableMetadataProxy;

            if (metadata != null)
            {
                AddMetadata data = new AddMetadata(metadata);

                AddMetadataDialog dialog = new AddMetadataDialog()
                    {
                        Owner = this,
                        DataContext = data,
                        ShowInTaskbar = false,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                    };

                Point point = Mouse.PrimaryDevice.GetPosition(this);

                if ((point.X + dialog.Width) > this.ActualWidth)
                {
                    point.X = this.ActualWidth - dialog.Width;
                }

                if ((point.Y + dialog.Height) > this.ActualHeight)
                {
                    point.Y = this.ActualHeight - dialog.Height;
                }

                dialog.Left = point.X + this.Left;
                dialog.Top = point.Y + this.Top;

                if (dialog.ShowDialog() == true)
                {
                    string key = data.Key;
                    object defaultValue = data.SelectedDefaultValue;

                    Debug.Assert(!String.IsNullOrWhiteSpace(key));
                    Debug.Assert(!metadata.ContainsKey(key));
                    Debug.Assert(defaultValue != null);

                    metadata[key] = defaultValue;
                }
            }
        }

        private void AddMetadataCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = true; 
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Windows.MessageBox.Show(System.String,System.String)")]
        private void DeleteMetadataCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            object[] parameters = e.Parameter as object[];
            if ((parameters != null) && (parameters.Length >= 2))
            {
                WritableMetadataProxy metadata = parameters[0] as WritableMetadataProxy;
                string key = parameters[1] as string;

                if ((metadata != null) && !String.IsNullOrWhiteSpace(key))
                {
                    metadata[key] = null;
                }
            }
        }

        private void DeleteMetadataCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = true;
            }
        }

        private void RecordToggleCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    if (this.kstudioService.HasRecording)
                    {
                        if (this.kstudioService.RecordingState == KStudioRecordingState.Recording)
                        {
                            this.kstudioService.StopRecording();
                        }
                        else
                        {
                            this.kstudioService.CloseRecording(true);
                        }
                    }
                    else
                    {
                        this.kstudioService.StartRecording();
                    }
                }
            }
        }

        private void RecordToggleCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && (this.kstudioService.HasRecording || this.kstudioService.CanRecord);
            }
        }

        private void TargetToggleCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                if (this.kstudioService != null)
                {
                    if (this.kstudioService.IsTargetConnected)
                    {
                        string currentFileName = this.kstudioService.PlaybackFilePath;
                        bool readOnly = this.kstudioService.IsPlaybackFileReadOnly;

                        this.kstudioService.DisconnectFromTarget();

                        if (!String.IsNullOrWhiteSpace(currentFileName))
                        {
                            this.suppressAutoSwitch = true;
                            this.kstudioService.OpenLocalPlayback(currentFileName, readOnly);
                        }
                    }
                    else
                    {
                        string currentFileName = this.kstudioService.PlaybackFilePath;
                        bool readOnly = this.kstudioService.IsPlaybackFileReadOnly;

                        this.kstudioService.ConnectToTarget(this.kstudioService.TargetAddress, this.kstudioService.TargetAlias);

                        if (!String.IsNullOrWhiteSpace(currentFileName))
                        {
                            this.suppressAutoSwitch = true;
                            this.kstudioService.ClosePlayback();
                            this.kstudioService.OpenTargetPlayback(currentFileName, readOnly);
                        }
                    }
                }
            }
        }

        private void TargetToggleCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null);
            }
        }

        private void OpenRecentReadOnlyLocalFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                string filePath = e.Parameter as string;

                bool readOnly = true;
                if (e.Parameter != null)
                {
                    bool temp;
                    if (bool.TryParse(e.Parameter as string, out temp))
                    {
                        readOnly = temp;
                    }
                }

                this.OpenFile(filePath, true);
            }
        }

        private void OpenRecentWritableLocalFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            using (WaitCursor waitCursor = new WaitCursor(this))
            {
                string filePath = e.Parameter as string;

                this.OpenFile(filePath, false);
            }
        }

        private void OpenRecentLocalFileCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null);
            }
        }

        private void OpenFile(string filePath, bool readOnly)
        {
            DebugHelper.AssertUIThread();

            this.IsFileTabOpen = false;
            this.suppressAutoSwitch = false;

            if ((this.kstudioService != null) && !String.IsNullOrWhiteSpace(filePath))
            {
                if (this.kstudioService.IsTargetConnected)
                {
                    this.kstudioService.OpenTargetPlayback(filePath, readOnly);
                }
                else
                {
                    this.kstudioService.OpenLocalPlayback(filePath, readOnly);
                }
            }
        }

        private void OpenRecentTargetFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(false);       
        }

        private void OpenRecentTargetFileCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null);
            }
        }

        private void CloseDocumentCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            if (this.kstudioService != null) 
            {
                this.kstudioService.CloseRecording(false);
                this.kstudioService.ClosePlayback();
            }
        }

        private void CloseDocumentCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.kstudioService != null) && 
                    (this.kstudioService.HasRecording || this.kstudioService.HasPlaybackFile);
            }
        }

        private void EditLayoutsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;
            }

            this.IsFileTabOpen = true;

            ToolsUIWindow.EditLayoutsCommand.Execute(null, this);
        }

        private void EditLayoutsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = true;
            }
        }

        private ILoggingService loggingService = null;
        private IUserNotificationService notificationService = null;
        private IKStudioService kstudioService = null;
        private IMetadataViewService metadataViewService = null;
        private IPluginService pluginService = null;
        private bool suppressAutoSwitch = false;

        public IKStudioService KStudioService
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.kstudioService;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public MainWindow(string[] args)
        {
            InitializeWindow();
            InitializeComponent();

            IServiceProvider serviceProvider = ToolsUIApplication.Instance.RootServiceProvider;
            if (serviceProvider != null)
            {
                this.loggingService = serviceProvider.GetService(typeof(ILoggingService)) as ILoggingService;
                this.notificationService = serviceProvider.GetService(typeof(IUserNotificationService)) as IUserNotificationService;
                this.kstudioService = serviceProvider.GetService(typeof(IKStudioService)) as IKStudioService;
                this.metadataViewService = serviceProvider.GetService(typeof(IMetadataViewService)) as IMetadataViewService;
                this.pluginService = serviceProvider.GetService(typeof(IPluginService)) as IPluginService;
            }

            if (this.kstudioService != null)
            {
                MultiBinding titleBinding = new MultiBinding
                    {
                        Converter = new TitleConverter
                            {
                                NoFileString = Strings.WindowTitle_NoFile,
                                ReadOnlyFileFormat = Strings.WindowTitle_ReadOnlyFileFormat,
                                WritableFileFormat = Strings.WindowTitle_WritableFileFormat,
                            },
                        FallbackValue = ToolsUIApplication.Instance.AppTitle,
                    };

                titleBinding.Bindings.Add(new Binding
                    {
                        Source = this.kstudioService,
                        Path = new PropertyPath("RecordingFilePath"),
                    });
                titleBinding.Bindings.Add(new Binding 
                    { 
                        Source = this.kstudioService,
                        Path = new PropertyPath("PlaybackFilePath"),
                    });
                titleBinding.Bindings.Add(new Binding
                    {
                        Source = this.kstudioService,
                        Path = new PropertyPath("IsPlaybackFileReadOnly"),
                    });

                this.SetBinding(Window.TitleProperty, titleBinding);

                this.kstudioService.PlaybackOpened += (s, e) =>
                    {
                        DebugHelper.AssertUIThread();

                        if (!this.suppressAutoSwitch)
                        {
                            this.SwitchToView("PlaybackableStreamsView");
                        }
                    };

                this.kstudioService.Busy += (s, e) =>
                    {
                        DebugHelper.AssertUIThread();
                        if (e != null)
                        {
                            this.IsEnabled = !e.IsBusy;
                        }
                    };
            }

            if ((args != null) && (this.kstudioService != null))
            {
                if (args.Length > 1)
                {
                    bool connect = false;
                    bool readOnly = false;
                    string fileName = null;

                    for (int i = 1; i < args.Length; ++i)
                    {
                        string arg = args[i];
                        if ((arg.Length > 1) && (arg[0] == '-'))
                        {
                            char ch = Char.ToUpperInvariant(arg[1]);

                            switch (ch)
                            {
                                case 'R':
                                    readOnly = true;
                                    break;
                            }
                        }
                        else
                        {
                            fileName = args[i];
                        }
                    }

                    string error = null;
                    string targetAlias = null;
                    IPAddress targetAddress = null;
                    
                    targetAlias = Environment.MachineName;
                    targetAddress = IPAddress.Loopback;

                    this.Loaded += (s, e) =>
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    if (error == null)
                                    {
                                        if (connect)
                                        {
                                            if (targetAddress != null)
                                            {
                                                using (WaitCursor waitCursor = new WaitCursor(this))
                                                {
                                                    if (!this.kstudioService.ConnectToTarget(targetAddress, targetAlias))
                                                    {
                                                        fileName = null;
                                                    }
                                                }
                                            }
                                        }

                                        if (fileName != null)
                                        {
                                            error = String.Format(CultureInfo.CurrentCulture, Strings.Arg_Error_InvalidFileName, fileName);

                                            if (fileName.Length > 2)
                                            {
                                                using (WaitCursor waitCursor = new WaitCursor(this))
                                                {
                                                    this.OpenFile(fileName, readOnly);

                                                    error = null;
                                                }
                                            }
                                        }
                                    }

                                    if (error != null)
                                    {
                                        if (this.notificationService != null)
                                        {
                                            this.notificationService.ShowMessageBox(error, MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
                                        }
                                        else if (this.loggingService != null)
                                        {
                                            this.loggingService.LogLine(error);
                                        }
                                    }

                                    CommandManager.InvalidateRequerySuggested();
                                }
                            ));
                        };
                }
            }
        }
    }
}
