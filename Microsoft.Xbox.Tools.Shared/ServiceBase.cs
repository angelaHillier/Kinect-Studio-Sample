//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public abstract class ServiceBase
    {
        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Helper method to create service instances. Typically used to populate a field variable with
        /// an instance of a given service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <returns></returns>
        protected T EnsureService<T>(ref T service) where T : class
        {
            if (service == null)
            {
                service = this.ServiceProvider.GetService(typeof(T)) as T;
                Debug.Assert(service != null);
            }

            return service;
        }

        protected ServiceBase(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }
    }
}
