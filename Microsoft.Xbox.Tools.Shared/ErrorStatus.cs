//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Windows;

namespace Microsoft.Xbox.Tools.Shared
{
    public enum ErrorSeverity
    {
        None,
        Info,
        Warning,
        Error
    }

    public class ErrorStatus : DependencyObject
    {
        static readonly DependencyPropertyKey codePropertyKey = DependencyProperty.RegisterReadOnly(
            "Code", typeof(string), typeof(ErrorStatus), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty CodeProperty = codePropertyKey.DependencyProperty;

        static readonly DependencyPropertyKey descriptionPropertyKey = DependencyProperty.RegisterReadOnly(
            "Description", typeof(string), typeof(ErrorStatus), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty DescriptionProperty = descriptionPropertyKey.DependencyProperty;

        public static readonly DependencyProperty PreambleProperty = DependencyProperty.Register(
            "Preamble", typeof(string), typeof(ErrorStatus));

        static readonly DependencyPropertyKey severityPropertyKey = DependencyProperty.RegisterReadOnly(
            "Severity", typeof(ErrorSeverity), typeof(ErrorStatus), new FrameworkPropertyMetadata(ErrorSeverity.None));
        public static readonly DependencyProperty SeverityProperty = severityPropertyKey.DependencyProperty;

        public string Code
        {
            get { return (string)GetValue(CodeProperty); }
            private set { SetValue(codePropertyKey, value); }
        }
        
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            private set { SetValue(descriptionPropertyKey, value); }
        }

        public string Preamble
        {
            get { return (string)GetValue(PreambleProperty); }
            set { SetValue(PreambleProperty, value); }
        }

        public ErrorSeverity Severity
        {
            get { return (ErrorSeverity)GetValue(SeverityProperty); }
            private set { SetValue(severityPropertyKey, value); }
        }

        public ErrorStatus()
        {
        }

        public ErrorStatus(ErrorSeverity severity, string description, string code)
        {
            this.SetStatus(severity, description, code);
        }

        public ErrorStatus(HResult hr)
        {
            this.SetErrorCode(hr);
        }

        public void ClearStatus()
        {
            this.Severity = ErrorSeverity.None;
            this.Code = null;
            this.Description = null;
        }

        public void SetErrorCode(HResult hr)
        {
            if (hr.Succeeded)
            {
                ClearStatus();
            }
            else
            {
                this.Severity = ErrorSeverity.Error;
                this.Code = hr.ErrorCodeAsString;
                this.Description = hr.DetailedMessage;
            }
        }

        public void SetStatus(ErrorSeverity severity, string description, string code)
        {
            this.Severity = severity;
            this.Description = description;
            this.Code = code;
        }
    }
}
