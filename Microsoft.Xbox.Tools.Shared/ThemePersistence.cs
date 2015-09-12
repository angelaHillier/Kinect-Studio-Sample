//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public static class ThemePersistence
    {
        static string GetPropertyValueOrBinding(DependencyObject theme, DependencyProperty property)
        {
            var binding = BindingOperations.GetBinding(theme, property);

            if (binding != null && binding.Source is PaletteColor)
            {
                return string.Format("={0}", ((PaletteColor)binding.Source).Name);
            }

            return theme.GetValue(property).ToString();
        }

        static XElement CreatePropertyElement(DependencyObject theme, DependencyProperty property)
        {
            return new XElement("Property", new XAttribute("Name", property.Name), new XAttribute("Value", GetPropertyValueOrBinding(theme, property)));
        }

        public static XElement SaveTheme(Theme theme)
        {
            var themeProps = theme.GetType().GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.FieldType == typeof(DependencyProperty))
                .Select(fi => (DependencyProperty)fi.GetValue(null))
                .Where(dp => !dp.ReadOnly);

            var element = new XElement("Theme",
                new XAttribute("Name", theme.ThemeName),
                new XAttribute("Type", theme.GetType().AssemblyQualifiedName),
                themeProps.Select(dp => CreatePropertyElement(theme, dp)));

            if (theme.Palette.Count > 0)
            {
                element.Add(new XElement("Palette", theme.Palette.Select(pc => new XElement("PaletteColor", new XAttribute("Name", pc.Name), new XAttribute("Color", pc.Color)))));
            }

            return element;
        }

        public static Theme LoadTheme(XElement element)
        {
            try
            {
                var themeType = Type.GetType(element.Attribute("Type").Value);
                var theme = Activator.CreateInstance(themeType) as Theme;

                var validProps = theme.GetType().GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                    .Where(fi => fi.FieldType == typeof(DependencyProperty))
                    .Select(fi => (DependencyProperty)fi.GetValue(null))
                    .Where(dp => !dp.ReadOnly)
                    .ToDictionary(dp => dp.Name);
                var converters = new Dictionary<Type, TypeConverter>();
                var paletteElement = element.Element("Palette");
                var palette = new Dictionary<string, PaletteColor>();

                if (paletteElement != null)
                {
                    var colorConverter = TypeDescriptor.GetConverter(typeof(Color));

                    converters[typeof(Color)] = colorConverter;

                    foreach (var colorElement in paletteElement.Elements("PaletteColor"))
                    {
                        var paletteColor = new PaletteColor
                        {
                            Name = colorElement.Attribute("Name").Value,
                            Color = (Color)colorConverter.ConvertFromString(null, CultureInfo.InvariantCulture, colorElement.Attribute("Color").Value)
                        };

                        palette[paletteColor.Name] = paletteColor;
                    }

                    // NOTE:  We add the palette entries after loading them all to remove any duplicates.
                    // If the user names palette colors the same, they'll work independently until saved.
                    // When loaded, they'll be unified.  Not awesome, but better than crashing.
                    theme.Palette.Clear();
                    foreach (var pc in palette.Values)
                    {
                        theme.Palette.Add(pc);
                    }
                }

                foreach (var propElement in element.Elements("Property"))
                {
                    var nameAttr = propElement.Attribute("Name");
                    var valueAttr = propElement.Attribute("Value");

                    if (nameAttr != null)
                    {
                        DependencyProperty dp;
                        object value;

                        if (validProps.TryGetValue(nameAttr.Value, out dp))
                        {
                            if (dp.PropertyType == typeof(Color) && valueAttr.Value.StartsWith("="))
                            {
                                PaletteColor paletteColor;

                                if (palette.TryGetValue(valueAttr.Value.Substring(1), out paletteColor))
                                {
                                    BindingOperations.SetBinding(theme, dp, new Binding { Source = paletteColor, Path = new PropertyPath(PaletteColor.ColorProperty) });
                                }
                            }
                            else
                            {
                                if (dp.PropertyType == typeof(string))
                                {
                                    value = valueAttr.Value;
                                }
                                else
                                {
                                    TypeConverter converter;

                                    if (!converters.TryGetValue(dp.PropertyType, out converter))
                                    {
                                        converter = TypeDescriptor.GetConverter(dp.PropertyType);
                                        converters[dp.PropertyType] = converter;
                                    }

                                    value = converter.ConvertFromString(valueAttr.Value);
                                }

                                theme.SetValue(dp, value);
                            }
                        }
                    }
                }

                return theme;
            }
            catch (Exception)
            {
                return Theme.Instance.ThemeCreator();
            }
        }
    }
}
