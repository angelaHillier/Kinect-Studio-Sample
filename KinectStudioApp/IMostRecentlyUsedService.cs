//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System.Collections.Generic;
    using Microsoft.Xbox.Tools.Shared;

    public interface IMostRecentlyUsedService
    {
        IEnumerable<OpenTabItemData> OpenReadOnlyFileTabControls { get; }
        IEnumerable<OpenTabItemData> OpenWritableFileTabControls { get; }

        void AddMostRecentlyUsedLocalFile(string filePath);
        void AddMostRecentlyUsedTargetFile(string targetAlias, string filePath);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#")]
        void GetLocalFileDialogSettings(ref string lastBrowsePath, ref string lastBrowseSpec);

        void SetLocalFileDialogSettings(string lastBrowsePath, string lastBrowseSpec);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "5#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "8#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "7#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "6#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "4#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "9#")]
        void GetTargetFileDialogSettings(string targetAlias, ref string lastBrowsePath, ref string lastBrowseSpec, ref double left, ref double top, ref double width, ref double height, ref double nameWidth, ref double dateWidth, ref double sizeWidth);

        void SetTargetFileDialogSettings(string targetAlias, string lastBrowsePath, string lastBrowseSpec, double left, double top, double width, double height, double nameWidth, double dateWidth, double sizeWidth);
        void SetTargetFileDialogSettings(string targetAlias, double left, double top, double width, double height, double nameWidth, double dateWidth, double sizeWidth);
    }
}
