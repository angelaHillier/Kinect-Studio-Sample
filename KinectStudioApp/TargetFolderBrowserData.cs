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
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    public class TargetFolderBrowserData : KStudioUserState
    {
        public TargetFolderBrowserData(KStudioClient client, string targetAlias, string defaultPath)
        {
            DebugHelper.AssertUIThread();

            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            if (String.IsNullOrWhiteSpace(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            if (String.IsNullOrWhiteSpace(defaultPath))
            {
                throw new ArgumentNullException("defaultPath");
            }

            string currentPath = null; 
            string fileSpec = null;

            if (fileSpec != null)
            {
                fileSpec = fileSpec.ToUpperInvariant();
            }

            if (String.IsNullOrWhiteSpace(currentPath))
            {
                currentPath = defaultPath;
            }

            int i = currentPath.IndexOf(Path.VolumeSeparatorChar);
            if (i >= 0)
            {
                this.drive = currentPath.Substring(0, i + 1);
                currentPath = currentPath.Substring(i + 1);
            }
            else
            {
                this.drive = String.Empty;
            }

            if (!currentPath.StartsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                currentPath = Path.DirectorySeparatorChar.ToString() + currentPath;
            }

            this.client = client;
            this.targetAlias = targetAlias;
            this.currentPath = currentPath;
            this.fileSpec = fileSpec;

            Reload();
        }

        public string SelectedItem
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.selectedItem;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.selectedItem != value)
                {
                    this.selectedItem = value;
                    this.RaisePropertyChanged("SelectedItem");
                }
            }
        }

        public string CurrentPath
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.drive + this.currentPath;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.currentPath != value)
                {
                    this.currentPath = value;

                    RaisePropertyChanged("CurrentPath");

                    Reload();
                }
            }
        }

        public string CurrentDirectory
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.currentDirectory;
            }
        }

        public string ParentDirectory
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.parentDirectory;
            }
        }

        public IEnumerable<object> Items
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.currentItems;
            }
        }

        public double Left
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.left;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.left != value)
                {
                    this.left = value;
                    RaisePropertyChanged("Left");
                }
            }
        }

        public double Top
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.top;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.top != value)
                {
                    this.top = value;
                    RaisePropertyChanged("Top");
                }
            }
        }

        public double Width
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.width;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.width != value)
                {
                    this.width = value;
                    RaisePropertyChanged("Width");
                }
            }
        }

        public double Height
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.height;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.height != value)
                {
                    this.height = value;
                    RaisePropertyChanged("Height");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "saveAll")]
        public void SaveSettings(bool saveAll)
        {
/*
            IMostRecentlyUsedService mruService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(IMostRecentlyUsedService)) as IMostRecentlyUsedService;
            if (mruService != null)
            {
                if (saveAll)
                {
                    mruService.SetTargetFileDialogSettings(this.targetAlias, this.drive + this.currentPath, this.fileSpec, this.left, this.top, this.width, this.height, this.nameWidth, this.dateWidth, this.sizeWidth);
                }
                else
                {
                    mruService.SetTargetFileDialogSettings(this.targetAlias, this.left, this.top, this.width, this.height, this.nameWidth, this.dateWidth, this.sizeWidth);
                }
            }
 */
        }

        public void GoUp()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.currentPath != null);

            string newPath = Path.GetDirectoryName(this.currentPath);
            if (!String.IsNullOrWhiteSpace(newPath))
            {
                this.CurrentPath = newPath;
            }
        }

        public void Open(string directoryName)
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.currentPath != null);

            string openDirectory = Path.Combine(this.currentPath, directoryName);

            this.CurrentPath = openDirectory;
        }

        public void Reload()
        {
            DebugHelper.AssertUIThread();
            Debug.Assert(this.client != null);
            Debug.Assert(this.currentPath != null);

            this.SelectedItem = null;

            string newCurrentDirectory = Path.GetFileName(this.currentPath);
            string newParentDirectory = null;

            if (String.IsNullOrWhiteSpace(newCurrentDirectory))
            {
                newCurrentDirectory = GetDriveName();
            }
            else
            {
                newParentDirectory = Path.GetDirectoryName(this.currentPath);
                if (newParentDirectory != null)
                {
                    newParentDirectory = Path.GetFileName(newParentDirectory);

                    if (String.IsNullOrWhiteSpace(newParentDirectory))
                    {
                        newParentDirectory = GetDriveName();
                    }
                }
            }

            if (this.currentDirectory != newCurrentDirectory)
            {
                this.currentDirectory = newCurrentDirectory;
                RaisePropertyChanged("CurrentDirectory");
            }

            if (this.parentDirectory != newParentDirectory)
            {
                this.parentDirectory = newParentDirectory;
                RaisePropertyChanged("ParentDirectory");
            }

            List<string> newItems = new List<string>();

            string newFileSpec = this.drive + Path.Combine(this.currentPath, "*");
            foreach (KStudioFileInfo fileInfo in this.client.GetFileList(newFileSpec, KStudioFileListFlags.Directories))
            {
                newItems.Add(fileInfo.FilePath);
            }

            this.currentItems = newItems;
            this.RaisePropertyChanged("Items");
        }

        private string GetDriveName()
        {
            string value;

            if (String.IsNullOrWhiteSpace(this.drive))
            {
                value = Strings.FileOpenSave_Root;
            }
            else
            {
                value = String.Format(CultureInfo.CurrentCulture, Strings.FileOpenSave_Drive_Format, this.drive);
            }

            return value;
        }

        private double left = 10.0;
        private double top  = 10.0;
        private double width = 500.0;
        private double height = 400.0;

        private KStudioClient client;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private string targetAlias;
        private string currentPath;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private string fileSpec;
        private string drive = String.Empty;
        private string currentDirectory = null;
        private string parentDirectory = null;
        private List<string> currentItems = null;
        private string selectedItem = null;
    }
}
