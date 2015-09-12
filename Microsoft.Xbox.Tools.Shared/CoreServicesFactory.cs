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
    [ServiceFactory(typeof(ILoggingService))]
    [ServiceFactory(typeof(ViewManager))]
    [ServiceFactory(typeof(DocumentManager))]
    [ServiceFactory(typeof(RecentDocumentService))]
    public class CoreServicesFactory : IServiceFactory
    {
        private Dictionary<Type, Func<IServiceProvider, object>> serviceCreators = new Dictionary<Type, Func<IServiceProvider, object>>
        {
            { typeof(ILoggingService), (sp) => new LoggingService() },
            { typeof(ViewManager), (sp) => new ViewManager() },
            { typeof(DocumentManager), (sp) => new DocumentManager(sp) },
            { typeof(RecentDocumentService), (sp) => new RecentDocumentService(sp) },
        };

        public object CreateService(Type serviceType, IServiceProvider serviceProvider)
        {
            Func<IServiceProvider, object> creator;

            if (this.serviceCreators.TryGetValue(serviceType, out creator))
                return creator(serviceProvider);

            Debug.Fail("Unknown service requested of CoreServicesFactory.  Huh?");
            return null;
        }
    }
}
