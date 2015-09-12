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
    using System.Linq;
    using Microsoft.Kinect.Tools;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    public class TargetOpenSaveFileData : KStudioUserState
    {
        public TargetOpenSaveFileData(bool save, KStudioClient client, string targetAlias, string defaultPath, bool showReadOnly)
            :this(save, client, targetAlias, defaultPath, null, showReadOnly)
        {
        }

        public TargetOpenSaveFileData(bool save, KStudioClient client, string targetAlias, string defaultPath, string fileName, bool showReadOnly)
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

            this.save = save;
            this.fileName = fileName;
            this.showReadOnly = showReadOnly;

            string currentPath = null; 
            string fileSpec = null;

            IMostRecentlyUsedService mruService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(IMostRecentlyUsedService)) as IMostRecentlyUsedService;
            if (mruService != null)
            {
                mruService.GetTargetFileDialogSettings(targetAlias, ref currentPath, ref fileSpec, ref this.left, ref this.top, ref this.width, ref this.height, ref this.nameWidth, ref this.dateWidth, ref this.sizeWidth);
            }

            if (fileSpec != null)
            {
                fileSpec = fileSpec.ToUpperInvariant();
            }

            if (String.IsNullOrWhiteSpace(currentPath))
            {
                currentPath = defaultPath;
            }

            if (String.IsNullOrEmpty(fileSpec) || (TargetOpenSaveFileData.fileTypes.FirstOrDefault((ft) => ft.Item1 == fileSpec) == null))
            {
                fileSpec = defaultFileSpec;
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

        public bool IsSaveDialog
        {
            get
            {
                return this.save;
            }
        }

        public bool ShowReadOnly
        {
            get
            {
                return this.showReadOnly;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public IEnumerable<Tuple<string, string>> FileTypes
        {
            get
            {
                return TargetOpenSaveFileData.fileTypes;
            }
        }

        public object SelectedItem
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.selectedItem;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.save)
                {
                    KStudioFileInfo fileInfo = value as KStudioFileInfo;
                    if (fileInfo != null)
                    {
                        this.fileName = fileInfo.FilePath;
                        RaisePropertyChanged("FileName");
                        RaisePropertyChanged("SelectedFileName");
                    }
                }
                else
                {
                    if (this.selectedItem != value)
                    {
                        this.selectedItem = value;
                        RaisePropertyChanged("SelectedItem");
                        RaisePropertyChanged("SelectedFileName");
                    }
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.IO.FileInfo"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "fileInfo")]
        public bool IsValidFileName
        {
            get
            {
                DebugHelper.AssertUIThread();

                IUserNotificationService notificationService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(IUserNotificationService)) as IUserNotificationService;
                ILoggingService loggingService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(ILoggingService)) as ILoggingService;

                bool okay = (this.fileName != null);
                string fixedFileName = null;

                if (okay)
                {
                    fixedFileName = this.fileName.Trim();

                    if (this.fileSpec != allFileSpec)
                    {
                        string fileExt = Path.GetExtension(fixedFileName).ToUpperInvariant();
                        if (fileExt != this.fileSpec.ToUpperInvariant().Substring(1))
                        {
                            fixedFileName += this.fileSpec.Substring(1);
                        }
                    }

                    try
                    {
                        FileInfo fileInfo = new FileInfo(fixedFileName);
                    }
                    catch (Exception)
                    {
                        okay = false;

                        string str = String.Format(CultureInfo.CurrentCulture, Strings.FileOpenSave_Error_InvalidFileName_Format, fixedFileName);

                        if (notificationService != null)
                        {
                            notificationService.ShowMessageBox(str, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                        }
                        else if (loggingService != null)
                        {
                            loggingService.LogLine(str);
                        }
                    }
                }

                if (okay)
                {
                    string upperFileName = fixedFileName.ToUpperInvariant();

                    foreach (object obj in this.currentItems)
                    {
                        bool isFolder = true;
                        string name = obj as string;

                        if (name == null)
                        {
                            isFolder = false;
                            KStudioFileInfo fileInfo = obj as KStudioFileInfo;
                            if (fileInfo != null)
                            {
                                name = fileInfo.FilePath;
                            }
                        }

                        if (name != null)
                        {
                            name = name.Trim().ToUpperInvariant();

                            if (name == upperFileName)
                            {
                                if (isFolder)
                                {
                                    okay = false;

                                    string str = String.Format(CultureInfo.CurrentCulture, Strings.FileOpenSave_Error_InvalidFileName_Format, fixedFileName);
                                    if (notificationService != null)
                                    {
                                        notificationService.ShowMessageBox(str, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.OK);
                                    }
                                    else if (loggingService != null)
                                    {
                                        loggingService.LogLine(str);
                                    }
                                }
                                else
                                {
                                    if (notificationService != null)
                                    {
                                        string str = String.Format(CultureInfo.CurrentCulture, Strings.FileOpenSave_Confirm_Overwrite_Format, fixedFileName);
                                        if (notificationService.ShowMessageBox(str, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.No) != System.Windows.MessageBoxResult.Yes)
                                        {
                                            okay = false;
                                        }
                                    }
                                    else if (loggingService != null)
                                    {
                                        string str = String.Format(CultureInfo.CurrentCulture, Strings.FileOpenSave_Error_InvalidFileName_Format, fixedFileName);
                                        loggingService.LogLine(str);
                                    }
                                }

                                break;
                            }
                        }
                    }
                }

                return okay;
            }
        }

        public string FileName
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.fileName;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.save)
                {
                    if (this.fileName != value)
                    {
                        this.fileName = value;

                        RaisePropertyChanged("FileName");
                    }
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

        public string FileSpec
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.fileSpec;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.fileSpec != value)
                {
                    this.fileSpec = value;
                    RaisePropertyChanged("FileSpec");

                    Reload();
                }
            }
        }

        public string SelectedFileName
        {
            get
            {
                DebugHelper.AssertUIThread();

                string value = null;

                if (this.save)
                {
                    if (this.fileName != null)
                    {
                        value = this.fileName.Trim();

                        value = Path.Combine(this.currentPath, value);

                        if (this.fileSpec != allFileSpec)
                        {
                            string fileExt = Path.GetExtension(value).ToUpperInvariant();
                            if (fileExt != this.fileSpec.ToUpperInvariant().Substring(1))
                            {
                                value += this.fileSpec.Substring(1);
                            }
                        }

                        value = this.drive + value;
                    }
                }
                else
                {
                    KStudioFileInfo fileInfo = this.SelectedItem as KStudioFileInfo;
                    if (fileInfo != null)
                    {
                        value = Path.Combine(this.currentPath, fileInfo.FilePath);
                        value = this.drive + value;
                    }
                }

                return value;
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

        public bool AsReadOnly
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.readOnly;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.readOnly != value)
                {
                    this.readOnly = value;
                    RaisePropertyChanged("AsReadOnly");
                }
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

        public double NameWidth
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.nameWidth;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.nameWidth != value)
                {
                    this.nameWidth = value;
                    RaisePropertyChanged("NameWidth");
                }
            }
        }

        public double DateWidth
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.dateWidth;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.dateWidth != value)
                {
                    this.dateWidth = value;
                    RaisePropertyChanged("DateWidth");
                }
            }
        }

        public double SizeWidth
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.sizeWidth;
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (this.sizeWidth != value)
                {
                    this.sizeWidth = value;
                    RaisePropertyChanged("SizeWidth");
                }
            }
        }

        public void SaveSettings(bool saveAll)
        {
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

            List<object> newItems = new List<object>();

            string newFileSpec = this.drive + Path.Combine(this.currentPath, "*");
            foreach (KStudioFileInfo fileInfo in this.client.GetFileList(newFileSpec, KStudioFileListFlags.Directories))
            {
                newItems.Add(fileInfo.FilePath);
            }

            newFileSpec = this.drive + Path.Combine(this.currentPath, this.fileSpec);
            newItems.AddRange(this.client.GetFileList(newFileSpec, KStudioFileListFlags.None).OrderByDescending(fi => fi.LastWriteUtcFileTime));

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

        private readonly bool save;
        private readonly bool showReadOnly = false;

        private const string xefFileSpec = "*.xef";
        private const string xrfFileSpec = "*.xrf";
        private const string allFileSpec = "*.*";
        private const string defaultFileSpec = xefFileSpec;

        private bool readOnly = false;

        private double left = 10.0;
        private double top  = 10.0;
        private double width = 500.0;
        private double height = 400.0;
        private double nameWidth = 200.0;
        private double dateWidth = 100.0;
        private double sizeWidth = 100.0;

        private KStudioClient client;
        private string targetAlias;
        private string currentPath;
        private string fileSpec;
        private string fileName = String.Empty;
        private string drive = String.Empty;
        private string currentDirectory = null;
        private string parentDirectory = null;
        private List<object> currentItems = null;
        private object selectedItem = null;

        private static Tuple<string, string>[] fileTypes = new Tuple<string, string>[] 
            {
                new Tuple<string, string>(xefFileSpec, String.Format(CultureInfo.CurrentCulture, Strings.FileOpenSave_FileSpec_XefFiles, xefFileSpec)),
                new Tuple<string, string>(xrfFileSpec, String.Format(CultureInfo.CurrentCulture, Strings.FileOpenSave_FileSpec_XrfFiles, xrfFileSpec)),
                new Tuple<string, string>(allFileSpec, String.Format(CultureInfo.CurrentCulture, Strings.FileOpenSave_FileSpec_AllFiles, allFileSpec)),
            };
    }
}
