//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using KinectStudioUtility;

    public abstract partial class EditValueDialog : Window, INotifyDataErrorInfo
    {
        protected EditValueDialog()
        {
            DebugHelper.AssertUIThread();

            this.Style = FindResource(this.GetType()) as Style;

            this.InitializeComponent();

            this.Loaded += EditValueDialogLoaded;
        }

        public string Prompt
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(PromptProperty) as string;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(PromptProperty, value);
            }
        }

        public string OutOfRangeErrorMessageFormat
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(OutOfRangeErrorMessageFormatProperty) as string;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(OutOfRangeErrorMessageFormatProperty, value);
            }
        }

        public Style ValueTextBoxStyle
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(ValueTextBoxStyleProperty) as Style;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(ValueTextBoxStyleProperty, value);
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            DebugHelper.AssertUIThread();

            IEnumerable value = null;

            if (this.error != null)
            {
                value = new string[] { this.error };
            }

            return value;
        }

        public bool HasErrors
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.error != null;
            }
        }

        protected abstract void TextBoxPreviewTextInput(object sender, TextCompositionEventArgs e);

        protected abstract void TextBoxTextChanged(object sender, TextChangedEventArgs e);

        protected void SetError(string value)
        {
            DebugHelper.AssertUIThread();

            if (this.error != value)
            {
                this.error = value;

                EventHandler<DataErrorsChangedEventArgs> handler = this.ErrorsChanged;
                if (handler != null)
                {
                    ErrorsChanged(this, new DataErrorsChangedEventArgs("Value"));
                }
            }
        }

        private void EditValueDialogLoaded(object sender, RoutedEventArgs e)
        {
            Window window = Owner;
            if (window != null)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;

                Point point = Mouse.PrimaryDevice.GetPosition(window);

                if ((point.X + this.Width) > window.ActualWidth)
                {
                    point.X = window.ActualWidth - this.Width;
                }

                if ((point.Y + this.Height) > window.ActualHeight)
                {
                    point.Y = window.ActualHeight - this.Height;
                }

                this.Left = point.X + window.Left;
                this.Top = point.Y + window.Top;
            }

            TextBox.SelectAll();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.DialogResult = true;

            Close();
        }

        private string error = null;

        public static readonly DependencyProperty PromptProperty = DependencyProperty.Register("Prompt", typeof(string), typeof(EditValueDialog));
        public static readonly DependencyProperty OutOfRangeErrorMessageFormatProperty = DependencyProperty.Register("OutOfRangeErrorMessageFormat", typeof(string), typeof(EditValueDialog));
        public static readonly DependencyProperty ValueTextBoxStyleProperty = DependencyProperty.Register("ValueTextBoxStyle", typeof(Style), typeof(EditValueDialog));
    }

    public abstract class EditValueDialog<T> : EditValueDialog where T : struct, IComparable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public T Value
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (T)this.GetValue(ValueProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(ValueProperty, value);
            }
        }

        public T Minimum
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (T)this.GetValue(MinimumProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(MinimumProperty, value);
            }
        }

        public T Maximum
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (T)this.GetValue(MaximumProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(MaximumProperty, value);
            }
        }

        protected abstract bool IsValidText(string text);

        protected abstract T? ParseText(string text);

        protected virtual string ConvertValueForString(T value)
        {
            return value.ToString();
        }

        protected override void TextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e != null)
            {
                e.Handled = !IsValidText(e.Text);
            }
        }

        protected override void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if ((textBox != null) && (e != null))
            {
                T? parsedValue = ParseText(textBox.Text);
                string error = null;

                if (parsedValue.HasValue)
                {
                    if ((parsedValue.Value.CompareTo(this.Maximum) > 0) || (parsedValue.Value.CompareTo(this.Minimum) < 0))
                    {
                        string format = this.OutOfRangeErrorMessageFormat;
                        if (String.IsNullOrWhiteSpace(format))
                        {
                            format = "{0} - {1}";
                        }

                        error = String.Format(CultureInfo.InvariantCulture, format, this.ConvertValueForString(this.Minimum), this.ConvertValueForString(this.Maximum));
                    }
                }
                else
                {
                    error = Strings.EditValue_Error_ValueMissing;
                }

                SetError(error);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(T), typeof(EditValueDialog<T>), new PropertyMetadata(default(T), null, CoerceValue));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(T), typeof(EditValueDialog<T>));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(T), typeof(EditValueDialog<T>));

        private static object CoerceValue(DependencyObject d, object value)
        {
            EditValueDialog<T> dialog = d as EditValueDialog<T>;

            if (dialog != null)
            {
                if (value is T)
                {
                    T typedValue = (T)value;

                    if (typedValue.CompareTo(dialog.Maximum) > 0)
                    {
                        value = dialog.Maximum;
                    }
                    else if (typedValue.CompareTo(dialog.Minimum) < 0)
                    {
                        value = dialog.Minimum;
                    }
                }
                else
                {
                    value = default(T);
                }
            }

            return value;
        }
    }

    public class EditStringDialog : EditValueDialog
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public int MaximumLength
        {
            get
            {
                DebugHelper.AssertUIThread();

                return (int)this.GetValue(MaximumLengthProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(MaximumLengthProperty, value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public string Value
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.GetValue(ValueProperty) as string;
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(ValueProperty, value);
            }
        }

        protected override void TextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
        }

        protected override void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string error = null;

            if ((textBox != null) && (e != null))
            {
                string text = textBox.Text.Trim();

                if (String.IsNullOrWhiteSpace(text))
                {
                    error = Strings.EditValue_Error_ValueMissing;
                }
                else if (text.Length > MaximumLength)
                {
                    error = String.Format(CultureInfo.CurrentCulture, Strings.EditString_Error_MaxLength, MaximumLength);
                }
            }

            SetError(error);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(string), typeof(EditStringDialog));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static readonly DependencyProperty MaximumLengthProperty = DependencyProperty.Register("MaximumLength", typeof(int), typeof(EditStringDialog));
    }
}
