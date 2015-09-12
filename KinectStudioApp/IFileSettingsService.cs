//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;

    public interface IFileSettingsService
    {
        void LoadSettings(KStudioClipSource clipSource);

        void UnloadSettings(KStudioClipSource clipSource);

        XElement GetSettings(KStudioClipSource clipSource, string settingsKey);

        void LoadSettings(string targetAlias, KStudioClipSource clipSource);

        void UnloadSettings(string targetAlias, KStudioClipSource clipSource);

        XElement GetSettings(string targetAlias, KStudioClipSource clipSource, string settingsKey);
    }
}
