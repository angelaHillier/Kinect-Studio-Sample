//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared
{
    public partial class StatusDialog : DialogBase
    {
        static StatusDialog instance;

        BackgroundRequest request;
        IReportProgress reportProgress;
        bool canceled;
        Queue<MessageNode> additionalMessages;
        Queue<Action> postActions;
        MessageNode currentMessage;
        DispatcherTimer timer;
        string originalMessage;
        bool ignoreClose;

        // NOTE:  These properties are for test automation/control purposes
        public static bool AutoDismiss { get; set; }        // Prevents the dialog from staying up waiting for user input on error
        public static HResult Result { get; set; }          // Holds the final result of the request (or the error result that would be displayed)

        public ErrorStatus ErrorStatus { get; private set; }

        private StatusDialog(Window owner, string title, BackgroundRequest request, HResult hr, string message, string errorPreamble, Action postAction)
        {
            instance = this;

            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.Owner = owner;
            this.Title = title;
            this.ErrorStatus = new ErrorStatus();
            this.ErrorStatus.Preamble = message;
            this.ErrorStatus.SetStatus(ErrorSeverity.Info, null, null);

            this.request = request;
            this.reportProgress = request as IReportProgress;
            this.DataContext = this;
            this.originalMessage = message;

            this.currentMessage = new MessageNode
            {
                Preamble = errorPreamble ?? StringResources.UnknownErrorOccurred,
                PostAction = postAction
            };

            InitializeComponent();

            if (request != null)
            {
                request.Dispatched += OnRequestDispatched;

                if (reportProgress != null)
                {
                    reportProgress.Progress += OnProgressReport;
                }

                if (request.IsDispatchComplete)
                {
                    // We were asked to wait on a request that has already been dispatched.  Simulate notification
                    // of the dispatch.  Can't call it directly because Close() right now is bad news.
                    this.Dispatcher.BeginInvoke((Action)(() => { this.OnRequestDispatched(request, EventArgs.Empty); }), DispatcherPriority.Background);
                }
            }
            else
            {
                SwitchToError(hr, false);
            }

            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopyExecuted));
        }

        public static void DisplayWait(Window owner, string title, BackgroundRequest request, string message, string errorPreamble)
        {
            // Don't call this if there's already one in flight!
            Debug.Assert(instance == null);

            if (instance == null)
            {
                new StatusDialog(owner, title, request, HResult.S_OK, message, errorPreamble, null).ShowDialog();
            }
        }

        private static void DisplayMessageInternal(Window owner, string title, HResult hr, bool isTheHresultAWarning, string errorPreamble, Action postAction)
        {
            if (instance == null)
            {
                var dialog = new StatusDialog(owner, title, null, hr, null, errorPreamble, postAction);
                if (isTheHresultAWarning)
                {
                    dialog.SwitchToError(hr, true);
                }
                dialog.ShowDialog();
            }
            else
            {
                if (instance.additionalMessages == null)
                {
                    instance.additionalMessages = new Queue<MessageNode>();
                }

                var newMessage = new MessageNode
                {
                    Preamble = errorPreamble ?? StringResources.UnknownErrorOccurred,
                    Result = hr,
                    PostAction = postAction,
                    IsWarningMessage = isTheHresultAWarning,
                };
                instance.additionalMessages.Enqueue(newMessage);
            }
        }

        public static void DisplayMessage(Window owner, string title, HResult hr, string errorPreamble, Action postAction)
        {
            DisplayMessageInternal(owner, title, hr, false, errorPreamble, postAction);
        }

        public static void DisplayWarningMessage(Window owner, string title, HResult hr, string errorPreamble, Action postAction)
        {
            DisplayMessageInternal(owner, title, hr, true, errorPreamble, postAction);
        }

        void OnProgressReport(object sender, ProgressEventArgs e)
        {
            if (this.canceled)
            {
                // Notify the ongoing opreation (usually file copy) to abort,
                // if cancel button has been clicked.
                e.Cancel = true;
            }
            else
            {
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.ErrorStatus.Preamble = e.Message == null ? this.originalMessage : e.Message;

                    if (e.TotalSize == null || e.SizeSoFar == e.TotalSize.Value)
                    {
                        this.progressBar.IsIndeterminate = true;
                    }
                    else
                    {
                        this.progressBar.IsIndeterminate = false;
                        this.progressBar.Minimum = 0;
                        this.progressBar.Maximum = e.TotalSize.Value;
                        this.progressBar.Value = e.SizeSoFar;
                    }
                }), null);
            }
        }

        void OnRequestDispatched(object sender, EventArgs e)
        {
            HResult hr = request.Result;

            request.Dispatched -= OnRequestDispatched;
            request = null;

            if (hr.Failed && hr != HResult.E_REQUEST_CANCELED && hr != HResult.E_REQUIRES_PROFILING_MODE)
            {
                SwitchToError(hr, false);
            }
            else
            {
                this.ignoreClose = false;
                this.Close();
            }
        }

        public void SwitchToError(HResult hr, bool isWarning)
        {
            this.ErrorStatus.Preamble = this.currentMessage.Preamble;
            if (isWarning)
            {
                this.ErrorStatus.SetStatus(ErrorSeverity.Warning, hr.DetailedMessage, hr.ErrorCodeAsString);
            }
            else
            {
                this.ErrorStatus.SetErrorCode(hr);
            }
            this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
            this.cancelButton.Content = StringResources.CloseButtonText;
            this.canceled = true;
            this.ProgressiveDisclosure = true;

            if (AutoDismiss)
            {
                Result = hr;
                this.Dispatcher.BeginInvoke((Action)(() => { this.Close(); }), DispatcherPriority.Background);
            }
        }

        void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("[Title]\r\n{0}\r\n[Preamble]\r\n{1}\r\n", this.Title, this.ErrorStatus.Preamble);

            if (!string.IsNullOrEmpty(this.ErrorStatus.Description))
            {
                sb.AppendFormat("[Description]\r\n{0}\r\n", this.ErrorStatus.Description);
            }

            sb.AppendFormat("[Error Code]\r\n{0}\r\n[{1}]", this.ErrorStatus.Code, this.cancelButton.Content);

            WpfUtilities.SetClipboardText(sb.ToString());
            e.Handled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            instance = null;
            if (reportProgress != null)
            {
                reportProgress.Progress -= OnProgressReport;
                reportProgress = null;
            }

            base.OnClosed(e);
        }

        void IgnoreExtraCloseAttempts()
        {
            this.ignoreClose = true;
            this.Dispatcher.BeginInvoke((Action)(() => { this.ignoreClose = false; }), DispatcherPriority.Background, null);
        }

        void OnCancelTimerTick(object sender, EventArgs e)
        {
            this.timer.Stop();
            this.Footnote = StringResources.RequestIgnoringCancel;
            this.cancelButton.Content = StringResources.ForceButtonText;
            this.cancelButton.IsCancel = false; // not a cancel button anymore, since pressing ESC should not force termination
            this.cancelButton.IsEnabled = true;
            this.cancelButton.Click += OnCancelButtonClicked;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (ignoreClose)
            {
                // Since Close/Cancel button is marked IsCancel, we can get TWO
                // attempts to close from WPF.
                e.Cancel = true;
                return;
            }

            this.Footnote = null;
            if (!canceled)
            {
                if (request != null)
                {
                    request.Cancel();
                    canceled = true;
                    e.Cancel = true;
                    this.progressBar.Foreground = Brushes.Red;
                    this.cancelButton.IsEnabled = false;
                    this.cancelButton.Content = StringResources.CancellingButtonText;
                    this.timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnCancelTimerTick, this.Dispatcher);
                    this.timer.Start();
                    IgnoreExtraCloseAttempts();
                }
            }
            else if (request != null && !request.IsDispatchComplete)
            {
                // Don't allow closing until the cancellation is dispatched
                e.Cancel = true;

                if (request.Processor != null)
                {
                    request.Processor.Shutdown();
                }
                IgnoreExtraCloseAttempts();
            }
            else
            {
                if (this.currentMessage.PostAction != null)
                {
                    if (this.postActions == null)
                        this.postActions = new Queue<Action>();

                    this.postActions.Enqueue(this.currentMessage.PostAction);
                }

                if (this.additionalMessages != null && this.additionalMessages.Count > 0)
                {
                    this.currentMessage = this.additionalMessages.Dequeue();
                    SwitchToError(this.currentMessage.Result, this.currentMessage.IsWarningMessage);
                    e.Cancel = true;
                    IgnoreExtraCloseAttempts();
                }
                else
                {
                    if (this.postActions != null)
                    {
                        while (this.postActions.Count > 0)
                            this.postActions.Dequeue()();
                    }
                }
            }
        }

        void OnCancelButtonClicked(object sender, RoutedEventArgs e)
        {
            // Clicking cancel needs to be the same thing as Alt+F4, or clicking the red X, etc.
            Close();
        }

        struct MessageNode
        {
            public string Preamble { get; set; }
            public HResult Result { get; set; }
            public Action PostAction { get; set; }
            public bool IsWarningMessage { get; set; }
        }
    }
}
