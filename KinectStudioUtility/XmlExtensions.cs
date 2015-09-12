//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Xml.Linq;

    public static class XmlExtensions
    {
        public static string GetAttribute(XElement element, string attributeName, string defaultValue)
        {
            string value = defaultValue;

            if ((element != null) && (attributeName != null))
            {
                XAttribute attribute = element.Attribute(attributeName);
                if (attribute != null)
                {
                    string temp = attribute.Value;
                    if (!String.IsNullOrWhiteSpace(temp))
                    {
                        value = temp;
                    }
                }
            }

            return value;
        }

        public static bool GetAttribute(XElement element, string attributeName, bool defaultValue)
        {
            bool value = defaultValue;

            if ((element != null) && (attributeName != null))
            {
                XAttribute attribute = element.Attribute(attributeName);
                if (attribute != null)
                {
                    bool temp;
                    if (bool.TryParse(attribute.Value, out temp))
                    {
                        value = temp;
                    }
                }
            }

            return value;
        }

        public static double GetAttribute(XElement element, string attributeName, double defaultValue)
        {
            double value = defaultValue;

            if ((element != null) && (attributeName != null))
            {
                XAttribute attribute = element.Attribute(attributeName);
                if (attribute != null)
                {
                    double temp;
                    if (double.TryParse(attribute.Value, out temp))
                    {
                        value = temp;
                    }
                }
            }

            return value;
        }

        public static float GetAttribute(XElement element, string attributeName, float defaultValue)
        {
            float value = defaultValue;

            if ((element != null) && (attributeName != null))
            {
                XAttribute attribute = element.Attribute(attributeName);
                if (attribute != null)
                {
                    float temp;
                    if (float.TryParse(attribute.Value, out temp))
                    {
                        value = temp;
                    }
                }
            }

            return value;
        }

        public static int GetAttribute(XElement element, string attributeName, int defaultValue)
        {
            int value = defaultValue;

            if ((element != null) && (attributeName != null))
            {
                XAttribute attribute = element.Attribute(attributeName);
                if (attribute != null)
                {
                    int temp;
                    if (int.TryParse(attribute.Value, out temp))
                    {
                        value = temp;
                    }
                }
            }

            return value;
        }

        public static uint GetAttribute(XElement element, string attributeName, uint defaultValue)
        {
            uint value = defaultValue;

            if ((element != null) && (attributeName != null))
            {
                XAttribute attribute = element.Attribute(attributeName);
                if (attribute != null)
                {
                    uint temp;
                    if (uint.TryParse(attribute.Value, out temp))
                    {
                        value = temp;
                    }
                }
            }

            return value;
        }

        public static Guid GetAttribute(XElement element, string attributeName, Guid defaultValue)
        {
            Guid value = defaultValue;

            if ((element != null) && (attributeName != null))
            {
                XAttribute attribute = element.Attribute(attributeName);
                if (attribute != null)
                {
                    Guid temp;
                    if (Guid.TryParse(attribute.Value, out temp))
                    {
                        value = temp;
                    }
                }
            }

            return value;
        }

        public static TimeSpan GetAttribute(XElement element, string attributeName, TimeSpan defaultValue)
        {
            TimeSpan value = defaultValue;

            if ((element != null) && (attributeName != null))
            {
                XAttribute attribute = element.Attribute(attributeName);
                if (attribute != null)
                {
                    TimeSpan temp;
                    if (TimeSpan.TryParse(attribute.Value, out temp))
                    {
                        value = temp;
                    }
                }
            }

            return value;
        }
    }
}
