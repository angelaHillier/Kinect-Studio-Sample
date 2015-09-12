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
    using KinectStudioPlugin;
    using KinectStudioUtility;

    [ServiceFactory(typeof(IPluginService))]
    public class PluginServiceFactory : IServiceFactory, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public object CreateService(Type serviceType, IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            object value = null;

            if (serviceType == typeof(IPluginService))
            {
                if (this.pluginService == null)
                {
                    this.pluginService = new PluginService(serviceProvider);
                }

                value = this.pluginService;
            }

            return value;
        }

        ~PluginServiceFactory()
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
                IDisposable disposable = this.pluginService as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }

                this.pluginService = null;
            }
        }

        private IPluginService pluginService = null;
    }
}
