//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Windows;

    public interface IMetadataPlugin
    {
        IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> FileReadOnlyDataTemplates { get; }
        IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> FileWritableDataTemplates { get; }

        IReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate> StreamReadOnlyDataTemplates { get; }
        IReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate> StreamWritableDataTemplates { get; }
    }
}
