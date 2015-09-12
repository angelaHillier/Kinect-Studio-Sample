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
    using System.Windows;
    using System.Windows.Threading;
    using System.Xml.Linq;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    public class MetadataView : View
    {
        private MetadataViewContent viewContent = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "serviceProvider")]
        public MetadataView(IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            // Give the metadata view a unique title when first added by the user
            IMetadataViewService metadataViewService = null;
            
            if (serviceProvider != null)
            {
                metadataViewService = serviceProvider.GetService(typeof(IMetadataViewService)) as IMetadataViewService;
            }

            if (metadataViewService != null)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                    {
                        DebugHelper.AssertUIThread();

                        if (this.setTitle)
                        {
                            this.setTitle = false;

                            this.Title = metadataViewService.GetUniqueTitle(this);
                        }
                    }));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "metadataInfo")]
        public void UpdateMetadataControls()
        {
            DebugHelper.AssertUIThread();

            if (this.viewContent != null)
            {
                MetadataInfo metadataInfo = this.viewContent.MetadataInfo;
                this.viewContent.MetadataInfo = null;
                this.viewContent.MetadataInfo = metadataInfo;
            }
        }

        public void CloseMetadataView(ISet<MetadataInfo> metadataViewsToClose)
        {
            DebugHelper.AssertUIThread();

            if ((metadataViewsToClose != null) && (this.metadataInfo != null))
            {
                if (metadataViewsToClose.Contains(this.metadataInfo))
                {
                    this.metadataInfo = null;
                }
            }

            if (this.viewContent != null)
            {
                this.viewContent.CloseMetadataView(metadataViewsToClose);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "metadataInfo")]
        public void SetMetadata(MetadataInfo metadataInfo)
        {
            DebugHelper.AssertUIThread();

            if (this.viewContent == null)
            {
                this.metadataInfo = metadataInfo;
            }
            else
            {
                this.viewContent.MetadataInfo = metadataInfo;
            }
        }

        public string FullTitle
        {
            get
            {
                string layoutName = this.LayoutName.Trim();
                string title = this.Title.Trim();

                if (String.IsNullOrWhiteSpace(layoutName))
                {
                    layoutName = Strings.UnknownLayout;
                }
                else if (String.IsNullOrWhiteSpace(title))
                {
                    title = Strings.UnknownViewTitle;
                }

                string value = String.Format(CultureInfo.CurrentCulture, Strings.LayoutView_Name_Format, layoutName, title);
                return value;
            }
        }

        protected override FrameworkElement CreateViewContent()
        {
            DebugHelper.AssertUIThread();

            this.viewContent = new MetadataViewContent(this)
                {
                    PublicKeyWidth = this.publicKeyWidth,
                    PublicValueWidth = this.publicValueWidth,
                    PersonalKeyWidth = this.personalKeyWidth,
                    PersonalValueWidth = this.personalValueWidth,
                    MetadataInfo = this.metadataInfo,
                };

            this.metadataInfo = null;

            return this.viewContent;
        }

        public override XElement GetViewState()
        {
            DebugHelper.AssertUIThread();

            if (this.viewContent != null)
            {
                this.publicKeyWidth = this.viewContent.PublicKeyWidth;
                this.publicValueWidth = this.viewContent.PublicValueWidth;
                this.personalKeyWidth = this.viewContent.PersonalKeyWidth;
                this.personalValueWidth = this.viewContent.PersonalValueWidth;
            }

            XElement element = new XElement("Columns");
            element.SetAttributeValue("publicKeyWidth", this.publicKeyWidth);
            element.SetAttributeValue("publicValueWidth", this.publicValueWidth);
            element.SetAttributeValue("personalKeyWidth", this.personalKeyWidth);
            element.SetAttributeValue("personalValueWidth", this.personalValueWidth);

            return element;
        }

        public override void LoadViewState(XElement state)
        {
            DebugHelper.AssertUIThread();

            this.setTitle = false;

            this.publicKeyWidth = XmlExtensions.GetAttribute(state, "publicKeyWidth", this.publicKeyWidth);
            this.publicValueWidth = XmlExtensions.GetAttribute(state, "publicValueWidth", this.publicValueWidth);
            this.personalKeyWidth = XmlExtensions.GetAttribute(state, "personalKeyWidth", this.personalKeyWidth);
            this.personalValueWidth = XmlExtensions.GetAttribute(state, "personalValueWidth", this.personalValueWidth);

            if (this.viewContent != null)
            {
                this.viewContent.PublicKeyWidth = this.publicKeyWidth;
                this.viewContent.PublicValueWidth = this.publicValueWidth;
                this.viewContent.PersonalKeyWidth = this.personalKeyWidth;
                this.viewContent.PersonalValueWidth = this.personalValueWidth;
            }
        }

        public static MetadataView CreateView(IServiceProvider serviceProvider)
        {
            DebugHelper.AssertUIThread();

            MetadataView value = new MetadataView(serviceProvider);

            return value;
        }

        private bool setTitle = true;
        private double publicKeyWidth = 100.0;
        private double publicValueWidth = 100.0;
        private double personalKeyWidth = 100.0;
        private double personalValueWidth = 100.0;
        private MetadataInfo metadataInfo = null;
    }
}
