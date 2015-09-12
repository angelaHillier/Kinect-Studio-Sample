//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared
{
    public class LayoutTabPanel : Panel
    {
        public static readonly DependencyProperty TabSpacingProperty = DependencyProperty.Register(
            "TabSpacing", typeof(double), typeof(LayoutTabPanel), new FrameworkPropertyMetadata(3d));

        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
            "SelectedIndex", typeof(int), typeof(LayoutTabPanel), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register(
            "BorderBrush", typeof(Brush), typeof(LayoutTabPanel), new FrameworkPropertyMetadata(Brushes.Gray));

        public static readonly DependencyProperty SelectedItemBackgroundProperty = DependencyProperty.Register(
            "SelectedItemBackground", typeof(Brush), typeof(LayoutTabPanel), new FrameworkPropertyMetadata(Brushes.White));

        public static readonly DependencyProperty DocumentTypeAffinityMarginProperty = DependencyProperty.Register(
            "DocumentTypeAffinityMargin", typeof(Thickness), typeof(LayoutTabPanel), new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DocumentTypeAffinityLineThicknessProperty = DependencyProperty.Register(
            "DocumentTypeAffinityLineThickness", typeof(double), typeof(LayoutTabPanel), new FrameworkPropertyMetadata(2d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DocumentTypeAffinityFadeProperty = DependencyProperty.Register(
            "DocumentTypeAffinityFade", typeof(double), typeof(LayoutTabPanel), new FrameworkPropertyMetadata(0.8d, FrameworkPropertyMetadataOptions.AffectsRender));

        public double DocumentTypeAffinityOpacity
        {
            get { return (double)GetValue(DocumentTypeAffinityFadeProperty); }
            set { SetValue(DocumentTypeAffinityFadeProperty, value); }
        }
        
        public double TabSpacing
        {
            get { return (double)GetValue(TabSpacingProperty); }
            set { SetValue(TabSpacingProperty, value); }
        }

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        public Brush SelectedItemBackground
        {
            get { return (Brush)GetValue(SelectedItemBackgroundProperty); }
            set { SetValue(SelectedItemBackgroundProperty, value); }
        }

        public Thickness DocumentTypeAffinityMargin
        {
            get { return (Thickness)GetValue(DocumentTypeAffinityMarginProperty); }
            set { SetValue(DocumentTypeAffinityMarginProperty, value); }
        }

        public double DocumentTypeAffinityLineThickness
        {
            get { return (double)GetValue(DocumentTypeAffinityLineThicknessProperty); }
            set { SetValue(DocumentTypeAffinityLineThicknessProperty, value); }
        }

        Pen borderPen;
        PathGeometry geometry;
        PathFigure figure;
        double affinitizedTabStartX;
        double affinitizedTabEndX;
        SolidColorBrush solidAffinitizedBrush;
        SolidColorBrush lightAffinitizedBrush;
        FormattedText docTypeText;
        Binding fadeBinding;

        public LayoutTabPanel()
        {
            this.borderPen = new Pen();
            this.borderPen.Thickness = 1;
            BindingOperations.SetBinding(this.borderPen, Pen.BrushProperty, new Binding { Source = this, Path = new PropertyPath(BorderBrushProperty) });
            this.geometry = new PathGeometry();
            this.figure = new PathFigure() { IsClosed = true, IsFilled = true };
            this.geometry.Figures.Add(this.figure);
            this.solidAffinitizedBrush = new SolidColorBrush();
            this.lightAffinitizedBrush = new SolidColorBrush();
            this.SetBinding(DocumentTypeAffinityMarginProperty, Theme.CreateBinding("DocumentAffinityMargin"));
            this.SetBinding(DocumentTypeAffinityLineThicknessProperty, Theme.CreateBinding("DocumentAffinityLineThickness"));
            this.fadeBinding = Theme.CreateBinding("DocumentAffinityFade");
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double totalWidth = this.TabSpacing * 2;
            double maxHeight = 2;

            this.docTypeText = null;

            foreach (var child in this.InternalChildren.OfType<TabItem>())
            {
                if (IsTabItemVisible(child))
                {
                    var layoutDef = LayoutDefinitionFromTabItem(child);

                    if (layoutDef != null && layoutDef.DocumentFactoryName != null)
                    {
                        var category = ToolsUIApplication.Instance.DocumentCategories.FirstOrDefault(c => c.DocumentFactoryName == layoutDef.DocumentFactoryName);

                        if (category != null)
                        {
                            var colorBinding = new Binding { Source = category, Path = new PropertyPath(DocumentCategory.ColorProperty) };

                            BindingOperations.SetBinding(this.solidAffinitizedBrush, SolidColorBrush.ColorProperty, colorBinding);

                            var lightBinding = new MultiBinding { Converter = FadeConverter.Instance };

                            lightBinding.Bindings.Add(colorBinding);
                            lightBinding.Bindings.Add(this.fadeBinding);
                            BindingOperations.SetBinding(this.lightAffinitizedBrush, SolidColorBrush.ColorProperty, lightBinding);

                            this.docTypeText = new FormattedText(category.DisplayName, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("SmallCaption"), 10, this.solidAffinitizedBrush);
                        }
                        else
                        {
                            BindingOperations.ClearBinding(this.solidAffinitizedBrush, SolidColorBrush.ColorProperty);
                            BindingOperations.ClearBinding(this.lightAffinitizedBrush, SolidColorBrush.ColorProperty);
                            solidAffinitizedBrush.Color = Colors.Gray;
                            lightAffinitizedBrush.Color = Colors.Gray;
                        }
                    }

                    child.Measure(availableSize);
                    totalWidth += child.DesiredSize.Width + this.TabSpacing;
                    maxHeight = Math.Max(maxHeight, child.DesiredSize.Height + 2);
                }
            }

            if (this.docTypeText != null)
            {
                totalWidth += this.docTypeText.Width + this.DocumentTypeAffinityMargin.Left + this.DocumentTypeAffinityMargin.Right;
            }

            return new Size(totalWidth, maxHeight + this.DocumentTypeAffinityLineThickness);
        }

        bool IsTabItemVisible(TabItem tabItem)
        {
            return tabItem.Visibility == Visibility.Visible;
        }

        LayoutDefinition LayoutDefinitionFromTabItem(TabItem tabItem)
        {
            var instance = tabItem.DataContext as LayoutInstance;

            if (instance != null)
            {
                return instance.LayoutDefinition;
            }

            return tabItem.DataContext as LayoutDefinition;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double runningX = this.TabSpacing * 2;
            double bottom = Math.Floor(finalSize.Height) + 0.5;
            bool foundAffinitizedTab = false;

            this.figure.StartPoint = new Point(0, bottom);
            this.figure.Segments.Clear();
            this.affinitizedTabStartX = double.NaN;

            foreach (var child in this.InternalChildren.OfType<TabItem>())
            {
                if (child.IsSelected)
                {
                    if (IsTabItemVisible(child))
                    {
                        double left = Math.Floor(runningX) - 0.5;
                        double right = Math.Floor(runningX + child.DesiredSize.Width + this.TabSpacing) + 0.5;
                        double top = Math.Floor(this.DocumentTypeAffinityLineThickness) + 0.5;
                        this.figure.Segments.Add(new LineSegment(new Point(left, bottom), true));
                        this.figure.Segments.Add(new LineSegment(new Point(left, top), true));
                        this.figure.Segments.Add(new LineSegment(new Point(right, top), true));
                        this.figure.Segments.Add(new LineSegment(new Point(right, bottom), true));
                    }
                    else
                    {
                        // We try to prevent this from happening by changing the selected item as appropriate when layout
                        // visibilities change, but this code is here to prevent the (bad) result of us missing a spot --
                        // if a hidden tab is selected, the tab won't render but its content will. This can be horribly confusing,
                        // so we post a suggestion to the tab control to select something visible.
                        this.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            var ctrl = this.FindParent<LayoutTabControl>();

                            if (ctrl != null)
                            {
                                ctrl.EnsureVisibleLayoutSelected();
                            }
                        }), DispatcherPriority.Background);
                    }
                }

                if (IsTabItemVisible(child))
                {
                    if (!foundAffinitizedTab)
                    {
                        var layoutDef = LayoutDefinitionFromTabItem(child);

                        if (layoutDef != null && layoutDef.DocumentFactoryName != null)
                        {
                            foundAffinitizedTab = true;
                            this.affinitizedTabStartX = Math.Floor(runningX) - 0.5;
                        }
                    }

                    child.Arrange(new Rect(runningX + (this.TabSpacing / 2), this.DocumentTypeAffinityLineThickness + 1, child.DesiredSize.Width, child.DesiredSize.Height));
                    runningX += child.DesiredSize.Width + this.TabSpacing;
                    this.affinitizedTabEndX = Math.Floor(runningX) + 0.5;
                }
            }

            this.figure.Segments.Add(new LineSegment(new Point(finalSize.Width, bottom), true));
            this.figure.Segments.Add(new LineSegment(new Point(0, bottom), false));
            return finalSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (!double.IsNaN(this.affinitizedTabStartX))
            {
                // Right justify (there is probably a better solution)
                this.affinitizedTabEndX = this.ActualWidth - (this.docTypeText.Width + this.DocumentTypeAffinityMargin.Left + this.DocumentTypeAffinityMargin.Right);

                var textPoint = new Point(this.affinitizedTabEndX + this.DocumentTypeAffinityMargin.Left, this.DocumentTypeAffinityLineThickness + this.DocumentTypeAffinityMargin.Top);

                if (this.docTypeText != null)
                {
                    this.affinitizedTabEndX += this.docTypeText.Width + this.DocumentTypeAffinityMargin.Left + this.DocumentTypeAffinityMargin.Right;
                }

                dc.DrawRectangle(this.solidAffinitizedBrush, null, new Rect(this.affinitizedTabStartX, 0, this.affinitizedTabEndX - this.affinitizedTabStartX, this.DocumentTypeAffinityLineThickness));
                dc.DrawRectangle(this.lightAffinitizedBrush, null, new Rect(this.affinitizedTabStartX, this.DocumentTypeAffinityLineThickness, this.affinitizedTabEndX - this.affinitizedTabStartX, this.ActualHeight - this.DocumentTypeAffinityLineThickness));

                if (this.docTypeText != null)
                {
                    dc.DrawText(this.docTypeText, textPoint);
                }
            }

            dc.DrawGeometry(this.SelectedItemBackground, this.borderPen, this.geometry);
        }

        class FadeConverter : IMultiValueConverter
        {
            public static FadeConverter Instance { get; private set; }

            static FadeConverter() { Instance = new FadeConverter(); }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length == 2 && values[0] is Color && values[1] is double)
                {
                    return LuminanceConverter.WashColor((Color)values[0], (float)(double)(values[1]));
                }

                return Binding.DoNothing;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
