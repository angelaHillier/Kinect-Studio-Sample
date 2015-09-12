//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Windows;

namespace Microsoft.Xbox.Tools.Shared
{
    public interface IUserNotificationService
    {
        MessageBoxResult ShowMessageBox(string message);
        MessageBoxResult ShowMessageBox(string message, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult defaultResult);
        MessageBoxResult ShowMessageBox(Window owner, string message, MessageBoxButton buttons, MessageBoxImage image, MessageBoxResult defaultResult);
        void ShowError(HResult hr);
        void ShowError(string errorPreamble, HResult hr);
        void ShowError(string errorText);
    }
}
