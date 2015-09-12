//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    class SlotContent : IActivationSite
    {
        // Sources and sites are separate, because sites are "instances" of sources.  (There can be
        // multiple layouts using the same sources; each must have its own instances of the view sites.)
        List<ViewSource> sources = new List<ViewSource>();
        Dictionary<ViewSource, ViewSite> sites = new Dictionary<ViewSource, ViewSite>();
        IActivationSite lastActiveChildSite;
        LayoutControl layoutControl;
        FrameworkElement slotPanelChild;
        bool childAddedToSlotPanel;

        public IActivationSite ParentSite { get; private set; }
        public FrameworkElement Content { get; private set; }

        // Note:  This property has protection against us containing sources with no site.  This can happen while editing
        // layouts.  Normally, there will be a site for every source, but due to possible re-entrancy caused by PushFrame
        // (which happens in View.Activate under certain circumstances) we may be in the process of changing our state.
        public IEnumerable<ViewSite> ViewSites { get { return sources.Where(p => this.sites.ContainsKey(p)).Select(p => this.sites[p]); } }
        public string SlotName { get; private set; }

        public ViewSite TopmostViewSite
        {
            get
            {
                ViewSite site = this.lastActiveChildSite as ViewSite;

                if (site == null)
                {
                    site = this.ViewSites.FirstOrDefault();
                    if (site == null)
                    {
                        Debug.Fail("A slot content object should not exist without view sources/sites.");
                        return null;
                    }
                    this.lastActiveChildSite = site;
                }

                return site;
            }
            set
            {
                // NOTE:  Setting this property is not the same as activating the view.  It should only be used
                // during state restoration.
                this.lastActiveChildSite = value;

                var tabControl = this.slotPanelChild as TabControl;

                if (tabControl != null)
                {
                    tabControl.SelectedIndex = tabControl.Items.IndexOf(value);
                }
            }
        }

        public SlotContent(LayoutInstance layoutInstance, string slotName)
        {
            this.ParentSite = layoutInstance;
            this.SlotName = slotName;
            this.layoutControl = layoutInstance.LayoutControl;
        }

        public void Mark()
        {
            this.sources.Clear();
        }

        public void ConfirmViewSource(ViewSource source)
        {
            this.sources.Add(source);
        }

        public void ReplicateSlotContentState(SlotContent sourceSlotContent)
        {
            foreach (var viewSource in sourceSlotContent.sources)
            {
                var ourSite = this.sites[viewSource];
                var siteToCopy = sourceSlotContent.sites[viewSource];

                ourSite.View.ReplicateEphemeralViewState(siteToCopy.View);
            }
        }

        void IActivationSite.BubbleActivation(object child)
        {
            var site = child as ViewSite;

            if (site != null)
            {
                // Must set last active site before bubbling -- the bubble may cause an immediate tunnel.
                this.lastActiveChildSite = site;
                this.ParentSite.BubbleActivation(this);

                var tabControl = this.slotPanelChild as ViewTabControl;

                if (tabControl != null && tabControl.Items.Contains(site))
                {
                    tabControl.SelectedIndex = tabControl.Items.IndexOf(site);
                }
            }
        }

        void IActivationSite.TunnelActivation()
        {
            if (this.lastActiveChildSite == null)
            {
                // We need to pick one... 
                var tabControl = this.slotPanelChild as ViewTabControl;

                if (tabControl != null)
                {
                    this.lastActiveChildSite = tabControl.SelectedItem as ViewSite;
                }
            }

            if (this.lastActiveChildSite != null)
            {
                this.lastActiveChildSite.TunnelActivation();
            }
        }

        void IActivationSite.NotifyActivation(object child)
        {
            var site = child as ViewSite;

            if (site != null)
            {
                this.lastActiveChildSite = site;
                this.ParentSite.NotifyActivation(this);
            }
        }

        void CloseRemainingViews(Dictionary<ViewSource, ViewSite> newSites)
        {
            var toClose = this.sites.Values.ToArray();
            this.sites = newSites;

            foreach (var site in toClose)
            {
                if (site.View != null)
                {
                    site.View.Close();
                }
            }
        }

        void SetSlotPanelContent(FrameworkElement element)
        {
            if (element != this.slotPanelChild)
            {
                if (this.childAddedToSlotPanel)
                {
                    this.layoutControl.RemoveSlotPanelChild(this.slotPanelChild);
                    this.slotPanelChild = null;
                    this.childAddedToSlotPanel = false;
                }

                if (element != null)
                {
                    this.slotPanelChild = element;
                }

                this.Content = this.slotPanelChild;
            }

            if (!this.childAddedToSlotPanel && (this.slotPanelChild != null))
            {
                this.childAddedToSlotPanel = this.layoutControl.AddSlotPanelChild(this.slotPanelChild, this.SlotName);
            }
        }

        public bool Sweep(bool editMode, DataTemplate singleViewTemplate, DataTemplate tabbedViewTemplate, IServiceProvider serviceProvider)
        {
            if (this.sources.Count == 0)
            {
                // Nothing added back to this slot, so we're toast.  Remove our old content (close all views) and die.
                SetSlotPanelContent(null);
                CloseRemainingViews(new Dictionary<ViewSource, ViewSite>());
                return false;
            }

            var newSites = new Dictionary<ViewSource, ViewSite>();

            if (this.sources.Count == 1)
            {
                var presenter = this.slotPanelChild as ContentPresenter;

                // We need our content to be a simple ContentPresenter.  If it isn't, we need to replace.
                if (presenter == null)
                {
                    presenter = new ContentPresenter() { ContentTemplate = singleViewTemplate };
                }

                // Need to call this regardless, as it ensures that it has been added to the slot panel
                SetSlotPanelContent(presenter);

                ViewSite site;

                if (!this.sites.TryGetValue(this.sources[0], out site))
                {
                    // NOTE:  Passing singleViewTemplate here has no effect -- the important thing is that it's set as the 
                    // content template of the presenter (above).  
                    site = new ViewSite(this, this.sources[0], singleViewTemplate, editMode, serviceProvider);
                }
                else
                {
                    this.sites.Remove(this.sources[0]);
                }

                presenter.Content = site;
                if (presenter.IsLoaded)
                {
                    OnViewPresenterLoaded(presenter, null);
                }
                else
                {
                    presenter.Loaded += OnViewPresenterLoaded;
                }

                newSites.Add(this.sources[0], site);
            }
            else
            {
                var tabControl = this.slotPanelChild as ViewTabControl;

                // We need our content to be a tab control.  If it isn't, we need to replace.
                if (tabControl == null)
                {
                    tabControl = new ViewTabControl();
                }

                // Call regardless, in case the layout control didn't have its template applied last time...
                SetSlotPanelContent(tabControl);

                for (int i = 0; i < this.sources.Count; i++)
                {
                    ViewSource source = this.sources[i];
                    ViewSite site;

                    if (!this.sites.TryGetValue(source, out site))
                    {
                        site = new ViewSite(this, source, tabbedViewTemplate, editMode, serviceProvider);
                    }
                    else
                    {
                        this.sites.Remove(source);
                    }

                    int index = tabControl.Items.IndexOf(site);

                    if (index < i)
                    {
                        Debug.Assert(index == -1, "A view site has been added twice?!?");
                        tabControl.Items.Insert(i, site);
                    }
                    else
                    {
                        while (index > i)
                        {
                            // Other (presumably old) view contents are in the way.  Remove them.
                            tabControl.Items.RemoveAt(i);
                            index--;
                        }
                    }

                    newSites.Add(source, site);
                }

                while (tabControl.Items.Count > this.sources.Count)
                {
                    tabControl.Items.RemoveAt(tabControl.Items.Count - 1);
                }

                if (tabControl.SelectedIndex < 0)
                {
                    tabControl.SelectedIndex = 0;
                }
            }

            CloseRemainingViews(newSites);
            this.sites = newSites;
            return true;
        }

        void OnViewPresenterLoaded(object sender, RoutedEventArgs e)
        {
            var presenter = sender as ContentPresenter;

            if ((presenter != null) && (presenter.Content is ViewSite))
            {
                var site = (ViewSite)presenter.Content;

                presenter.SetBinding(ShortcutManager.ShortcutProperty, new Binding { Source = site.ViewSource, Path = new PropertyPath(ViewSource.ShortcutKeyProperty), StringFormat = "{0}:ViewSelect" });
                ShortcutManager.SetHorizontalAlignment(presenter, HorizontalAlignment.Left);
                ShortcutManager.SetVerticalAlignment(presenter, VerticalAlignment.Top);
                ShortcutManager.SetOffset(presenter, new Point(25, 25));
                ShortcutManager.SetCommand(presenter, LayoutControl.ViewShortcutCommand);
                ShortcutManager.SetCommandParameter(presenter, site);
                ShortcutManager.SetCommandTarget(presenter, presenter);

                presenter.Loaded -= OnViewPresenterLoaded;
            }
        }
    }
}

