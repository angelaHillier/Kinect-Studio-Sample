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
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    public class LastSelectedStreams
    {
        public LastSelectedStreams(IServiceProvider serviceProvider, string name)
        {
            DebugHelper.AssertUIThread();

            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            this.name = name;

            if (serviceProvider != null)
            {
                this.sessionStateService = serviceProvider.GetService(typeof(ISessionStateService)) as ISessionStateService;
                if (this.sessionStateService != null)
                {
                    this.sessionStateService.StateSaveRequested += SessionStateService_StateSaveRequested;

                    XElement element = this.sessionStateService.GetSessionState(this.name);
                    if (element == null)
                    {
                        element = this.sessionStateService.GetSessionState("KStudioServiceLast" + this.name);

                        if (element != null)
                        {
                            XElement oldElement = element;
                            element = new XElement("last" + this.name);

                            XElement oldItemsElement  = oldElement.Element("Items");
                            if (oldItemsElement != null)
                            {
                                List<XElement> oldElements = new List<XElement>(oldItemsElement.Elements("ListEntry"));

                                int count = oldElements.Count;
                                count = (count / 2) * 2;

                                for (int i = 0; i < count; i += 2)
                                {
                                    Guid dataTypeId = XmlExtensions.GetAttribute(oldElements[i], "Value", Guid.Empty);
                                    Guid semanticId = XmlExtensions.GetAttribute(oldElements[i + 1], "Value", Guid.Empty);

                                    XElement streamElement = new XElement("stream");

                                    streamElement.SetAttributeValue("dataTypeId", dataTypeId.ToString());
                                    streamElement.SetAttributeValue("semanticId", semanticId.ToString());

                                    element.Add(streamElement);
                                }
                            }

                            this.sessionStateService.SetSessionState("KStudioServiceLast" + this.name, null);
                            this.sessionStateService.SetSessionState(this.name, element);
                        }
                    }

                    if (element != null)
                    {
                        foreach (XElement streamElement in element.Elements("stream"))
                        {
                            Guid dataTypeId = XmlExtensions.GetAttribute(streamElement, "dataTypeId", Guid.Empty);
                            Guid semanticId = XmlExtensions.GetAttribute(streamElement, "semanticId", Guid.Empty);

                            KStudioEventStreamIdentifier identifier = new KStudioEventStreamIdentifier(dataTypeId, semanticId);

                            if (!this.hashSet.Contains(identifier))
                            {
                                this.hashSet.Add(identifier);
                            }
                        }
                    }
                }
            }
        }

        public HashSet<KStudioEventStreamIdentifier> HashSet
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.hashSet;
            }
        }

        private void SessionStateService_StateSaveRequested(object sender, EventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (this.sessionStateService != null)
            {
                XElement element = this.sessionStateService.GetSessionState(this.name);
                if (element == null)
                {
                    element = new XElement("last" + this.name);
                    this.sessionStateService.SetSessionState(this.name, element);
                }

                element.RemoveAll();

                foreach (KStudioEventStreamIdentifier identifier in this.hashSet)
                {
                    XElement streamElement = new XElement("stream");

                    streamElement.SetAttributeValue("dataTypeId", identifier.DataTypeId.ToString());
                    streamElement.SetAttributeValue("semanticId", identifier.SemanticId.ToString());

                    element.Add(streamElement);
                }
            }
        }

        private readonly HashSet<KStudioEventStreamIdentifier> hashSet = new HashSet<KStudioEventStreamIdentifier>();
        private readonly string name;
        private readonly ISessionStateService sessionStateService;
    }
}
