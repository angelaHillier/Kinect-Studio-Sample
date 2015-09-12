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

    [ServiceFactory(typeof(IMetadataViewService))]
    public class MetadataViewFactory : IServiceFactory
    {
        public object CreateService(Type serviceType, IServiceProvider serviceProvider)
        {
            object value = null;

            if (serviceType == typeof(IMetadataViewService))
            {
                lock (this)
                {
                    if (this.metadataViewService == null)
                    {
                        this.metadataViewService = new MetadataViewService();
                    }
                }

                value = this.metadataViewService;
            }

            return value;
        }

        private IMetadataViewService metadataViewService = null;
    }
}
