//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace Microsoft.Xbox.Tools.Shared
{
    public class SimpleNotificationService : IUserNotificationService
    {
        string appTitle;

        public SimpleNotificationService(string appTitle)
        {
            this.appTitle = appTitle;
        }

        public MessageBoxResult ShowMessageBox(string message)
        {
            return MessageBox.Show(message, appTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public MessageBoxResult ShowMessageBox(string message, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult defaultResult)
        {
            return MessageBox.Show(message, appTitle, buttons, image, defaultResult);
        }

        public MessageBoxResult ShowMessageBox(Window owner, string message, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult defaultResult)
        {
            return MessageBox.Show(owner, message, appTitle, buttons, image, defaultResult);
        }

        public void ShowError(HResult hr)
        {
            ShowError(null, hr);
        }

        public void ShowError(string errorText)
        {
            ShowError(HResult.FromErrorText(errorText));
        }

        public void ShowError(string errorPreamble, HResult hr)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(errorPreamble))
            {
                sb.AppendLine(errorPreamble);
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(hr.DetailedMessage))
            {
                sb.Append(hr.DetailedMessage);
            }
            else
            {
                sb.AppendLine(StringResources.UnknownErrorOccurred);
                sb.Append(hr.ErrorCodeAsString);
            }

            ShowMessageBox(sb.ToString());
        }
    }
}