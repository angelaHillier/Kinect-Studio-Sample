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
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Xml;
    using Microsoft.Kinect.Tools;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    public class MostRecentlyUsedService : KStudioUserState, IMostRecentlyUsedService, IDisposable
    {
        public MostRecentlyUsedService()
        {
        }

        ~MostRecentlyUsedService()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public void AddMostRecentlyUsedLocalFile(string filePath)
        {
            DebugHelper.AssertUIThread();

            if (String.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            lock (this.lockObj)
            {
                Init();

                if (this.localComputer != null)
                {
                    this.localComputer.Add(filePath);
                }
            }
        }

        public void AddMostRecentlyUsedTargetFile(string targetAlias, string filePath)
        {
            DebugHelper.AssertUIThread();

            if (String.IsNullOrWhiteSpace(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            if (String.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            lock (this.lockObj)
            {
                Init();

                if (this.targets != null)
                {
                    TargetMostRecentlyUsedState mruState;
                    if (this.targets.TryGetValue(targetAlias, out mruState))
                    {
                        mruState.Add(filePath);
                    }
                }
            }
        }

        public void GetLocalFileDialogSettings(ref string lastBrowsePath, ref string lastBrowseSpec)
        {
            lock (this.lockObj)
            {
                Init();

                if (!String.IsNullOrWhiteSpace(this.localComputer.LastBrowsePath))
                {
                    lastBrowsePath = this.localComputer.LastBrowsePath;
                }

                if (!String.IsNullOrWhiteSpace(this.localComputer.LastBrowseSpec))
                {
                    lastBrowseSpec = this.localComputer.LastBrowseSpec;
                }
            }
        }

        public void SetLocalFileDialogSettings(string lastBrowsePath, string lastBrowseSpec)
        {
            lock (this.lockObj)
            {
                Init();

                this.localComputer.LastBrowsePath = lastBrowsePath;
                this.localComputer.LastBrowseSpec = lastBrowseSpec;
            }
        }

        public void GetTargetFileDialogSettings(string targetAlias, ref string lastBrowsePath, ref string lastBrowseSpec, ref double left, ref double top, ref double width, ref double height, ref double nameWidth, ref double dateWidth, ref double sizeWidth)
        {
            if (String.IsNullOrWhiteSpace(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            lock (this.lockObj)
            {
                Init();

                TargetMostRecentlyUsedState mruState;
                if (targets.TryGetValue(targetAlias, out mruState))
                {
                    if (!String.IsNullOrWhiteSpace(mruState.LastBrowsePath))
                    {
                        lastBrowsePath = mruState.LastBrowsePath;
                    }
                    if (!String.IsNullOrWhiteSpace(mruState.LastBrowseSpec))
                    {
                        lastBrowseSpec = mruState.LastBrowseSpec;
                    }
                    if (mruState.Left > 0.0)
                    {
                        left = mruState.Left;
                    }
                    if (mruState.Top > 0.0)
                    {
                        top = mruState.Top;
                    }
                    if (mruState.Height > 0.0)
                    {
                        height = mruState.Height;
                    }
                    if (mruState.Width > 0.0)
                    {
                        width = mruState.Width;
                    }
                    if (mruState.Height > 0.0)
                    {
                        height = mruState.Height;
                    }
                    if (mruState.NameWidth > 0.0)
                    {
                        nameWidth = mruState.NameWidth;
                    }
                    if (mruState.DateWidth > 0.0)
                    {
                        dateWidth = mruState.DateWidth;
                    }
                    if (mruState.SizeWidth > 0.0)
                    {
                        sizeWidth = mruState.SizeWidth;
                    }
                }
            }
        }

        public void SetTargetFileDialogSettings(string targetAlias, string lastBrowsePath, string lastBrowseSpec, double left, double top, double width, double height, double nameWidth, double dateWidth, double sizeWidth)
        {
            DebugHelper.AssertUIThread();

            if (String.IsNullOrWhiteSpace(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            lock (this.lockObj)
            {
                Init();

                TargetMostRecentlyUsedState mruState;
                if (targets.TryGetValue(targetAlias, out mruState))
                {
                    mruState.LastBrowsePath = lastBrowsePath;
                    mruState.LastBrowseSpec = lastBrowseSpec;
                    mruState.Left = left;
                    mruState.Top = top;
                    mruState.Width = width;
                    mruState.Height = height;
                    mruState.NameWidth = nameWidth;
                    mruState.DateWidth = dateWidth;
                    mruState.SizeWidth = sizeWidth;
                }
            }
        }

        public void SetTargetFileDialogSettings(string targetAlias, double left, double top, double width, double height, double nameWidth, double dateWidth, double sizeWidth)
        {
            DebugHelper.AssertUIThread();

            if (String.IsNullOrWhiteSpace(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            lock (this.lockObj)
            {
                Init();

                TargetMostRecentlyUsedState mruState;
                if (targets.TryGetValue(targetAlias, out mruState))
                {
                    mruState.Left = left;
                    mruState.Top = top;
                    mruState.Width = width;
                    mruState.Height = height;
                    mruState.NameWidth = nameWidth;
                    mruState.DateWidth = dateWidth;
                    mruState.SizeWidth = sizeWidth;
                }
            }
        }

        private void Dispose(bool disposing)
        {
            lock (this.lockObj)
            {
                if (disposing)
                {
                }
            }
        }

        // should be locked
        private void Init()
        {
            DebugHelper.AssertUIThread();

            if (this.localComputer == null)
            {
                this.localComputer = new MostRecentlyUsedState();
                this.sessionStateService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(ISessionStateService)) as ISessionStateService;

                if (this.sessionStateService != null)
                {
                    this.sessionStateService.DeclareSessionStateVariable("MostRecentlyUsed-LocalComputer", this.localComputer);

                    string alias = Environment.MachineName;
                    TargetMostRecentlyUsedState mruState = new TargetMostRecentlyUsedState(alias);
                    this.targets[alias] = mruState;
                    this.sessionStateService.DeclareSessionStateVariable("MostRecentlyUsed-Target-" + mruState.Id, mruState);
                }

                this.openReadOnlyFileTabControls.Add(new OpenTabItemData()
                    {
                        SubMode = "ComputerReadOnly",
                        Header = Strings.SingleBoxReadOnlyDevelopmentComputer_Label,
                        Shortcut = Strings.SingleBoxReadOnlyDevelopmentComputer_Shortcut,
                        ContentTemplate = Application.Current.Resources["OpenSingleBoxReadOnlyTemplate"] as DataTemplate,
                        IconTemplate = Application.Current.Resources["LocalComputerIconTemplate"] as DataTemplate,
                        Content = new Tuple<string, object>(null, this.localComputer.Items),
                    });

                this.openWritableFileTabControls.Add(new OpenTabItemData()
                {
                    SubMode = "ComputerEdit",
                    Header = Strings.SingleBoxWritableDevelopmentComputer_Label, 
                    Shortcut = Strings.SingleBoxWritableDevelopmentComputer_Shortcut, 
                    ContentTemplate = Application.Current.Resources["OpenSingleBoxWritableTemplate"] as DataTemplate,
                    IconTemplate = Application.Current.Resources["LocalComputerIconTemplate"] as DataTemplate,
                    Content = new Tuple<string, object>(null, this.localComputer.Items),
                });
            }
        }

        public IEnumerable<OpenTabItemData> OpenReadOnlyFileTabControls
        {
            get
            {
                DebugHelper.AssertUIThread();

                lock (this.lockObj)
                {
                    Init();

                    return this.openReadOnlyFileTabControls;
                }
            }
        }

        public IEnumerable<OpenTabItemData> OpenWritableFileTabControls
        {
            get
            {
                DebugHelper.AssertUIThread();

                lock (this.lockObj)
                {
                    Init();

                    return this.openWritableFileTabControls;
                }
            }
        }

        private object lockObj = new Object();
        private ISessionStateService sessionStateService = null;
        private ObservableCollection<OpenTabItemData> openReadOnlyFileTabControls = new ObservableCollection<OpenTabItemData>();
        private ObservableCollection<OpenTabItemData> openWritableFileTabControls = new ObservableCollection<OpenTabItemData>();
        private MostRecentlyUsedState localComputer = null;
        private Dictionary<string, TargetMostRecentlyUsedState> targets = new Dictionary<string, TargetMostRecentlyUsedState>();
    }
}

