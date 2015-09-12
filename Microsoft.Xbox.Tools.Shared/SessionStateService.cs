//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class SessionStateService : ISessionStateService
    {
        private static Dictionary<Type, Func<string, object>> primitiveTypes;

        private Dictionary<string, XElement> stateTable;
        private Dictionary<string, object> variableTable;

        static SessionStateService()
        {
            primitiveTypes = new Dictionary<Type, Func<string, object>>();
            primitiveTypes.Add(typeof(bool), str => bool.Parse(str));
            primitiveTypes.Add(typeof(int), str => int.Parse(str, CultureInfo.InvariantCulture));
            primitiveTypes.Add(typeof(long), str => long.Parse(str, CultureInfo.InvariantCulture));
            primitiveTypes.Add(typeof(float), str => float.Parse(str, CultureInfo.InvariantCulture));
            primitiveTypes.Add(typeof(double), str => double.Parse(str, CultureInfo.InvariantCulture));
            primitiveTypes.Add(typeof(string), str => str);
            primitiveTypes.Add(typeof(uint), str => uint.Parse(str, CultureInfo.InvariantCulture));
            primitiveTypes.Add(typeof(Guid), str => Guid.Parse(str));
        }

        public SessionStateService()
        {
            this.stateTable = new Dictionary<string, XElement>();
            this.variableTable = new Dictionary<string, object>();
        }

        public event EventHandler StateSaveRequested;

        public bool SetFullSessionState(XElement rootStateElement)
        {
            var states = rootStateElement.Elements("State");

            this.stateTable.Clear();
            if (states != null)
            {
                foreach (var keyElement in states)
                {
                    this.stateTable[keyElement.Attribute("Key").Value] = keyElement.Elements().First();
                }
            }

            return true;
        }

        public XElement GetFullSessionState()
        {
            var handler = this.StateSaveRequested;

            // Let everyone that's interested in saving their state give it to us
            if (handler != null)
                handler(this, EventArgs.Empty);

            // Save our state variables
            foreach (var pair in this.variableTable)
            {
                SetSessionState(pair.Key, SaveVariableState(pair.Key, pair.Value));
            }

            return new XElement("SessionState",
                this.stateTable.Select(pair => new XElement("State",
                    new XAttribute("Key", pair.Key),
                    pair.Value)));
        }

        public XElement GetSessionState(string stateKey)
        {
            XElement state;

            this.stateTable.TryGetValue(stateKey, out state);
            return state;
        }

        public void SetSessionState(string stateKey, XElement state)
        {
            // Use unique state keys!  Don't walk on other people.  Play nice.
            if (state != null)
            {
                this.stateTable[stateKey] = state;
            }
            else
            {
                this.stateTable.Remove(stateKey);
            }
        }

        public void DeclareSessionStateVariable(string name, object variable)
        {
            // Use unique names!  They are used as state keys, so they must be unique w.r.t. them as well...
            if (variable != null)
            {
                this.variableTable[name] = variable;

                XElement state;

                if (this.stateTable.TryGetValue(name, out state))
                {
                    // This variable had state persisted -- load it.
                    LoadVariableState(variable, state);
                }
            }
            else
            {
                this.variableTable.Remove(name);
                this.stateTable.Remove(name);
            }
        }

        Func<string, object> GetConverterForType(Type type)
        {
            Func<string, object> converter;

            if (!primitiveTypes.TryGetValue(type, out converter))
            {
                if (type.IsEnum)
                {
                    converter = (str) => Enum.Parse(type, str);
                    primitiveTypes[type] = converter;
                }
            }

            return converter;
        }

        void LoadList(Type elementType, IList list, XElement state)
        {
            Func<string, object> converter = null;

            converter = GetConverterForType(elementType);
            foreach (var element in state.Elements("ListEntry"))
            {
                if (converter != null)
                {
                    list.Add(converter(element.Attribute("Value").Value));
                }
                else
                {
                    var instance = Activator.CreateInstance(elementType);
                    var valueElement = element.Element("Value");

                    if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        Type subElementType = elementType.GetGenericArguments()[0];
                        LoadList(subElementType, (IList)instance, valueElement);
                    }
                    else
                    {
                        LoadVariableState(instance, valueElement);
                    }

                    list.Add(instance);
                }
            }
        }

        void LoadVariableState(object variable, XElement state)
        {
            try
            {
                Type type = variable.GetType();

                foreach (var attr in state.Attributes())
                {
                    var pi = type.GetProperty(attr.Name.LocalName);

                    if (pi != null)
                    {
                        var ignoreAttr = pi.GetCustomAttributes(typeof(IgnoreSessionStateFieldAttribute), false);

                        if (ignoreAttr == null || ignoreAttr.Length == 0)
                        {
                            var func = GetConverterForType(pi.PropertyType);
                            pi.SetValue(variable, func(attr.Value), null);
                        }
                    }
                }

                foreach (var element in state.Elements())
                {
                    var pi = type.GetProperty(element.Name.LocalName);

                    if (pi != null)
                    {
                        var instance = Activator.CreateInstance(pi.PropertyType);

                        if (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            Type elementType = pi.PropertyType.GetGenericArguments()[0];
                            LoadList(elementType, (IList)instance, element);
                        }
                        else
                        {
                            LoadVariableState(instance, element);
                        }

                        pi.SetValue(variable, instance, null);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        XElement SaveList(string name, object variable)
        {
            var list = variable as IList;
            List<XElement> childElements = null;

            try
            {
                childElements = list.OfType<object>().Select(o =>
                {
                    object value = null;

                    if (o != null)
                    {
                        Type type = o.GetType();
                        var converter = GetConverterForType(type);

                        if (converter != null)
                        {
                            value = new XAttribute("Value", o);
                        }
                        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            value = SaveList("Value", o);
                        }
                        else if (type.GetConstructor(Type.EmptyTypes) != null || type.IsValueType)
                        {
                            value = SaveVariableState("Value", o);
                        }
                    }

                    return new XElement("ListEntry", value);
                }).ToList();
            }
            catch (Exception)
            {
            }

            return new XElement(name, childElements);
        }

        XElement SaveVariableState(string name, object variable)
        {
            List<object> children = new List<object>();

            try
            {
                Type type = variable.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite && p.CanRead);
                foreach (var p in props)
                {
                    var attr = p.GetCustomAttributes(typeof(IgnoreSessionStateFieldAttribute), false);

                    if (attr == null || attr.Length == 0)
                    {
                        object value = p.GetValue(variable, null);

                        if (value == null)
                            continue;

                        var converter = GetConverterForType(p.PropertyType);

                        if (converter != null)
                        {
                            children.Add(new XAttribute(p.Name, value));
                        }
                        else if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            children.Add(SaveList(p.Name, value));
                        }
                        else if (p.PropertyType.GetConstructor(Type.EmptyTypes) != null || p.PropertyType.IsValueType)
                        {
                            children.Add(SaveVariableState(p.Name, value));
                        }
                    }
                }
            }
            catch (Exception)
            {
                children.Clear();
            }

            return new XElement(name, children);
        }
    }
}
