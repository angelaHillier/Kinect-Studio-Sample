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
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class LayoutInstance : DependencyObject, IActivationSite, IViewBindingService
    {
        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(
            "IsVisible", typeof(bool), typeof(LayoutInstance), new FrameworkPropertyMetadata(true));

        static int nextActivationIndex;

        Dictionary<string, SlotContent> existingSlotContents = new Dictionary<string, SlotContent>();
        List<ViewBinding> viewBindings = new List<ViewBinding>();
        IActivationSite lastActiveChildSite;
        ServiceContainer serviceContainer;
        DataTemplate singleViewContentTemplate;
        DataTemplate tabbedViewContentTemplate;
        ToolsUIWindow ourWindow;
        LayoutInstanceState state;

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public LayoutInstance(IServiceProvider serviceProvider, IActivationSite parentSite, LayoutDefinition layoutDefinition, bool isInEditMode)
        {
            this.LayoutDefinition = layoutDefinition;
            this.LayoutDefinition.SlotDefinition.Changed += OnRootSlotChanged;
            this.LayoutDefinition.ViewSources.CollectionChanged += OnLayoutDefinitionViewSourcesChanged;
            this.ParentSite = parentSite;
            this.IsInEditMode = isInEditMode;

            this.serviceContainer = new ServiceContainer(serviceProvider);
            this.serviceContainer.AddService(typeof(IViewBindingService), this);

            this.ServiceProvider = serviceContainer;

            this.LayoutControl = new LayoutControl(this);
            this.LayoutControl.SlotDefinition = isInEditMode ? this.LayoutDefinition.SlotDefinition : this.LayoutDefinition.SlotDefinition.Clone();

            this.singleViewContentTemplate = this.LayoutControl.FindResource(isInEditMode ? "EditModeSingleViewContentTemplate" : "SingleViewContentTemplate") as DataTemplate;
            this.tabbedViewContentTemplate = this.LayoutControl.FindResource(isInEditMode ? "EditModeTabbedViewContentTemplate" : "TabbedViewContentTemplate") as DataTemplate;

            this.ourWindow = serviceProvider.GetService(typeof(ToolsUIWindow)) as ToolsUIWindow;

            object stateObject;

            // We do this trick (storing our activation state in our window per layout definition) so that
            // the layout editor can create an instance that matches the "real" layout states.
            if (!this.ourWindow.StateTable.TryGetValue(this.LayoutDefinition, out stateObject))
            {
                stateObject = new LayoutInstanceState();
                this.ourWindow.StateTable[this.LayoutDefinition] = stateObject;
            }

            this.state = (LayoutInstanceState)stateObject;

            EnsureSlotContentPopulation();
        }

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public IActivationSite ParentSite { get; private set; }

        public LayoutDefinition LayoutDefinition { get; private set; }

        public LayoutControl LayoutControl { get; private set; }

        public bool IsInEditMode { get; private set; }

        public IServiceProvider ServiceProvider { get; private set; }

        public int ActivationIndex { get { return this.state.ActivationIndex; } private set { this.state.ActivationIndex = value; } }

        public event EventHandler LayoutChanged;

        public void RemoveViewSource(ViewSource viewSource)
        {
            var slot = this.LayoutDefinition.RemoveViewSource(viewSource);

            if (slot == null)
            {
                // No slot to remove, we're done.
                return;
            }

            if (slot.Children.Count > 0)
            {
                Debug.Fail("What?");
                return;
            }

            var parentSlot = slot.Parent;

            if (parentSlot.Children.Count < 2)
            {
                Debug.Fail("Huh?");
                return;
            }

            parentSlot.Children.Remove(slot);

            if (parentSlot.Children.Count == 1)
            {
                var otherSlot = parentSlot.Children[0];

                // The parent slot is no longer necessary, because it now has a single child.
                // If that child has no children itself, then make the parent slot look like that child.
                // That includes the slot name (which moves the child's elements into place).
                if (otherSlot.Children.Count == 0)
                {
                    parentSlot.Children.Remove(otherSlot);
                    parentSlot.Orientation = otherSlot.Orientation;
                    parentSlot.Name = otherSlot.Name;
                }
                else if ((parentSlot == this.LayoutDefinition.SlotDefinition) || (otherSlot.Orientation != parentSlot.Parent.Orientation))
                {
                    // We can just copy the children (and the orientation) of the other slot up to the parent.
                    // This will make the parent slot look just like its remaining child.
                    parentSlot.Children.Remove(otherSlot);
                    foreach (var child in otherSlot.Children)
                    {
                        parentSlot.Children.Add(child);
                    }
                    parentSlot.Orientation = otherSlot.Orientation;
                }
                else
                {
                    // The parent slot goes away, and is replaced by the children of the other slot
                    var grandparent = parentSlot.Parent;
                    int index = grandparent.Children.IndexOf(parentSlot);

                    grandparent.Children.Remove(parentSlot);
                    foreach (var child in otherSlot.Children)
                    {
                        grandparent.Children.Insert(index++, child);
                    }
                }
            }
        }

        public void CloseAllViews()
        {
            foreach (var slotContent in this.existingSlotContents.Values)
            {
                foreach (var site in slotContent.ViewSites)
                {
                    if (site.View != null)
                    {
                        site.View.Close();
                    }
                }
            }
        }

        IEnumerable<XElement> BuildViewStateElements(SlotContent content)
        {
            var activeViews = this.existingSlotContents.Values.Select(sc => sc.TopmostViewSite).ToDictionary(vc => vc.ViewSource.Id);

            return content.ViewSites.Select(vc => new XElement("View",
                new XAttribute("RegisteredName", vc.View.ViewCreationCommand.RegisteredName),
                new XAttribute("SlotName", vc.ViewSource.SlotName),
                new XAttribute("Id", vc.ViewSource.Id),
                new XAttribute("Title", vc.ViewSource.Title),
                activeViews.ContainsKey(vc.ViewSource.Id) ? new XAttribute("IsSelected", "true") : null,
                new XElement("ViewState", vc.View.GetViewState())));
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public void WriteState(XElement element)
        {
            element.Add(new XAttribute("ActivationIndex", this.ActivationIndex));
            element.Add(LayoutDefinition.BuildSlotElement(this.LayoutControl.SlotDefinition));
            element.Add(this.existingSlotContents.Values.SelectMany(s => BuildViewStateElements(s)));
        }

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public void ReadState(XElement element)
        {
            if (this.LayoutDefinition.RevertedToDefault)
            {
                // This layout was reverted to default at startup, so ignore the saved instance state (it probably doesn't map).
                return;
            }

            var activationIndexAttr = element.Attribute("ActivationIndex");

            if (activationIndexAttr != null)
            {
                int index;

                if (int.TryParse(activationIndexAttr.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
                {
                    this.ActivationIndex = index;
                    nextActivationIndex = Math.Max(index + 1, nextActivationIndex);
                }
            }

            var slotElement = element.Element("Slot");

            if (slotElement != null)
            {
                this.LayoutControl.SlotDefinition = LayoutDefinition.LoadSlotFromState(element.Element("Slot"));

                try
                {
                    var viewElementTable = element.Elements("View").ToDictionary(e => int.Parse(e.Attribute("Id").Value));

                    foreach (var site in this.existingSlotContents.Values.SelectMany(sc => sc.ViewSites))
                    {
                        XElement viewElement;

                        if (viewElementTable.TryGetValue(site.ViewSource.Id, out viewElement))
                        {
                            Debug.Assert(viewElement.Attribute("RegisteredName").Value == site.View.ViewCreationCommand.RegisteredName);
                            Debug.Assert(viewElement.Attribute("SlotName").Value == site.ViewSource.SlotName);

                            var titleAttr = viewElement.Attribute("Title");

                            if (titleAttr != null)
                            {
                                site.ViewSource.Title = titleAttr.Value;
                            }

                            var viewStateElement = viewElement.Element("ViewState");

                            if (viewStateElement != null && viewStateElement.Elements().Any())
                            {
                                site.View.LoadViewState(viewStateElement.Elements().First());
                            }

                            if (viewElement.Attribute("IsSelected") != null)
                            {
                                var slotContent = site.FindParentSite<SlotContent>();
                                if (slotContent != null)
                                {
                                    slotContent.TopmostViewSite = site;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
        }

        public void ReplicateLayoutState(LayoutInstance layoutToCopy)
        {
            foreach (var kvp in this.existingSlotContents)
            {
                var ourSlotContent = kvp.Value;
                var slotContentToCopy = layoutToCopy.existingSlotContents[kvp.Key];

                ourSlotContent.ReplicateSlotContentState(slotContentToCopy);
            }
        }

        public View FindView(string registeredViewName)
        {
            var view = this.existingSlotContents.Values.SelectMany(sc => sc.ViewSites.Select(s => s.View))
                .OrderBy(v => -v.ActivationIndex)
                .FirstOrDefault(v => StringComparer.Ordinal.Equals(registeredViewName, v.ViewCreationCommand.RegisteredName));
            return view;
        }

        public IEnumerable<View> FindViews(string registeredViewName)
        {
            var views = this.existingSlotContents.Values.SelectMany(sc => sc.ViewSites.Select(s => s.View))
                .Where(v => StringComparer.Ordinal.Equals(registeredViewName, v.ViewCreationCommand.RegisteredName));
            return views;
        }

        void IActivationSite.BubbleActivation(object child)
        {
            var slotContent = child as SlotContent;

            if (slotContent != null)
            {
                this.lastActiveChildSite = slotContent;
                this.ActivationIndex = ++nextActivationIndex;
                this.ParentSite.BubbleActivation(this);
            }
        }

        void IActivationSite.TunnelActivation()
        {
            if (this.lastActiveChildSite == null)
            {
                // We need to pick one... at random
                this.lastActiveChildSite = this.existingSlotContents.Values.FirstOrDefault();
                this.ActivationIndex = ++nextActivationIndex;
            }

            if (this.lastActiveChildSite != null)
            {
                this.lastActiveChildSite.TunnelActivation();
            }
        }

        void IActivationSite.NotifyActivation(object child)
        {
            var slotContent = child as SlotContent;

            if (slotContent != null)
            {
                this.lastActiveChildSite = slotContent;
                this.ActivationIndex = ++nextActivationIndex;
                this.ParentSite.NotifyActivation(this);
            }
        }

        IViewBinding IViewBindingService.CreateViewBinding(string registeredViewName)
        {
            return new ViewBinding(this, v => StringComparer.OrdinalIgnoreCase.Equals(v.ViewCreationCommand.RegisteredName, registeredViewName));
        }

        IViewBinding IViewBindingService.CreateViewBinding(Func<View, bool> searchPredicate)
        {
            return new ViewBinding(this, searchPredicate);
        }

        void OnViewActivated(object sender, EventArgs e)
        {
            ToolsUIApplication.Instance.RequestViewBindingUpdate();
        }

        public void ProcessViewBindingUpdateRequest()
        {
            // NOTE: This should only be called by ToolsUIApplication.RequestViewBindingUpdate.  If you need a binding update, call that.
            foreach (var binding in this.viewBindings)
            {
                binding.UpdateBinding();
            }
        }

        public void EnsureSlotContentPopulation()
        {
            // This is a mark-and-sweep style operation. Mark all existing content objects; any that
            // aren't touched by the loop below will die.
            foreach (var content in this.existingSlotContents.Values)
            {
                content.Mark();
                foreach (var site in content.ViewSites)
                {
                    if (site.View != null)
                    {
                        site.View.Activated -= OnViewActivated;
                    }
                }
            }

            foreach (var source in this.LayoutDefinition.ViewSources)
            {
                SlotContent content;

                if (!this.existingSlotContents.TryGetValue(source.SlotName, out content))
                {
                    content = new SlotContent(this, source.SlotName);
                    this.existingSlotContents.Add(source.SlotName, content);
                }

                content.ConfirmViewSource(source);
            }

            List<string> removals = null;
            bool modified = false;

            foreach (var kvp in this.existingSlotContents)
            {
                if (!kvp.Value.Sweep(this.IsInEditMode, this.singleViewContentTemplate, this.tabbedViewContentTemplate, this.serviceContainer))
                {
                    if (removals == null)
                    {
                        removals = new List<string>();
                    }

                    removals.Add(kvp.Key);
                }
            }

            if (removals != null)
            {
                foreach (var slotName in removals)
                {
                    this.existingSlotContents.Remove(slotName);
                }

                modified = true;
            }

            foreach (var content in this.existingSlotContents.Values)
            {
                foreach (var site in content.ViewSites)
                {
                    if (site.View != null)
                    {
                        site.View.Activated += OnViewActivated;
                    }
                }
            }

            if (modified)
            {
                ToolsUIApplication.Instance.RequestViewBindingUpdate();

                var handler = this.LayoutChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        void OnLayoutDefinitionViewSourcesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EnsureSlotContentPopulation();
        }

        void OnRootSlotChanged(object sender, SlotChangedEventArgs e)
        {
            if (!this.IsInEditMode)
            {
                this.LayoutControl.SlotDefinition = this.LayoutDefinition.SlotDefinition.Clone();
            }

            EnsureSlotContentPopulation();
        }

        class LayoutInstanceState
        {
            public int ActivationIndex { get; set; }
        }

        class ViewBinding : IViewBinding
        {
            LayoutInstance targetLayoutInstance;
            View view;
            Func<View, bool> searchPredicate;

            public ViewBinding(LayoutInstance targetLayoutInstance, Func<View, bool> searchPredicate)
            {
                // Terminology reminder for bindings:
                //  "Target" refers to the thing that wants to track another view (the "target" of the update when the source changes)
                //  "Source" refers to the thing the target wants to be bound to
                this.targetLayoutInstance = targetLayoutInstance;
                this.searchPredicate = searchPredicate;

                targetLayoutInstance.viewBindings.Add(this);

                // Delay the first attempt at binding to the view, to avoid mis-bindings on initial
                // layout creation.  (The rest of the layout may not be populated yet, so doing a
                // binding immediately will usually not resolve correctly).
                Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() => UpdateBinding()), DispatcherPriority.Background);
            }

            public View View
            {
                get
                {
                    return this.view;
                }
                private set
                {
                    if (this.view != value)
                    {
                        if (this.view != null)
                        {
                            this.view.Closed -= OnViewClosed;
                            this.view.PropertyChanged += OnViewPropertyChanged;
                        }

                        this.view = value;

                        if (this.view != null)
                        {
                            this.view.Closed += OnViewClosed;
                            this.view.PropertyChanged += OnViewPropertyChanged;
                        }

                        if (this.view == null || this.view.IsViewContentLoaded)
                        {
                            NotifyViewChanged();
                        }
                    }
                }
            }

            public event EventHandler ViewChanged;

            public bool UpdateBinding()
            {
                var newView = TryBindToView();

                if (newView != null)
                {
                    this.View = newView;
                    return true;
                }

                return false;
            }

            class ViewBindingSource
            {
                public ToolsUIWindow Window { get; set; }
                public LayoutInstance LayoutInstance { get; set; }
                public View View { get; set; }
            }

            class ViewBindingComparer : IComparer<ViewBindingSource>
            {
                LayoutInstance targetLayoutInstance;

                public ViewBindingComparer(LayoutInstance targetLayoutInstance)
                {
                    this.targetLayoutInstance = targetLayoutInstance;
                }

                public int Compare(ViewBindingSource x, ViewBindingSource y)
                {
                    // If one view is in the same layout as the target (and the other is not) then it wins.
                    if (x.LayoutInstance == this.targetLayoutInstance && y.LayoutInstance != this.targetLayoutInstance) { return -1; }
                    if (x.LayoutInstance != this.targetLayoutInstance && y.LayoutInstance == this.targetLayoutInstance) { return 1; }

                    // Either both, or neither, are in the target layout.  Whichever one was last active wins.
                    return y.View.ActivationIndex - x.View.ActivationIndex;
                }
            }

            View TryBindToView()
            {
                var comparer = new ViewBindingComparer(this.targetLayoutInstance);
                var allViews = ToolsUIApplication.Instance.ToolsUIWindows
                    .SelectMany(w => w.LayoutTabControl.Items.OfType<LayoutInstance>()
                        .SelectMany(data => data.existingSlotContents.Values
                            .SelectMany(sc => sc.ViewSites
                                .Where(site => this.searchPredicate(site.View))
                                .Select(site => new ViewBindingSource
                                {
                                    Window = w,
                                    LayoutInstance = data,
                                    View = site.View
                                }))))
                                .OrderBy(vd => vd, comparer);

                var vbs = allViews.FirstOrDefault();

                if (vbs != null)
                {
                    return vbs.View;
                }

                return null;
            }

            void OnViewClosed(object sender, EventArgs e)
            {
                if (!UpdateBinding())
                {
                    this.View = null;
                }
            }

            void OnViewPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (object.ReferenceEquals(sender, this.view) && this.view.IsViewContentLoaded && (StringComparer.Ordinal.Equals(e.PropertyName, "IsViewContentLoaded")))
                {
                    // We don't fire the view changed event until the view's content is loaded
                    NotifyViewChanged();
                }
            }

            void NotifyViewChanged()
            {
                var handler = this.ViewChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }

            public void Dispose()
            {
                if (this.view != null)
                {
                    this.view.Closed -= OnViewClosed;
                    this.view.PropertyChanged -= OnViewPropertyChanged;
                    this.view = null;
                }

                if (this.targetLayoutInstance != null)
                {
                    this.targetLayoutInstance.viewBindings.Remove(this);
                    this.targetLayoutInstance = null;
                }
            }
        }
    }
}
