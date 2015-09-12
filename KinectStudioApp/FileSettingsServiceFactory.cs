//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    [ServiceFactory(typeof(IFileSettingsService))]
    public class FileSettingsServiceFactory : IServiceFactory
    {
        public object CreateService(Type serviceType, IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            object value = null;

            if (serviceType == typeof(IFileSettingsService))
            {
                if (this.fileSettingsService == null)
                {
                    this.fileSettingsService = new FileSettingsService();
                }

                value = this.fileSettingsService;
            }

            return value;
        }

        private IFileSettingsService fileSettingsService = null;
    }
}
