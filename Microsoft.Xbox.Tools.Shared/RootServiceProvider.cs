//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Xbox.Tools.Shared
{
    public class RootServiceProvider : IServiceProvider
    {
        private ExtensionManager extensionManager;
        private Dictionary<Type, object> serviceCache;
        private bool creatingService;

        public RootServiceProvider(ExtensionManager extensionManager)
        {
            this.extensionManager = extensionManager;
            this.serviceCache = new Dictionary<Type, object>();
            this.serviceCache[typeof(ExtensionManager)] = extensionManager;
        }

        public void AddService(Type serviceType, object service)
        {
            this.serviceCache[serviceType] = service;
        }

        public object GetService(Type serviceType)
        {
            if (this.creatingService)
            {
                Debug.Fail("Re-entrancy detected in service creation.  Do not attempt to acquire services during your own creation!");
                return null;
            }

            object service = null;

            // Look first in the cache.  The cache will contain any pre-provided
            // services directly placed here at startup time.
            if (this.serviceCache.TryGetValue(serviceType, out service))
                return service;

            // Ask the extension manager to provide this service.  Note that we 
            // store the null value if the service is not available; we won't ask again.
            // Note also that it is not okay for service factories to acquire other services
            // via the service provider at creation time, because dependency order is unknown
            // and it's impossible to prevent circularity.  Thus, we check for re-entrancy
            // during service creation.
            this.creatingService = true;
            try
            {
                service = this.extensionManager.CreateProvidedService(serviceType, this);
            }
            finally
            {
                this.creatingService = false;
            }

            this.serviceCache[serviceType] = service;

            return service;
        }
    }

}
