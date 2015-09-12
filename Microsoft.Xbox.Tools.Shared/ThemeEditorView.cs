//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ThemeEditorView : Control
    {
        public static readonly DependencyProperty ThemeObjectProperty = DependencyProperty.Register(
            "ThemeObject", typeof(DependencyObject), typeof(ThemeEditorView), new FrameworkPropertyMetadata(OnThemeObjectChanged));

        public static readonly DependencyProperty ThemePropertiesProperty = DependencyProperty.Register(
            "ThemeProperties", typeof(IEnumerable), typeof(ThemeEditorView));

        public static readonly DependencyProperty ThemePropertySourceProperty = DependencyProperty.Register(
            "ThemePropertySource", typeof(CollectionViewSource), typeof(ThemeEditorView));

        public static readonly DependencyProperty EditorProperty = DependencyProperty.Register(
            "Editor", typeof(PropertyEditor), typeof(ThemeEditorView));

        public static readonly RoutedCommand AddThemeCommand = new RoutedCommand("AddTheme", typeof(ThemeEditorView));
        public static readonly RoutedCommand DeleteThemeCommand = new RoutedCommand("DeleteTheme", typeof(ThemeEditorView));

        ListBox propertyList;
        TextBox filterBox;
        Dictionary<Type, PropertyEditor> editorTable;
        Dictionary<Type, TypeConverter> converterTable;
        List<ThemeProperty> selectedProperties;
        PropertyEditor nothingSelectedEditor;
        PropertyEditor incompatibleTypesEditor;

        public static DataTemplate ColorSwatchTemplate;
        public static DataTemplate TextSwatchTemplate;

        public DependencyObject ThemeObject
        {
            get { return (DependencyObject)GetValue(ThemeObjectProperty); }
            set { SetValue(ThemeObjectProperty, value); }
        }

        public IEnumerable ThemeProperties
        {
            get { return (IEnumerable)GetValue(ThemePropertiesProperty); }
            set { SetValue(ThemePropertiesProperty, value); }
        }

        public PropertyEditor Editor
        {
            get { return (PropertyEditor)GetValue(EditorProperty); }
            set { SetValue(EditorProperty, value); }
        }

        public CollectionViewSource ThemePropertySource
        {
            get { return (CollectionViewSource)GetValue(ThemePropertySourceProperty); }
            set { SetValue(ThemePropertySourceProperty, value); }
        }

        public ThemeEditorView()
        {
            this.ThemeProperties = new ObservableCollection<string>();
            this.editorTable = new Dictionary<Type, PropertyEditor>();
            this.converterTable = new Dictionary<Type, TypeConverter>();
            this.incompatibleTypesEditor = new MessagePropertyEditor("The properties you have selected are not all of the same type.");
            this.nothingSelectedEditor = new MessagePropertyEditor("Select a property to edit.");

            this.SetBinding(ThemeObjectProperty, new Binding { Source = Theme.Instance, Path = new PropertyPath("Theme") });

            // Custom editors go here; everything else will get a basic text editor.
            this.editorTable.Add(typeof(Color), new ColorPropertyEditor());

            if (ColorSwatchTemplate == null)
            {
                ColorSwatchTemplate = this.FindResource("ColorSwatchTemplate") as DataTemplate;
                TextSwatchTemplate = this.FindResource("TextSwatchTemplate") as DataTemplate;
            }

            this.CommandBindings.Add(new CommandBinding(AddThemeCommand, OnAddThemeExecuted));
            this.CommandBindings.Add(new CommandBinding(DeleteThemeCommand, OnDeleteThemeExecuted, OnDeleteThemeCanExecute));
            this.Editor = this.nothingSelectedEditor;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.propertyList = GetTemplateChild("PART_PropertyList") as ListBox;
            this.filterBox = GetTemplateChild("PART_FilterBox") as TextBox;

            if (this.propertyList != null)
            {
                this.propertyList.SelectionChanged += OnPropertyListSelectionChanged;
            }

            if (this.filterBox != null)
            {
                this.filterBox.TextChanged += OnFilterBoxTextChanged;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (this.filterBox != null && e.Key == Key.Escape && (this.filterBox.IsKeyboardFocusWithin || this.propertyList.IsKeyboardFocusWithin))
            {
                this.filterBox.Text = string.Empty;
            }

            base.OnPreviewKeyDown(e);
        }

        void OnAddThemeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var newTheme = Theme.Instance.ThemeCreator();
            Theme.Instance.Themes.Add(newTheme);
            Theme.Instance.Theme = newTheme;
        }

        void OnDeleteThemeExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (Theme.Instance.Themes.Count > 1)
            {
                Theme.Instance.Themes.Remove(Theme.Instance.Theme);
                Theme.Instance.Theme = Theme.Instance.Themes[0];
            }
        }

        void OnDeleteThemeCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Theme.Instance.Themes.Count > 1;
            e.Handled = true;
        }

        void OnFilterBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.ThemePropertySource != null && this.ThemePropertySource.View != null)
            {
                this.ThemePropertySource.View.Refresh();
            }
        }

        void OnEditorValueChanged(object sender, EventArgs e)
        {
            object valueToPush = this.Editor.Value;
            Type typeToPush = this.selectedProperties[0].PropertyInfo.PropertyType;

            try
            {
                if (typeToPush == typeof(Color) && valueToPush is PaletteColor)
                {
                    foreach (var tp in this.selectedProperties)
                    {
                        var dp = Theme.Instance.Theme.LookupThemeProperty(tp.PropertyInfo.Name);
                        BindingOperations.SetBinding(this.ThemeObject, dp, new Binding
                        {
                            Source = valueToPush,
                            Path = new PropertyPath(PaletteColor.ColorProperty)
                        });
                        tp.PaletteItemName = ((PaletteColor)valueToPush).Name;
                    }
                }
                else
                {
                    if (valueToPush != null)
                    {
                        if (valueToPush.GetType() != typeToPush)
                        {
                            TypeConverter converter;

                            if (!this.converterTable.TryGetValue(typeToPush, out converter))
                            {
                                converter = TypeDescriptor.GetConverter(typeToPush);
                                this.converterTable[typeToPush] = converter;
                            }

                            valueToPush = converter.ConvertFrom(valueToPush);
                        }
                    }

                    foreach (var tp in this.selectedProperties)
                    {
                        var dp = Theme.Instance.Theme.LookupThemeProperty(tp.PropertyInfo.Name);
                        var b = BindingOperations.GetBinding(Theme.Instance.Theme, dp);

                        if (b != null)
                        {
                            BindingOperations.ClearBinding(this.ThemeObject, dp);
                        }

                        tp.PropertyInfo.SetValue(this.ThemeObject, valueToPush, null);
                        tp.PaletteItemName = string.Empty;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        void OnPropertyListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.selectedProperties = this.propertyList.SelectedItems.OfType<ThemeProperty>().ToList();

            if (this.Editor != null)
            {
                this.Editor.ValueChanged -= OnEditorValueChanged;
            }

            PropertyEditor editor;

            if (this.selectedProperties.Count == 0)
            {
                editor = this.nothingSelectedEditor;
            }
            else if (this.selectedProperties.Any(p => p.PropertyInfo.PropertyType != this.selectedProperties[0].PropertyInfo.PropertyType))
            {
                editor = this.incompatibleTypesEditor;
            }
            else
            {
                if (!this.editorTable.TryGetValue(this.selectedProperties[0].PropertyInfo.PropertyType, out editor))
                {
                    editor = new GenericPropertyEditor();
                    this.editorTable[this.selectedProperties[0].PropertyInfo.PropertyType] = editor;
                }
            }

            this.Editor = editor;

            if (this.selectedProperties.Count == 1)
            {
                object value = null;

                // Only set the editor's value if this is the only property selected
                if (this.selectedProperties[0].PropertyInfo.PropertyType == typeof(Color))
                {
                    var dp = Theme.Instance.Theme.LookupThemeProperty(this.selectedProperties[0].PropertyInfo.Name);
                    var binding = BindingOperations.GetBinding(Theme.Instance.Theme, dp);

                    if (binding != null && binding.Source is PaletteColor)
                    {
                        value = binding.Source;
                    }
                }

                if (value == null)
                {
                    value = this.selectedProperties[0].PropertyInfo.GetValue(this.ThemeObject, null);
                }

                editor.Value = value;
            }

            this.Editor.ValueChanged += OnEditorValueChanged;

        }

        void RebuildThemePropertyList()
        {
            if (this.ThemeObject != null)
            {
                var props = this.ThemeObject.GetType().GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance)
                    .Where(pi => pi.GetCustomAttributes(typeof(ThemePropertyAttribute), false).Length > 0)
                    .Select(pi => new ThemeProperty(pi, ((ThemePropertyAttribute)pi.GetCustomAttributes(typeof(ThemePropertyAttribute), false)[0]).Category))
                    .ToList();
                this.ThemeProperties = props;

                foreach (var tp in props)
                {
                    var binding = BindingOperations.GetBinding(this.ThemeObject, Theme.Instance.Theme.LookupThemeProperty(tp.PropertyInfo.Name));

                    if (binding != null && binding.Source is PaletteColor)
                    {
                        tp.PaletteItemName = ((PaletteColor)binding.Source).Name;
                    }
                }
            }
            else
            {
                this.ThemeProperties = Enumerable.Empty<ThemeProperty>();
            }
            this.ThemePropertySource = new CollectionViewSource() { Source = this.ThemeProperties };
            this.ThemePropertySource.SortDescriptions.Add(new SortDescription { PropertyName = "Category" });
            this.ThemePropertySource.SortDescriptions.Add(new SortDescription { PropertyName = "Name" });
            this.ThemePropertySource.GroupDescriptions.Add(new PropertyGroupDescription { PropertyName = "Category" });
            this.ThemePropertySource.Filter += OnThemePropertySourceFilter;
        }

        void OnThemePropertySourceFilter(object sender, FilterEventArgs e)
        {
            var prop = e.Item as ThemeProperty;

            if (prop == null || this.filterBox == null || string.IsNullOrEmpty(this.filterBox.Text))
            {
                e.Accepted = true;
                return;
            }

            e.Accepted = prop.Name.ToLower().Contains(this.filterBox.Text.ToLower());
        }

        static void OnThemeObjectChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ThemeEditorView view = obj as ThemeEditorView;

            if (view != null)
            {
                if (view.ThemeObject == null && Theme.Instance.Theme != null)
                {
                    view.ThemeObject = Theme.Instance.Theme;
                }

                view.RebuildThemePropertyList();
            }
        }

    }

    [ViewFactory("Microsoft.Xbox.Tools.Shared.ThemeEditorView", IsInternalOnly = true)]
    public class ThemeEditorViewFactory : IViewFactory
    {
        public string GetViewDisplayName(string registeredViewName)
        {
            return "Theme Editor";
        }

        public object CreateView(string registeredViewName, IServiceProvider serviceProvider)
        {
            return new ThemeEditorView();
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ThemePropertyAttribute : Attribute
    {
        public string Category { get; set; }

        public ThemePropertyAttribute()
        {
            this.Category = "General";
        }

        public ThemePropertyAttribute(string category)
        {
            this.Category = category;
        }
    }

    public class ThemeProperty : DependencyObject
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(object), typeof(ThemeProperty));

        public static readonly DependencyProperty PaletteItemNameProperty = DependencyProperty.Register(
            "PaletteItemName", typeof(string), typeof(ThemeProperty));

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public ThemeProperty(PropertyInfo pi, string category)
        {
            this.PropertyInfo = pi;
            this.Category = category;
            BindingOperations.SetBinding(this, ValueProperty, new Binding { Source = Theme.Instance, Path = new PropertyPath("Theme." + pi.Name) });
        }

        public DependencyObject ThemeObject { get { return Theme.Instance.Theme; } }
        public PropertyInfo PropertyInfo { get; private set; }
        public string Name { get { return PropertyInfo.Name; } }
        public string Type { get { return PropertyInfo.PropertyType.Name; } }
        public string Category { get; private set; }

        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public string PaletteItemName
        {
            get { return (string)GetValue(PaletteItemNameProperty); }
            set { SetValue(PaletteItemNameProperty, value); }
        }

        public DataTemplate SwatchTemplate
        {
            get
            {
                if (this.PropertyInfo.PropertyType == typeof(Color))
                {
                    return ThemeEditorView.ColorSwatchTemplate;
                }

                return ThemeEditorView.TextSwatchTemplate;
            }
        }
    }

    public class PropertyEditor : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(object), typeof(PropertyEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public event EventHandler ValueChanged;

        protected virtual void OnValueChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        static void OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            PropertyEditor editor = obj as PropertyEditor;

            if (editor != null)
            {
                editor.OnValueChanged(e);

                var handler = editor.ValueChanged;

                if (handler != null)
                {
                    handler(editor, EventArgs.Empty);
                }
            }
        }
    }

    public class GenericPropertyEditor : PropertyEditor
    {
    }

    public class MessagePropertyEditor : PropertyEditor
    {
        public string Message { get; private set; }

        public MessagePropertyEditor(string message)
        {
            this.Message = message;
        }
    }

    public class ColorPropertyEditor : PropertyEditor
    {
        public static readonly RoutedCommand NewPaletteColorCommand = new RoutedCommand("NewPaletteColor", typeof(ColorPropertyEditor));

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(Color), typeof(ColorPropertyEditor), new FrameworkPropertyMetadata(OnColorChanged));

        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            "Brush", typeof(Brush), typeof(ColorPropertyEditor));

        public static readonly DependencyProperty RProperty = DependencyProperty.Register(
            "R", typeof(byte), typeof(ColorPropertyEditor), new FrameworkPropertyMetadata(OnComponentChanged));

        public static readonly DependencyProperty GProperty = DependencyProperty.Register(
            "G", typeof(byte), typeof(ColorPropertyEditor), new FrameworkPropertyMetadata(OnComponentChanged));

        public static readonly DependencyProperty BProperty = DependencyProperty.Register(
            "B", typeof(byte), typeof(ColorPropertyEditor), new FrameworkPropertyMetadata(OnComponentChanged));

        public static readonly DependencyProperty AProperty = DependencyProperty.Register(
            "A", typeof(byte), typeof(ColorPropertyEditor), new FrameworkPropertyMetadata(OnComponentChanged));

        public static readonly DependencyProperty GrayScaleProperty = DependencyProperty.Register(
            "GrayScale", typeof(byte), typeof(ColorPropertyEditor), new FrameworkPropertyMetadata(OnGrayScaleChanged));

        public static readonly DependencyProperty SelectedPaletteColorProperty = DependencyProperty.Register(
            "SelectedPaletteColor", typeof(PaletteColor), typeof(ColorPropertyEditor), new FrameworkPropertyMetadata(OnSelectedPaletteColorChanged));

        bool autoSettingComponents;
        bool autoSettingColor;
        bool autoSettingValue;

        public ColorPropertyEditor()
        {
            this.CommandBindings.Add(new CommandBinding(NewPaletteColorCommand, OnNewPaletteColorExecuted));
        }

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public Brush Brush
        {
            get { return (Brush)GetValue(BrushProperty); }
            set { SetValue(BrushProperty, value); }
        }

        public byte R
        {
            get { return (byte)GetValue(RProperty); }
            set { SetValue(RProperty, value); }
        }

        public byte G
        {
            get { return (byte)GetValue(GProperty); }
            set { SetValue(GProperty, value); }
        }

        public byte B
        {
            get { return (byte)GetValue(BProperty); }
            set { SetValue(BProperty, value); }
        }

        public byte A
        {
            get { return (byte)GetValue(AProperty); }
            set { SetValue(AProperty, value); }
        }

        public byte GrayScale
        {
            get { return (byte)GetValue(GrayScaleProperty); }
            set { SetValue(GrayScaleProperty, value); }
        }

        public PaletteColor SelectedPaletteColor
        {
            get { return (PaletteColor)GetValue(SelectedPaletteColorProperty); }
            set { SetValue(SelectedPaletteColorProperty, value); }
        }

        void OnNewPaletteColorExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int colorNumber = 1;
            string untitledColorName;

            do
            {
                untitledColorName = string.Format("PC{0}", colorNumber++);
            }
            while (Theme.Instance.Theme.Palette.Any(t => t.Name == untitledColorName));

            Theme.Instance.Theme.Palette.Add(new PaletteColor { Name = untitledColorName, Color = this.Color });
        }

        protected override void OnValueChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!this.autoSettingValue)
            {
                if (e.NewValue is Color)
                {
                    this.SelectedPaletteColor = null;
                    this.Color = (Color)e.NewValue;
                }
                else if (this.Value is PaletteColor)
                {
                    this.SelectedPaletteColor = (PaletteColor)this.Value;
                }
            }
        }

        void AutoSetValue()
        {
            this.autoSettingValue = true;
            try
            {
                if (this.SelectedPaletteColor != null)
                {
                    this.Value = this.SelectedPaletteColor;
                    this.SelectedPaletteColor.Color = this.Color;
                }
                else
                {
                    this.Value = this.Color;
                }
            }
            finally
            {
                this.autoSettingValue = false;
            }
        }

        static void OnColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ColorPropertyEditor editor = obj as ColorPropertyEditor;

            if (editor != null)
            {
                editor.Brush = new SolidColorBrush(editor.Color);
                editor.AutoSetValue();

                if (!editor.autoSettingColor)
                {
                    editor.autoSettingComponents = true;
                    editor.R = editor.Color.R;
                    editor.G = editor.Color.G;
                    editor.B = editor.Color.B;
                    editor.A = editor.Color.A;
                    editor.GrayScale = (byte)(((int)editor.R + editor.G + editor.B) / 3);
                    editor.autoSettingComponents = false;
                }
            }
        }

        static void OnComponentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ColorPropertyEditor editor = obj as ColorPropertyEditor;

            if (editor != null)
            {
                if (!editor.autoSettingComponents)
                {
                    editor.autoSettingColor = true;
                    editor.Color = Color.FromArgb(editor.A, editor.R, editor.G, editor.B);
                    editor.autoSettingColor = false;

                    editor.autoSettingComponents = true;
                    editor.GrayScale = (byte)(((int)editor.R + editor.G + editor.B) / 3);
                    editor.autoSettingComponents = false;
                }
            }
        }

        static void OnGrayScaleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ColorPropertyEditor editor = obj as ColorPropertyEditor;

            if (editor != null)
            {
                if (!editor.autoSettingComponents)
                {
                    editor.autoSettingColor = true;
                    editor.Color = Color.FromArgb(editor.A, editor.GrayScale, editor.GrayScale, editor.GrayScale);
                    editor.autoSettingColor = false;

                    editor.autoSettingComponents = true;
                    editor.R = editor.GrayScale;
                    editor.G = editor.GrayScale;
                    editor.B = editor.GrayScale;
                    editor.autoSettingComponents = false;
                }
            }
        }

        static void OnSelectedPaletteColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ColorPropertyEditor editor = obj as ColorPropertyEditor;

            if (editor != null)
            {
                if (editor.SelectedPaletteColor == null || editor.Color == editor.SelectedPaletteColor.Color)
                {
                    // Color already matches; must auto-set to ensure our value becomes the palette color object
                    editor.AutoSetValue();
                }
                else
                {
                    // Since the color needs to change, let it auto-set the value
                    editor.Color = editor.SelectedPaletteColor.Color;
                }
            }
        }
    }

    public class PaletteColorListBox : ListBox
    {
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            var listBoxItem = element as ListBoxItem;

            if (listBoxItem != null)
            {
                listBoxItem.PreviewMouseDown += OnItemPreviewMouseDown;
            }
        }

        void OnItemPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;

            if (item != null)
            {
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                    e.Handled = true;
                    return;
                }
            }
        }

    }
}
