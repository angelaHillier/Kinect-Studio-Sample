//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using KinectStudioUtility;

    public class VisualizationViewSettings
    {
        public void Load(XElement element)
        {
            DebugHelper.AssertUIThread();

            this.pluginViewSettings.Clear();

            if (element != null)
            {
                this.viewSettingsElement = new XElement(element);

                foreach (XElement pluginViewSettingsElement in element.Elements("plugin"))
                {
                    XAttribute idAttribute = pluginViewSettingsElement.Attribute("id");
                    if (idAttribute != null)
                    {
                        Guid pluginViewId;

                        if (Guid.TryParse(idAttribute.Value, out pluginViewId))
                        {
                            this.pluginViewSettings[pluginViewId] = new XElement(pluginViewSettingsElement);
                        }
                    }
                }

                this.viewSettingsElement.RemoveNodes();
            }
        }

        public XElement Save()
        {
            DebugHelper.AssertUIThread();

            XElement element = new XElement(this.viewSettingsElement);

            foreach (KeyValuePair<Guid, XElement> kv in this.pluginViewSettings)
            {
                XElement pluginViewSettingsElement = new XElement(kv.Value);
                pluginViewSettingsElement.SetAttributeValue("id", kv.Key.ToString());

                element.Add(pluginViewSettingsElement);
            }

            return element;
        }

        public XElement ViewSettingsElement
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.viewSettingsElement;
            }
        }

        public XElement GetPluginViewSettings(Guid pluginViewId)
        {
            DebugHelper.AssertUIThread();

            XElement pluginViewSettingsElement;
            this.pluginViewSettings.TryGetValue(pluginViewId, out pluginViewSettingsElement);
            return pluginViewSettingsElement;
        }

        public void SetPluginViewSettings(Guid pluginViewId, XElement pluginViewSettingsElement)
        {
            DebugHelper.AssertUIThread();

            if (pluginViewSettingsElement == null)
            {
                this.pluginViewSettings.Remove(pluginViewId);
            }
            else
            {
                this.pluginViewSettings[pluginViewId] = pluginViewSettingsElement;
            }
        }

        private XElement viewSettingsElement = new XElement("view");
        private readonly Dictionary<Guid, XElement> pluginViewSettings = new Dictionary<Guid, XElement>();
    }
}
