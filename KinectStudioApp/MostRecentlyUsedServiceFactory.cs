//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using KinectStudioUtility;
    using Microsoft.Xbox.Tools.Shared;
    using System;

    [ServiceFactory(typeof(IMostRecentlyUsedService))]
    public class MostRecentlyUsedServiceFactory : IServiceFactory, IDisposable
    {
        ~MostRecentlyUsedServiceFactory()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public object CreateService(Type serviceType, IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            object value = null;

            if (serviceType == typeof(IMostRecentlyUsedService))
            {
                if (this.mruService == null)
                {
                    this.mruService = new MostRecentlyUsedService();
                }

                value = this.mruService;
            }

            return value;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                DebugHelper.AssertUIThread();

                if (this.mruService != null)
                {
                    this.mruService.Dispose();
                    this.mruService = null;
                }
            }
        }

        private MostRecentlyUsedService mruService = null;
    }
}
