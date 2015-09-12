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

    [ServiceFactory(typeof(IKStudioService))]
    public class KStudioServicesFactory : IServiceFactory, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public object CreateService(Type serviceType, IServiceProvider serviceProvider)
        {
            object value = null;

            if (serviceType == typeof(IKStudioService))
            {
                lock (this)
                {
                    if (this.kstudioService == null)
                    {
                        this.kstudioService = new KStudioService();
                    }
                }

                value = this.kstudioService;
            }

            return value;
        }

        ~KStudioServicesFactory()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.kstudioService != null)
                {
                    this.kstudioService.Dispose();
                    this.kstudioService = null; 
                }
            }
        }

        private KStudioService kstudioService = null;
    }
}
