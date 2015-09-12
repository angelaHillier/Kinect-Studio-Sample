//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class HitTextBlock : Control
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));

        public static readonly DependencyProperty HitTextProperty = DependencyProperty.Register(
            "HitText", typeof(string), typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));

        public static readonly DependencyProperty HitTextForegroundProperty = DependencyProperty.Register(
            "HitTextForeground", typeof(Brush), typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));

        public static readonly DependencyProperty HitTextBackgroundProperty = DependencyProperty.Register(
            "HitTextBackground", typeof(Brush), typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));

        public static readonly DependencyProperty HitTextFontWeightProperty = DependencyProperty.Register(
            "HitTextFontWeight", typeof(FontWeight), typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));

        public static readonly DependencyProperty HitTextFontStyleProperty = DependencyProperty.Register(
            "HitTextFontStyle", typeof(FontStyle), typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public string HitText
        {
            get { return (string)GetValue(HitTextProperty); }
            set { SetValue(HitTextProperty, value); }
        }

        public Brush HitTextForeground
        {
            get { return (Brush)GetValue(HitTextForegroundProperty); }
            set { SetValue(HitTextForegroundProperty, value); }
        }

        public Brush HitTextBackground
        {
            get { return (Brush)GetValue(HitTextBackgroundProperty); }
            set { SetValue(HitTextBackgroundProperty, value); }
        }

        public FontWeight HitTextFontWeight
        {
            get { return (FontWeight)GetValue(HitTextFontWeightProperty); }
            set { SetValue(HitTextFontWeightProperty, value); }
        }

        public FontStyle HitTextFontStyle
        {
            get { return (FontStyle)GetValue(HitTextFontStyleProperty); }
            set { SetValue(HitTextFontStyleProperty, value); }
        }

        FormattedText formattedText;
        Typeface typeface;
        List<Hit> hits = new List<Hit>();

        static HitTextBlock()
        {
            FontFamilyProperty.OverrideMetadata(typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));
            FontSizeProperty.OverrideMetadata(typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));
            FontWeightProperty.OverrideMetadata(typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));
            FontStyleProperty.OverrideMetadata(typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));
            ForegroundProperty.OverrideMetadata(typeof(HitTextBlock), new FrameworkPropertyMetadata(OnTextAffectingPropertyChanged));
        }

        void RecreateFormattedText()
        {
            string text = this.Text ?? string.Empty;

            this.typeface = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch);
            this.formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, this.typeface, this.FontSize, this.Foreground);

            this.hits.Clear();
            if (!string.IsNullOrEmpty(this.HitText))
            {
                int hitLength = this.HitText.Length;
                for (int i = 0; i < text.Length; )
                {
                    int index = text.IndexOf(this.HitText, i, StringComparison.CurrentCultureIgnoreCase);

                    if (index != -1)
                    {
                        this.formattedText.SetForegroundBrush(this.HitTextForeground, index, hitLength);
                        this.formattedText.SetFontWeight(this.HitTextFontWeight, index, hitLength);
                        this.formattedText.SetFontStyle(this.HitTextFontStyle, index, hitLength);
                        this.hits.Add(new Hit { StartIndex = index, Count = hitLength });
                        i = index + hitLength;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            this.InvalidateMeasure();
            this.InvalidateVisual();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (this.formattedText == null)
            {
                RecreateFormattedText();
            }

            this.formattedText.MaxTextWidth = double.IsInfinity(constraint.Width) ? 0 : constraint.Width;
            return new Size(this.formattedText.Width, this.formattedText.Height);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.formattedText == null)
            {
                return;
            }

            foreach (var hit in this.hits)
            {
                var g = this.formattedText.BuildHighlightGeometry(new Point(0, 0), hit.StartIndex, hit.Count);
                drawingContext.DrawGeometry(this.HitTextBackground ?? this.Background, null, g);
            }

            drawingContext.DrawText(this.formattedText, new Point(0, 0));
        }

        static void OnTextAffectingPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            HitTextBlock block = obj as HitTextBlock;

            if (block != null)
            {
                block.RecreateFormattedText();
            }
        }

        struct Hit
        {
            public int StartIndex { get; set; }
            public int Count { get; set; }
        }
    }
}
