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
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;
    using System.Diagnostics;
    
    public class MetadataViewService : IMetadataViewService
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        public MetadataViewService()
        {
            DebugHelper.AssertUIThread();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "metadataViews")]
        public IEnumerable<MetadataView> GetMetadataViews(Window window)
        {
            DebugHelper.AssertUIThread();

            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            while (window.Owner != null)
            {
                window = window.Owner;
            }

            IEnumerable<MetadataView> value = null;

            ToolsUIWindow toolsWindow = window as ToolsUIWindow;
            
            if (toolsWindow != null) 
            {
                LayoutTabControl tabControl = toolsWindow.LayoutTabControl;

                if (tabControl != null)
                {
                    int layoutIndex = tabControl.SelectedIndex;

                    List<MetadataView> activeTab = new List<MetadataView>();
                    List<MetadataView> otherTabs = new List<MetadataView>();
                    List<MetadataView> temp = new List<MetadataView>();

                    for (int i = 0; i < tabControl.Items.Count; ++i)
                    {
                        List<MetadataView> list = (i == layoutIndex) ? activeTab : temp;

                        LayoutInstance layout = tabControl.Items[i] as LayoutInstance;
                        if (layout != null)
                        {
                            foreach (View v in layout.FindViews("MetadataView"))
                            {
                                MetadataView mv = v as MetadataView;

                                if (mv != null)
                                {
                                    list.Add(mv);
                                }
                            }
                        }

                        if ((i != layoutIndex) && (list.Count > 0))
                        {
                            if (otherTabs.Count > 0)
                            {
                                otherTabs.Add(null);
                            }

                            otherTabs.AddRange(list.OrderBy(mv => mv.Title));

                            list.Clear();
                        }
                    }

                    List<MetadataView> metadataViews = new List<MetadataView>();

                    if (activeTab.Count > 0)
                    {
                        metadataViews.AddRange(activeTab.OrderBy(mv => mv.Title));

                        if (otherTabs.Count > 0)
                        {
                            metadataViews.Add(null);
                        }
                    }

                    metadataViews.AddRange(otherTabs);
                    value = metadataViews;
                }
            }

            return value;
        }

        public View CreateView(IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            MetadataView view = MetadataView.CreateView(serviceProvider);
            
            if (view != null)
            {
                this.metadataViews.Add(view);
                view.Closed += MetadataView_Closed;
            }

            return view;
        }

        public void UpdateMetadataControls()
        {
            DebugHelper.AssertUIThread();

            foreach (MetadataView metadataView in this.metadataViews)
            {
                metadataView.UpdateMetadataControls();
            }
        }

        public void CloseMetadataViews(ISet<MetadataInfo> metadataViewsToClose)
        {
            DebugHelper.AssertUIThread();

            if (metadataViewsToClose != null)
            {
                foreach (MetadataView metadataView in this.metadataViews)
                {
                    metadataView.CloseMetadataView(metadataViewsToClose);
                }
            }
        }

        public string GetUniqueTitle(MetadataView metadataViewIgnore)
        {
            DebugHelper.AssertUIThread();

            HashSet<string> existingTitles = new HashSet<string>();

            foreach (MetadataView metadataView in this.metadataViews)
            {
                if (metadataView != metadataViewIgnore)
                {
                    string title = metadataView.Title;
                    if (title != null)
                    {
                        title = title.Trim().ToUpper(CultureInfo.CurrentCulture);
                        existingTitles.Add(title);
                    }
                }
            }

            string value = Strings.MetadataView_Title;
            if (existingTitles.Contains(value.Trim().ToUpper(CultureInfo.CurrentCulture)))
            {
                string formatString = Strings.MetadataView_Title_Format;
                int i = 2;

                while (true)
                {
                    value = String.Format(CultureInfo.CurrentCulture, formatString, i);

                    if (!existingTitles.Contains(value.Trim().ToUpper(CultureInfo.CurrentCulture)))
                    {
                        break;
                    }

                    ++i;
                }
            }

            return value;
        }

        private void MetadataView_Closed(object sender, EventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.metadataViews.Remove(sender as MetadataView);
        }

        private readonly ObservableCollection<MetadataView> metadataViews = new ObservableCollection<MetadataView>();
    }
}
