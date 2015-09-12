//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class Theme : DependencyObject
    {
        static Dictionary<Type, Dictionary<string, DependencyProperty>> themePropertiesTable = new Dictionary<Type, Dictionary<string, DependencyProperty>>();

        public static ThemeRoot Instance { get; private set; }

        public static readonly DependencyProperty ThemeNameProperty = DependencyProperty.Register(
            "ThemeName", typeof(string), typeof(Theme), new FrameworkPropertyMetadata(""));

        public string ThemeName
        {
            get { return (string)GetValue(ThemeNameProperty); }
            set { SetValue(ThemeNameProperty, value); }
        }

        public ObservableCollection<PaletteColor> Palette { get; private set; }

        static Theme()
        {
            Instance = new ThemeRoot();
        }

        public Theme()
        {
            int themeNumber = 1;
            string untitledThemeName;

            do
            {
                untitledThemeName = string.Format("Untitled Theme {0}", themeNumber++);
            }
            while (Instance.Themes.Any(t => t.ThemeName == untitledThemeName));

            this.ThemeName = untitledThemeName;
            this.Palette = new ObservableCollection<PaletteColor>();
        }

        public DependencyProperty LookupThemeProperty(string name)
        {
            var table = GetThemePropertyTable(this);
            DependencyProperty dp = null;

            table.TryGetValue(name, out dp);
            return dp;
        }

        static Dictionary<string, DependencyProperty> GetThemePropertyTable(Theme theme)
        {
            Dictionary<string, DependencyProperty> table;

            if (!themePropertiesTable.TryGetValue(theme.GetType(), out table))
            {
                table = theme.GetType().GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Select(fi => fi.GetValue(null) as DependencyProperty)
                    .ToDictionary(dp => dp.Name);
                themePropertiesTable.Add(theme.GetType(), table);
            }

            return table;
        }

        public static Binding CreateBinding(string propertyName)
        {
            return new Binding { Source = Instance, Path = new PropertyPath("Theme." + propertyName) };
        }
    }

    public class ThemeRoot : DependencyObject
    {
        public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(
            "Theme", typeof(Theme), typeof(ThemeRoot), new FrameworkPropertyMetadata(OnThemeChanged));

        public Theme Theme
        {
            get { return (Theme)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        public Func<Theme> ThemeCreator { get; set; }

        public ThemeRoot()
        {
            this.Themes = new ObservableCollection<Theme>();
            this.ThemeCreator = () => new BaseTheme();
        }

        public ObservableCollection<Theme> Themes { get; private set; }

        static void OnThemeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ThemeRoot root = obj as ThemeRoot;

            if (root != null)
            {
                var handler = root.ThemeChanged;

                if (handler != null)
                {
                    handler(root, EventArgs.Empty);
                }
            }
        }

        public event EventHandler ThemeChanged;
    }

    public class PaletteColor : DependencyObject
    {
        public static readonly DependencyProperty NameProperty = DependencyProperty.Register(
            "Name", typeof(string), typeof(PaletteColor));

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(Color), typeof(PaletteColor));

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }
    }
}
