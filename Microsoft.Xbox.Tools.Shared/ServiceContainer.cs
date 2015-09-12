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
    public class ServiceContainer : IServiceProvider
    {
        private IServiceProvider parentServiceProvider;
        private Dictionary<Type, object> services;

        // Note, this class is a pared-down version of the class of the same name in System.ComponentModel.Design.
        // It exists because we specifically do not want the IServiceContainer interface implemented on the 
        // provider, and don't need the other bells and whistles, etc.  
        public ServiceContainer(IServiceProvider parentServiceProvider)
        {
            this.parentServiceProvider = parentServiceProvider;
            this.services = new Dictionary<Type, object>();
        }

        public void AddService(Type serviceType, object service)
        {
            this.services[serviceType] = service;
        }

        public object GetService(Type serviceType)
        {
            object service = null;

            // See if we have this service locally first.
            if (this.services.TryGetValue(serviceType, out service))
            {
                return service;
            }

            // Then consult our parent if we have one.
            if (this.parentServiceProvider != null)
            {
                return this.parentServiceProvider.GetService(serviceType);
            }

            return null;
        }
    }

}
