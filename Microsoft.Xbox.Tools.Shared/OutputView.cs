//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class OutputView : TextBox
    {
        public static readonly DependencyProperty IsOutputViewProperty = DependencyProperty.RegisterAttached(
            "IsOutputView", typeof(bool), typeof(OutputView), new FrameworkPropertyMetadata(false));

        private const int MaxBufferSize = 32768;

        private IServiceProvider serviceProvider;
        private ILoggingService loggingService;

        public OutputView(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            SetIsOutputView(this, true);
            View.SetIsInitiallyFocused(this, true);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            //this.Style = this.TryFindResource(typeof(TextBox)) as Style;
            this.loggingService = (ILoggingService)this.serviceProvider.GetService(typeof(ILoggingService));

            if (this.loggingService == null)
            {
                this.Text = "ERROR:  Unable to get logging service; no output available.";
            }
            else
            {
                // Every output view gets its own text (and starts with what has accumulated so far, so they're all equal).
                this.Text = this.loggingService.AccumulatedLog;
                if (this.IsLoaded)
                {
                    OnLoaded(this, null);
                }
                else
                {
                    this.Loaded += OnLoaded;
                }

                this.loggingService.MessageLogged += OnMessageLogged;
            }
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.CaretIndex = this.Text.Length;
            this.ScrollToEnd();
            this.Loaded -= OnLoaded;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        void OnMessageLogged(object sender, LogEventArgs e)
        {
            if (!CheckAccess())
            {
                this.Dispatcher.BeginInvoke((Action)(() => LogMessage(e)));
            }
            else
            {
                LogMessage(e);
            }
        }

        void LogMessage(LogEventArgs e)
        {
            int caret = this.CaretIndex;
            int selectionStart = this.SelectionStart;
            int selectionLength = this.SelectionLength;
            bool scroll = (caret == this.Text.Length && selectionLength == 0);
            var newText = e.Message;

            if (newText.Length > MaxBufferSize)
            {
                this.Text = newText.Substring(newText.Length - MaxBufferSize);
                scroll = true;
            }
            else if (newText.Length + this.Text.Length > MaxBufferSize)
            {
                int keep = Math.Min(MaxBufferSize - newText.Length, this.Text.Length);
                int lost = this.Text.Length - keep;

                this.Text = this.Text.Substring(lost) + newText;
                if (lost >= caret && lost >= selectionStart)
                {
                    scroll = true;
                }
                else
                {
                    if (selectionLength > 0)
                    {
                        selectionStart -= lost;
                    }

                    caret -= lost;
                }
            }
            else
            {
                this.Text = this.Text + newText;
            }

            if (scroll)
            {
                this.CaretIndex = this.Text.Length;
                this.ScrollToEnd();
            }
            else
            {
                this.CaretIndex = caret;
                if (selectionLength > 0)
                {
                    this.SelectionStart = selectionStart;
                    this.SelectionLength = selectionLength;
                }
            }
        }

        public static bool GetIsOutputView(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsOutputViewProperty);
        }

        public static void SetIsOutputView(DependencyObject obj, bool value)
        {
            obj.SetValue(IsOutputViewProperty, value);
        }
    }

    [ViewFactory("Microsoft.Xbox.Tools.Shared.OutputView")]
    public class OutputViewFactory : IViewFactory
    {
        public string GetViewDisplayName(string registeredViewName)
        {
            return "Output";
        }

        public object CreateView(string registeredViewName, IServiceProvider serviceProvider)
        {
            return new OutputView(serviceProvider);
        }
    }

}
