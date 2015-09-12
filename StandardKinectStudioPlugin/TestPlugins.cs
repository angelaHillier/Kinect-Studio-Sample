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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Input;
    using KinectStudioPlugin;
    using KinectStudioUtility;

    public class ValueHolder
    {
        public ValueHolder(object value)
        {
            this.Value = value;
        }

        public object Value { get; set; }
    }

    public class AdvancedMetadataArrayPlugin : BasePlugin, IMetadataPlugin 
    {
        public AdvancedMetadataArrayPlugin()
            : base(Strings.AdvancedMetadataArrayPlugin_Title, new Guid(0x34bfd727, 0x13c1, 0x4a42, 0xb3, 0x1b, 0x10, 0x5d, 0xa6, 0x5c, 0x22, 0xe1))
        {
            {
                CommandBinding binding = new CommandBinding(AdvancedMetadataArrayPlugin.viewArrayMetadataCommand, ViewArrayMetadataCommand_Executed, ViewArrayMetadataCommand_CanExecute);
                CommandManager.RegisterClassCommandBinding(typeof(Window), binding);
            }

            {
                CommandBinding binding = new CommandBinding(AdvancedMetadataArrayPlugin.editArrayMetadataCommand, EditArrayMetadataCommand_Executed, EditArrayMetadataCommand_CanExecute);
                CommandManager.RegisterClassCommandBinding(typeof(Window), binding);
            }

            {
                Dictionary<FileMetadataDataTemplateKey, DataTemplate> fileDataTemplates = new Dictionary<FileMetadataDataTemplateKey, DataTemplate>();
                Dictionary<StreamMetadataDataTemplateKey, DataTemplate> streamDataTemplates = new Dictionary<StreamMetadataDataTemplateKey, DataTemplate>();

                DataTemplate dataTemplate = Resources.Get("ReadOnlyAdvancedArrayMetadataValueDataTemplate") as DataTemplate;

                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Byte[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Int16[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(UInt16[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Int32[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(UInt32[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Int64[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(UInt64[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Single[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Double[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Char[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Boolean[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Guid[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(DateTime[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(TimeSpan[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Point[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Size[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Rect[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(String[]));

                this.fileReadOnlyDataTemplates = new ReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate>(fileDataTemplates);
                this.streamReadOnlyDataTemplates = new ReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate>(streamDataTemplates);
            }

            {
                Dictionary<FileMetadataDataTemplateKey, DataTemplate> fileDataTemplates = new Dictionary<FileMetadataDataTemplateKey, DataTemplate>();
                Dictionary<StreamMetadataDataTemplateKey, DataTemplate> streamDataTemplates = new Dictionary<StreamMetadataDataTemplateKey, DataTemplate>();

                DataTemplate dataTemplate = Resources.Get("WritableAdvancedArrayMetadataValueDataTemplate") as DataTemplate;

                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Byte[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Int16[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(UInt16[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Int32[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(UInt32[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Int64[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(UInt64[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Single[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Double[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Char[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Boolean[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Guid[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(DateTime[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(TimeSpan[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Point[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Size[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(Rect[]));
                AdvancedMetadataArrayPlugin.LoadDataTemplates(dataTemplate, fileDataTemplates, streamDataTemplates, typeof(String[]));

                this.fileWritableDataTemplates = new ReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate>(fileDataTemplates);
                this.streamWritableDataTemplates = new ReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate>(streamDataTemplates);
            }
        }

        public IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> FileReadOnlyDataTemplates
        {
            get 
            {
                return this.fileReadOnlyDataTemplates;
            }
        }

        public IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> FileWritableDataTemplates
        {
            get
            {
                return this.fileWritableDataTemplates;
            }
        }

        public IReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate> StreamReadOnlyDataTemplates
        {
            get
            {
                return this.streamReadOnlyDataTemplates;
            }
        }

        public IReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate> StreamWritableDataTemplates
        {
            get
            {
                return this.streamWritableDataTemplates;
            }
        }

        public static RoutedUICommand ViewArrayMetadataCommand
        {
            get
            {
                return AdvancedMetadataArrayPlugin.viewArrayMetadataCommand;
            }
        }

        public static RoutedUICommand EditArrayMetadataCommand
        {
            get
            {
                return AdvancedMetadataArrayPlugin.editArrayMetadataCommand;
            }
        }

        private void ViewArrayMetadataCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.HandleArrayMetadataCommand(sender, e, true);
        }

        private void ViewArrayMetadataCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                object[] parameters = e.Parameter as object[];

                e.CanExecute = (parameters != null) && (parameters.Length > 1) && (parameters[1] is IList);
            }
        }

        private void EditArrayMetadataCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.HandleArrayMetadataCommand(sender, e, false);
        }

        private void EditArrayMetadataCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e != null)
            {
                e.Handled = true;

                object[] parameters = e.Parameter as object[];

                e.CanExecute = (parameters != null) && (parameters.Length > 1) && (parameters[1] is IList);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void HandleArrayMetadataCommand(object sender, ExecutedRoutedEventArgs e, bool readOnly)
        {
            if (e != null)
            {
                e.Handled = true;
            }

            FrameworkElement element = sender as FrameworkElement;
            if (element != null)
            {
                Window window = Window.GetWindow(element);
                object[] parameters = e.Parameter as object[];

                if ((window != null) && (parameters != null) && (parameters.Length > 1) && (parameters[1] is IList))
                {
                    string itemTemplateKey = null;

                    string key = parameters[0] as string;
                    IList list = parameters[1] as IList;
                    object defaultValue = null;
                    WritableMetadataProxy metadata = null;

                    if (!readOnly && parameters.Length > 2)
                    {
                        metadata = parameters[2] as WritableMetadataProxy;
                    }

                    Func<ObservableCollection<ValueHolder>, Array> func = null;

                    if ((key != null) && (list != null))
                    {
                        Type listType = list.GetType();

                        if (listType.IsGenericType)
                        {
                            switch (listType.GetGenericArguments()[0].Name)
                            {
                                case "Byte":
                                    defaultValue = default(Byte);
                                    itemTemplateKey = readOnly ? "NumberMetadataValueDataTemplate" : "ByteMetadataValueDataTemplate";
                                    func = this.Convert<byte>;
                                    break;

                                case "Int16":
                                    defaultValue = default(Int16);
                                    itemTemplateKey = readOnly ? "NumberMetadataValueDataTemplate" : "Int16MetadataValueDataTemplate";
                                    func = this.Convert<Int16>;
                                    break;

                                case "UInt16":
                                    defaultValue = default(UInt16);
                                    itemTemplateKey = readOnly ? "NumberMetadataValueDataTemplate" : "UInt16MetadataValueDataTemplate";
                                    func = this.Convert<UInt16>;
                                    break;

                                case "Int32":
                                    defaultValue = default(Int32);
                                    itemTemplateKey = readOnly ? "NumberMetadataValueDataTemplate" : "Int32MetadataValueDataTemplate";
                                    func = this.Convert<Int32>;
                                    break;

                                case "UInt32":
                                    defaultValue = default(UInt32);
                                    itemTemplateKey = readOnly ? "NumberMetadataValueDataTemplate" : "UInt32MetadataValueDataTemplate";
                                    func = this.Convert<UInt32>;
                                    break;

                                case "Int64":
                                    defaultValue = default(Int64);
                                    itemTemplateKey = readOnly ? "NumberMetadataValueDataTemplate" : "Int64MetadataValueDataTemplate";
                                    func = this.Convert<Int64>;
                                    break;

                                case "UInt64":
                                    defaultValue = default(UInt64);
                                    itemTemplateKey = readOnly ? "NumberMetadataValueDataTemplate" : "UInt64MetadataValueDataTemplate";
                                    func = this.Convert<UInt64>;
                                    break;

                                case "Single":
                                    defaultValue = default(Single);
                                    itemTemplateKey = readOnly ? "NumberMetadataValueDataTemplate" : "SingleMetadataValueDataTemplate";
                                    func = this.Convert<Single>;
                                    break;

                                case "Double":
                                    defaultValue = default(Double);
                                    itemTemplateKey = readOnly ? "NumberMetadataValueDataTemplate" : "DoubleMetadataValueDataTemplate";
                                    func = this.Convert<Double>;
                                    break;

                                case "Char":
                                    defaultValue = 'X';
                                    itemTemplateKey = readOnly ? "StringMetadataValueDataTemplate" : "CharMetadataValueDataTemplate";
                                    func = this.Convert<Char>;
                                    break;

                                case "Boolean":
                                    defaultValue = default(Boolean);
                                    itemTemplateKey = "BooleanMetadataValueDataTemplate";
                                    func = this.Convert<Boolean>;
                                    break;

                                case "String":
                                    defaultValue = String.Empty;
                                    itemTemplateKey = readOnly ? "StringMetadataValueDataTemplate" : "StringMetadataValueDataTemplate";
                                    func = this.Convert<String>;
                                    break;

                                case "DateTime":
                                    defaultValue = DateTime.UtcNow.Date;
                                    itemTemplateKey = readOnly ? "StringMetadataValueDataTemplate" : "DateTimeMetadataValueDataTemplate";
                                    func = this.Convert<DateTime>;
                                    break;

                                case "TimeSpan":
                                    defaultValue = default(TimeSpan);
                                    itemTemplateKey = readOnly ? "StringMetadataValueDataTemplate" : "TimeSpanMetadataValueDataTemplate";
                                    func = this.Convert<TimeSpan>;
                                    break;

                                case "Guid":
                                    defaultValue = default(Guid); 
                                    itemTemplateKey = readOnly ? "StringMetadataValueDataTemplate" : "GuidMetadataValueDataTemplate";
                                    func = this.Convert<Guid>;
                                    break;

                                case "Point":
                                    defaultValue = default(Point); 
                                    itemTemplateKey = readOnly ? "StringMetadataValueDataTemplate" : "PointMetadataValueDataTemplate";
                                    func = this.Convert<Point>;
                                    break;

                                case "Size":
                                    defaultValue = default(Size);
                                    itemTemplateKey = readOnly ? "StringMetadataValueDataTemplate" : "SizeMetadataValueDataTemplate";
                                    func = this.Convert<Size>;
                                    break;

                                case "Rect":
                                    defaultValue = default(Rect);
                                    itemTemplateKey = readOnly ? "StringMetadataValueDataTemplate" : "RectMetadataValueDataTemplate";
                                    func = this.Convert<Rect>;
                                    break;

                                default:
                                    Debug.Assert(false);
                                    break;
                            }
                        }

                        DataTemplate itemTemplate = null;
                        if (itemTemplateKey != null)
                        {
                            if (readOnly)
                            {
                                itemTemplate = Resources.Get("ReadOnly" + itemTemplateKey) as DataTemplate;
                            }
                            else
                            {
                                itemTemplate = Application.Current.FindResource("KinectStudioPlugin.Writable" + itemTemplateKey) as DataTemplate;
                            }
                        }

                        ObservableCollection<ValueHolder> listCopy = null;
                        if (!readOnly)
                        {
                            listCopy = new ObservableCollection<ValueHolder>();
                            foreach (object o in list)
                            {
                                listCopy.Add(new ValueHolder(o));
                            }

                            list = listCopy;
                        }

                        using (WaitCursor waitCursor = new WaitCursor(window))
                        {
                            MetadataArrayViewerDialog dialog = new MetadataArrayViewerDialog()
                                {
                                    Owner = window,
                                    Key = key,
                                    ItemsSource = list,
                                    WindowStartupLocation = WindowStartupLocation.Manual,
                                    ItemTemplate = itemTemplate,
                                    DefaultValue = defaultValue,
                                    IsWritable = !readOnly,
                                };

                            Point point = Mouse.PrimaryDevice.GetPosition(window);

                            if ((point.X + dialog.Width) > window.ActualWidth)
                            {
                                point.X = window.ActualWidth - dialog.Width;
                            }

                            if ((point.Y + dialog.Height) > window.ActualHeight)
                            {
                                point.Y = window.ActualHeight - dialog.Height;
                            }

                            dialog.Left = point.X + window.Left;
                            dialog.Top = point.Y + window.Top;

                            if (dialog.ShowDialog() == true)
                            {
                                if (metadata != null)
                                {
                                    metadata[key] = func(listCopy);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static readonly RoutedUICommand viewArrayMetadataCommand = new RoutedUICommand("ViewArrayMetadataCommand", "ViewArrayMetadataCommand", typeof(AdvancedMetadataArrayPlugin));
        private static readonly RoutedUICommand editArrayMetadataCommand = new RoutedUICommand("EditArrayMetadataCommand", "EditArrayMetadataCommand", typeof(AdvancedMetadataArrayPlugin));

        private Array Convert<T>(ObservableCollection<ValueHolder> c)
        {
            T [] value = new T[c.Count];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (T)c[i].Value;
            }

            return value;
        }

        private static void LoadDataTemplates(DataTemplate dataTemplate, IDictionary<FileMetadataDataTemplateKey, DataTemplate> fileDataTemplates, IDictionary<StreamMetadataDataTemplateKey, DataTemplate> streamDataTemplates, Type valueType)
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(fileDataTemplates != null);
            Debug.Assert(streamDataTemplates != null);
            Debug.Assert(valueType != null);

            if (dataTemplate != null)
            {
                {
                    FileMetadataDataTemplateKey key = new FileMetadataDataTemplateKey(valueType);
                    fileDataTemplates.Add(key, dataTemplate);
                }

                {
                    StreamMetadataDataTemplateKey key = new StreamMetadataDataTemplateKey(valueType);
                    streamDataTemplates.Add(key, dataTemplate);
                }
            }
        }

        private readonly IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> fileReadOnlyDataTemplates;
        private readonly IReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate> streamReadOnlyDataTemplates;
        private readonly IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> fileWritableDataTemplates;
        private readonly IReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate> streamWritableDataTemplates;
    }
    

#if TODO_TEST
    public class TitlePlugin : BasePlugin, IMetadataPlugin
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "serviceProvider")]
        public TitlePlugin(IServiceProvider serviceProvider)
            : base("Title Plugin", new Guid(0x66666666, 0x6666, 0x6666, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66))
        {
            {
                Dictionary<FileMetadataDataTemplateKey, DataTemplate> dataTemplates = new Dictionary<FileMetadataDataTemplateKey, DataTemplate>();

                {
                    DataTemplate template = Resources.Get("ReadOnlyBananaStringMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "Banana"), template);
                    }
                }

                {
                    DataTemplate template = Resources.Get("SampleTestReadOnlyByteArrayMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        Guid metadataKey = new Guid(0x1276341d, 0x8dd, 0x4b3e, 0xb2, 0xfc, 0x91, 0xfa, 0x98, 0xa7, 0xc3, 0x6d);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(byte[]), "{" + metadataKey.ToString() + "}"), template);
                    }
                }

                this.fileReadOnlyDataTemplates = new ReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate>(dataTemplates);
            }
            {
                Dictionary<FileMetadataDataTemplateKey, DataTemplate> dataTemplates = new Dictionary<FileMetadataDataTemplateKey, DataTemplate>();

                {
                    DataTemplate template = Resources.Get("WritableBananaStringMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "Banana"), template);
                    }
                }

                {
                    DataTemplate template = Resources.Get("ForceReadOnlyStringMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        // don't let me sample users change some of metadata
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_ConsoleId"), template);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_NuiProductCode"), template);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_NuiSensorSerialNumber"), template);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_NuiSensorVersion"), template);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_NuiServiceVersion"), template);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_XStudioVersion"), template);
                    }

                    template = Resources.Get("SampleTestWritableByteArrayMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        Guid metadataKey = new Guid(0x1276341d, 0x8dd, 0x4b3e, 0xb2, 0xfc, 0x91, 0xfa, 0x98, 0xa7, 0xc3, 0x6d);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(byte[]), "{" + metadataKey.ToString() + "}"), template);
                    }
                }

                {
                    // sample title specific metadata
                    DataTemplate template = Resources.Get("SampleWritableByteArrayMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        Guid metadataKey = new Guid(0x93766207, 0x21f6, 0x4895, 0x8f, 0xa6, 0x44, 0xe7, 0x33, 0xc5, 0x9a, 0x44);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(byte[]), "{" + metadataKey.ToString() + "}" ), template);
                    }
                }

                this.fileWritableDataTemplates = new ReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate>(dataTemplates);
            }
        }

        public IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> FileReadOnlyDataTemplates
        {
            get
            {
                return this.fileReadOnlyDataTemplates;
            }
        }

        public IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> FileWritableDataTemplates
        {
            get
            {
                return this.fileWritableDataTemplates;
            }
        }

        public IReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate> StreamReadOnlyDataTemplates
        {
            get
            {
                return null;
            }
        }

        public IReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate> StreamWritableDataTemplates
        {
            get
            {
                return null;
            }
        }

        private readonly IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> fileReadOnlyDataTemplates;
        private readonly IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> fileWritableDataTemplates;
    }

    public class Title2Plugin : BasePlugin, IMetadataPlugin
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "serviceProvider")]
        public Title2Plugin(IServiceProvider serviceProvider)
            : base("Title 2 Plugin", new Guid(0x77777777, 0x7777, 0x7777, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77))
        {
            {
                Dictionary<FileMetadataDataTemplateKey, DataTemplate> dataTemplates = new Dictionary<FileMetadataDataTemplateKey, DataTemplate>();

                {
                    DataTemplate template = Resources.Get("ReadOnlyBananaStringMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "Banana"), template);
                    }
                }

                {
                    DataTemplate template = Resources.Get("SampleTestReadOnlyByteArrayMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        Guid metadataKey = new Guid(0x1276341d, 0x8dd, 0x4b3e, 0xb2, 0xfc, 0x91, 0xfa, 0x98, 0xa7, 0xc3, 0x6d);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(byte[]), "{" + metadataKey.ToString() + "}"), template);
                    }
                }

                this.fileReadOnlyDataTemplates = new ReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate>(dataTemplates);
            }
            {
                Dictionary<FileMetadataDataTemplateKey, DataTemplate> dataTemplates = new Dictionary<FileMetadataDataTemplateKey, DataTemplate>();

                {
                    DataTemplate template = Resources.Get("ForceReadOnlyStringMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        // don't let me sample users change some of metadata
                        // dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_ConsoleId"), template);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_NuiProductCode"), template);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_NuiSensorSerialNumber"), template);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_NuiSensorVersion"), template);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_NuiServiceVersion"), template);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(String), "_XStudioVersion"), template);
                    }

                    template = Resources.Get("SampleTestWritableByteArrayMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        Guid metadataKey = new Guid(0x1276341d, 0x8dd, 0x4b3e, 0xb2, 0xfc, 0x91, 0xfa, 0x98, 0xa7, 0xc3, 0x6d);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(byte[]), "{" + metadataKey.ToString() + "}"), template);
                    }
                }

                {
                    // sample title specific metadata
                    DataTemplate template = Resources.Get("SampleWritableByteArrayMetadataValueDataTemplate") as DataTemplate;
                    if (template != null)
                    {
                        Guid metadataKey = new Guid(0x93766207, 0x21f6, 0x4895, 0x8f, 0xa6, 0x44, 0xe7, 0x33, 0xc5, 0x9a, 0x44);
                        dataTemplates.Add(new FileMetadataDataTemplateKey(typeof(byte[]), "{" + metadataKey.ToString() + "}"), template);
                    }
                }

                this.fileWritableDataTemplates = new ReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate>(dataTemplates);
            }
        }

        public IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> FileReadOnlyDataTemplates
        {
            get
            {
                return this.fileReadOnlyDataTemplates;
            }
        }

        public IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> FileWritableDataTemplates
        {
            get
            {
                return this.fileWritableDataTemplates;
            }
        }

        public IReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate> StreamReadOnlyDataTemplates
        {
            get
            {
                return null;
            }
        }

        public IReadOnlyDictionary<StreamMetadataDataTemplateKey, DataTemplate> StreamWritableDataTemplates
        {
            get
            {
                return null;
            }
        }

        private readonly IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> fileReadOnlyDataTemplates;
        private readonly IReadOnlyDictionary<FileMetadataDataTemplateKey, DataTemplate> fileWritableDataTemplates;
    }

    public class BodyInfoPlugin : BasePlugin, IEventHandlerPlugin, IWpfVisualPlugin
    {
        public BodyInfoPlugin()
            : base("Body Plugin", new Guid(0x11111111, 0x1111, 0x1111, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11))
        {
        }

        public void HandleEvent(KStudioEvent eventObj)
        {
            RandomData++;
        }

        public IPluginViewSettings AddWpfView(Guid viewId, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new SamplePluginViewSettings();

            if (hostControl != null)
            {
                BodyStuff bodyStuff = new BodyStuff();
                bodyStuff.DataContext = this;
                hostControl.Children.Add(bodyStuff);
            }

            return pluginViewSettings;
        }

        public void RemoveView(Guid viewId, Panel hostControl, IPluginViewSettings pluginViewSettings)
        {
        }

        public int RandomData
        {
            get { return _randomData; }
            set { if (_randomData != value) { _randomData = value; RaisePropertyChanged("RandomData"); } }
        }

        private int _randomData = 0;
    }

    public class ExpressionInfoPlugin : BasePlugin, IEventHandlerPlugin, IWpfVisualPlugin
    {
        public ExpressionInfoPlugin()
            : base("Expression Plugin", new Guid(0x33333333, 0x3333, 0x3333, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33))
        {
        }

        public void HandleEvent(KStudioEvent eventObj)
        {
            RandomData++;
        }

        public void RemoveView(Guid viewId, Panel hostControl, IPluginViewSettings pluginViewSettings)
        {
        }

        public IPluginViewSettings AddWpfView(Guid viewId, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = new SamplePluginViewSettings();

            if (hostControl != null)
            {
                ExpressionStuff expressionStuff = new ExpressionStuff();
                expressionStuff.DataContext = this;
                hostControl.Children.Add(expressionStuff);
            }

            return pluginViewSettings;
        }

        public int RandomData
        {
            get { return _randomData; }
            set { if (_randomData != value) { _randomData = value; RaisePropertyChanged("RandomData"); } }
        }

        private int _randomData = 0;
    }
#endif

}
