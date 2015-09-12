//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using KinectStudioUtility;
    using System.Windows.Controls.Primitives;

    public partial class MetadataArrayViewerDialog : Window
    {
        public MetadataArrayViewerDialog()
        {
            InitializeComponent();

            this.DataContext = this;

            this.List.GotFocus += List_GotFocus;
            this.List.SelectionChanged += List_SelectionChanged;
            this.List.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }

        public string Key
        {
            get
            {
                return this.GetValue(MetadataArrayViewerDialog.KeyProperty) as string;
            }
            set
            {
                this.SetValue(MetadataArrayViewerDialog.KeyProperty, value);
            }
        }

        public DataTemplate ItemTemplate
        {
            get
            {
                return this.GetValue(MetadataArrayViewerDialog.ItemTemplateProperty) as DataTemplate;
            }
            set
            {
                this.SetValue(MetadataArrayViewerDialog.ItemTemplateProperty, value);
            }
        }

        public IEnumerable ItemsSource
        {
            get
            {
                return this.GetValue(MetadataArrayViewerDialog.ItemsSourceProperty) as IEnumerable;
            }
            set
            {
                this.SetValue(MetadataArrayViewerDialog.ItemsSourceProperty, value);
            }
        }

        public object DefaultValue
        {
            get
            {
                return this.GetValue(MetadataArrayViewerDialog.DefaultValueProperty);
            }
            set
            {
                this.SetValue(MetadataArrayViewerDialog.DefaultValueProperty, value);
            }
        }

        public bool IsWritable
        {
            get
            {
                return (bool)this.GetValue(MetadataArrayViewerDialog.IsWritableProperty);
            }
            set
            {
                this.SetValue(MetadataArrayViewerDialog.IsWritableProperty, value);
            }
        }

        public readonly static DependencyProperty KeyProperty = DependencyProperty.Register("Key", typeof(string), typeof(MetadataArrayViewerDialog));
        public readonly static DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(MetadataArrayViewerDialog));
        public readonly static DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(MetadataArrayViewerDialog));
        public readonly static DependencyProperty DefaultValueProperty = DependencyProperty.Register("DefaultValue", typeof(object), typeof(MetadataArrayViewerDialog));
        public readonly static DependencyProperty IsWritableProperty = DependencyProperty.Register("IsWritable", typeof(bool), typeof(MetadataArrayViewerDialog));

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e.Parameter as string) == true.ToString(CultureInfo.InvariantCulture))
            {
                this.DialogResult = true;
            }

            this.Close();
        }

        private void CloseCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = !this.IsWritable || !this.HasErrors();
            }
        }

        private void AddCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            ObservableCollection<ValueHolder> list = this.ItemsSource as ObservableCollection<ValueHolder>;
            if (list != null)
            {
                ValueHolder newItem = new ValueHolder(this.DefaultValue);

                int index = this.List.SelectedIndex;
                if (index < 0)
                {
                    list.Add(newItem);
                    this.List.SelectedIndex = list.Count - 1;
                }
                else
                {
                    list.Insert(index, newItem);
                    this.List.SelectedIndex = index;
                }
            }
        }

        private void AddCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.ItemsSource is ObservableCollection<ValueHolder>);
            }
        }

        private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            ObservableCollection<ValueHolder> list = this.ItemsSource as ObservableCollection<ValueHolder>;
            if ((list != null) && (list.Count > 0))
            {
                int index = this.List.SelectedIndex;
                if (index >= 0)
                {
                    list.RemoveAt(index);
                    if (list.Count > 0)
                    {
                        if (index < list.Count)
                        {
                            this.List.SelectedIndex = index;
                        }
                        else
                        {
                            this.List.SelectedIndex = index - 1;
                        }
                    }
                    else
                    {
                        this.List.SelectedIndex = -1;
                    }
                }
            }
        }

        private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.ItemsSource is ObservableCollection<ValueHolder>) && (this.List.Items.Count > 1) && (this.List.SelectedIndex >= 0);
            }
        }

        private void MoveUpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            ObservableCollection<ValueHolder> list = this.ItemsSource as ObservableCollection<ValueHolder>;
            if ((list != null) && (list.Count > 1))
            {
                int index = this.List.SelectedIndex;
                if (index > 0)
                {
                    ValueHolder temp = list[index];
                    list.RemoveAt(index);
                    list.Insert(index - 1, temp);
                    this.List.SelectedIndex = index - 1;
                }
            }
        }

        private void MoveUpCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.ItemsSource is ObservableCollection<ValueHolder>) && (this.List.SelectedIndex > 0);
            }
        }

        private void MoveDownCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            ObservableCollection<ValueHolder> list = this.ItemsSource as ObservableCollection<ValueHolder>;
            if ((list != null) && (list.Count > 1))
            {
                int index = this.List.SelectedIndex;
                if (index < (list.Count - 1))
                {
                    ValueHolder temp = list[index];
                    list.RemoveAt(index);
                    list.Insert(index + 1, temp);
                    this.List.SelectedIndex = index + 1;
                }
            }
        }

        private void MoveDownCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                e.CanExecute = (this.ItemsSource is ObservableCollection<ValueHolder>) && (this.List.SelectedIndex != -1) &&  (this.List.SelectedIndex < (this.List.Items.Count - 1));
            }
        }

        private void List_GotFocus(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                DependencyObject d = e.OriginalSource as DependencyObject;

                if (d != null)
                {
                    ListBoxItem item = d.GetVisualParent<ListBoxItem>() as ListBoxItem;
                    if (item != null)
                    {
                        int index = this.List.ItemContainerGenerator.IndexFromContainer(item);
                        if (this.List.SelectedIndex != index)
                        {
                            this.List.SelectedIndex = index;
                        }
                    }
                }
            }
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.List.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                int index = this.List.SelectedIndex;
                if (index >= 0)
                {
                    ListBoxItem item = this.List.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
                    if (item != null)
                    {
                        Control control = item.GetVisualChild<Control>();
                        if (control != null)
                        {
                            control.Focus();
                        }
                    }
                }
            }
        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (this.List.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                int index = this.List.SelectedIndex;
                if (index >= 0)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ListBoxItem item = this.List.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
                            if (item != null)
                            {
                                Control control = item.GetVisualChild<Control>();
                                if (control != null)
                                {
                                    control.Focus();
                                }
                            }
                        }));
                }
            }
        }
    }
}
