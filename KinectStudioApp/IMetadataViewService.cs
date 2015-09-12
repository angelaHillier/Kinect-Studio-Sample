//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using Microsoft.Xbox.Tools.Shared;

    public interface IMetadataViewService
    {
        IEnumerable<MetadataView> GetMetadataViews(Window window);

        View CreateView(IServiceProvider serviceProvider);

        void CloseMetadataViews(ISet<MetadataInfo> metadataViewsToClose);

        void UpdateMetadataControls();

        string GetUniqueTitle(MetadataView metadataViewIgnore);
    }
}
