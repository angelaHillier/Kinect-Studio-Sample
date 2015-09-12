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
    class ViewSite : IActivationSite
    {
        public ViewSource ViewSource { get; private set; }
        public DataTemplate Template { get; private set; }
        public View View { get; set; }
        public IActivationSite ParentSite { get; private set; }

        public ViewSite(IActivationSite parentSite, ViewSource viewSource, DataTemplate template, bool editMode, IServiceProvider serviceProvider)
        {
            this.ParentSite = parentSite;
            this.ViewSource = viewSource;
            this.Template = template;

            if (!editMode)
            {
                this.View = viewSource.ViewCreator.CreateView(serviceProvider);
                this.View.DocumentAffinity = this.ViewSource.Parent.DocumentFactoryName;
                this.View.Site = this;

                // This binding allows views to control the view source title by simply changing their View.Title property.
                BindingOperations.SetBinding(this.View, View.TitleProperty, new Binding
                {
                    Source = this.ViewSource,
                    Path = new PropertyPath(ViewSource.TitleProperty),
                    Mode = BindingMode.TwoWay
                });
            }
        }

        void IActivationSite.BubbleActivation(object child)
        {
            if (object.ReferenceEquals(child, this.View))
            {
                this.ParentSite.BubbleActivation(this);
            }
        }

        void IActivationSite.TunnelActivation()
        {
            // Tunnel activation is called when a parent site (typically a tab control) has switched
            // to make this view active.  It can be called as a result of View.Activate(), but the
            // call is idempotent here in that case.
            if (this.View != null)
            {
                this.View.Activate();
            }
        }

        void IActivationSite.NotifyActivation(object child)
        {
            if (object.ReferenceEquals(child, this.View))
            {
                this.ParentSite.NotifyActivation(this);
            }
        }
    }
}
