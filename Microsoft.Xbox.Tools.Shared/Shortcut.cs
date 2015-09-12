//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ShortcutManager : DependencyObject
    {
        public static readonly RoutedCommand ShortcutCommand = new RoutedCommand("Shortcut", typeof(ShortcutManager));

        public static readonly DependencyProperty ShortcutProperty = DependencyProperty.RegisterAttached(
            "Shortcut", typeof(string), typeof(ShortcutManager), new FrameworkPropertyMetadata(OnAdornerStatePropertyChanged));

        public static readonly DependencyProperty HorizontalAlignmentProperty = DependencyProperty.RegisterAttached(
            "HorizontalAlignment", typeof(HorizontalAlignment?), typeof(ShortcutManager), new FrameworkPropertyMetadata(OnAdornerStatePropertyChanged));

        public static readonly DependencyProperty VerticalAlignmentProperty = DependencyProperty.RegisterAttached(
            "VerticalAlignment", typeof(VerticalAlignment?), typeof(ShortcutManager), new FrameworkPropertyMetadata(OnAdornerStatePropertyChanged));

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.RegisterAttached(
            "Offset", typeof(Point?), typeof(ShortcutManager), new FrameworkPropertyMetadata(OnAdornerStatePropertyChanged));

        public static readonly DependencyProperty IsAdornerVisibleProperty = DependencyProperty.RegisterAttached(
            "IsAdornerVisible", typeof(bool), typeof(ShortcutManager), new FrameworkPropertyMetadata(OnAdornerStatePropertyChanged));

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(ShortcutManager), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty ShortcutAdornerProperty = DependencyProperty.RegisterAttached(
            "ShortcutAdorner", typeof(ShortcutAdorner), typeof(ShortcutManager));

        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
            "Command", typeof(ICommand), typeof(ShortcutManager));

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached(
            "CommandParameter", typeof(object), typeof(ShortcutManager));

        public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.RegisterAttached(
            "CommandTarget", typeof(IInputElement), typeof(ShortcutManager));

        public static readonly DependencyProperty UIModeProperty = DependencyProperty.Register(
            "UIMode", typeof(string), typeof(ShortcutManager), new FrameworkPropertyMetadata(string.Empty, OnUIModeChanged));

        public static readonly DependencyProperty AreShortcutAdornmentsVisibleProperty = DependencyProperty.Register(
            "AreShortcutAdornmentsVisible", typeof(bool), typeof(ShortcutManager), new FrameworkPropertyMetadata(OnAreShortcutAdornmentsVisibleChanged));

        public static readonly DependencyProperty InstanceProperty = DependencyProperty.RegisterAttached(
            "Instance", typeof(ShortcutManager), typeof(ShortcutManager), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        #region DP getters/setters
        public static string GetShortcut(UIElement obj)
        {
            return (string)obj.GetValue(ShortcutProperty);
        }

        public static void SetShortcut(UIElement obj, string value)
        {
            obj.SetValue(ShortcutProperty, value);
        }

        public static HorizontalAlignment? GetHorizontalAlignment(UIElement obj)
        {
            return (HorizontalAlignment?)obj.GetValue(HorizontalAlignmentProperty);
        }

        public static void SetHorizontalAlignment(UIElement obj, HorizontalAlignment? value)
        {
            obj.SetValue(HorizontalAlignmentProperty, value);
        }

        public static VerticalAlignment? GetVerticalAlignment(UIElement obj)
        {
            return (VerticalAlignment?)obj.GetValue(VerticalAlignmentProperty);
        }

        public static void SetVerticalAlignment(UIElement obj, VerticalAlignment? value)
        {
            obj.SetValue(VerticalAlignmentProperty, value);
        }

        public static Point? GetOffset(UIElement obj)
        {
            return (Point?)obj.GetValue(OffsetProperty);
        }

        public static void SetOffset(UIElement obj, Point? value)
        {
            obj.SetValue(OffsetProperty, value);
        }

        public static bool GetIsAdornerVisible(UIElement obj)
        {
            return (bool)obj.GetValue(IsAdornerVisibleProperty);
        }

        public static void SetIsAdornerVisible(UIElement obj, bool value)
        {
            obj.SetValue(IsAdornerVisibleProperty, value);
        }

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static IInputElement GetCommandTarget(DependencyObject obj)
        {
            return (IInputElement)obj.GetValue(CommandTargetProperty);
        }

        public static void SetCommandTarget(DependencyObject obj, IInputElement value)
        {
            obj.SetValue(CommandTargetProperty, value);
        }

        public static object GetCommandParameter(DependencyObject obj)
        {
            return (object)obj.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(DependencyObject obj, object value)
        {
            obj.SetValue(CommandParameterProperty, value);
        }

        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CommandProperty);
        }

        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }

        public static ShortcutAdorner GetShortcutAdorner(UIElement obj)
        {
            return (ShortcutAdorner)obj.GetValue(ShortcutAdornerProperty);
        }

        public static void SetShortcutAdorner(UIElement obj, ShortcutAdorner value)
        {
            obj.SetValue(ShortcutAdornerProperty, value);
        }

        public static ShortcutManager GetInstance(DependencyObject obj)
        {
            return (ShortcutManager)obj.GetValue(InstanceProperty);
        }

        public static void SetInstance(DependencyObject obj, ShortcutManager value)
        {
            obj.SetValue(InstanceProperty, value);
        }

        #endregion

        HashSet<UIElement> adornedElements = new HashSet<UIElement>();
        DispatcherTimer timer;

        public event EventHandler UIModeChanged;
        public event EventHandler EmptyModePushed;

        public ShortcutManager()
        {
            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromSeconds(0.25);
            this.timer.Tick += OnTimerTick;
        }

        public string UIMode
        {
            get { return (string)GetValue(UIModeProperty); }
            set { SetValue(UIModeProperty, value); }
        }

        void OnTimerTick(object sender, EventArgs e)
        {
            // If the adorned element is affected by a render transform, WPF does not reliably keep 
            // the adornments in the adorner layer in lock-step with it.  To get around this, we use
            // a timer to continually update the visibility state (which forces current positioning based
            // on underlying transforms) while the adorners are visible.
            if (this.AreShortcutAdornmentsVisible)
            {
                UpdateAdornerVisualStates();
            }
        }

        public bool AreShortcutAdornmentsVisible
        {
            get { return (bool)GetValue(AreShortcutAdornmentsVisibleProperty); }
            set { SetValue(AreShortcutAdornmentsVisibleProperty, value); }
        }

        public void PushUISubMode(string subMode)
        {
            if (string.IsNullOrEmpty(this.UIMode))
            {
                this.UIMode = subMode;
            }
            else
            {
                this.UIMode = this.UIMode + "|" + subMode;
            }

            if (this.AreShortcutAdornmentsVisible)
            {
                // If there are no adorners in this mode, then drop out of shortcut mode.
                CheckForEmptyMode(true);
            }
        }

        void CheckForEmptyMode(bool waitForIdle)
        {
            if (!this.adornedElements.Any(e => GetIsAdornerVisible(e)))
            {
                if (waitForIdle)
                {
                    // Why wait for idle?  Because this may be the first time adorned elements are loaded, and thus may
                    // not have been registered.  So wait until that has a chance to happen.
                    Dispatcher.BeginInvoke((Action)(() => CheckForEmptyMode(false)), DispatcherPriority.Background);
                }
                else
                {
                    var handler = this.EmptyModePushed;

                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
            }
        }

        public bool PopUISubMode()
        {
            if (string.IsNullOrEmpty(this.UIMode))
            {
                return false;
            }

            string[] modes = this.UIMode.Split('|');

            if (modes.Length > 1)
            {
                this.UIMode = string.Join("|", modes.Take(modes.Length - 1));
            }
            else
            {
                this.UIMode = string.Empty;
            }

            return true;
        }

        static bool GetShortcutKeyAndMode(UIElement element, out Key key, out string mode)
        {
            string shortcut = GetShortcut(element);

            if (shortcut != null)
            {
                string[] parts = shortcut.Split(':');

                if ((parts.Length == 2 || parts.Length == 1) && Enum.TryParse<Key>(parts[0], out key))
                {
                    mode = parts.Length == 2 ? parts[1] : string.Empty;
                    return true;
                }
            }

            key = default(Key);
            mode = null;
            return false;
        }

        public bool ProcessShortcutKey(Key key)
        {
            if (key == Key.Escape)
            {
                return PopUISubMode();
            }

            foreach (var element in this.adornedElements.Where(e => e.IsVisible))
            {
                Key shortcutKey;
                string mode;

                if (GetShortcutKeyAndMode(element, out shortcutKey, out mode))
                {
                    if (key == shortcutKey && mode == this.UIMode)
                    {
                        ICommand cmd = GetCommand(element);
                        object parameter = GetCommandParameter(element);
                        IInputElement target = GetCommandTarget(element);

                        if (cmd == null)
                        {
                            var commandSource = element as ICommandSource;

                            if (commandSource != null)
                            {
                                cmd = commandSource.Command;
                                parameter = commandSource.CommandParameter;
                                target = commandSource.CommandTarget;
                            }
                        }

                        bool executed = false;

                        if (cmd != null)
                        {
                            var routedCmd = cmd as RoutedCommand;

                            if (routedCmd != null)
                            {
                                if (routedCmd.CanExecute(parameter, target ?? element))
                                {
                                    routedCmd.Execute(parameter, target);
                                    executed = true;
                                }
                            }
                            else
                            {
                                if (cmd.CanExecute(parameter))
                                {
                                    cmd.Execute(parameter);
                                    executed = true;
                                }
                            }
                        }

                        return executed;
                    }
                }
            }

            Debug.WriteLine("ProcessShortcutKey:  Failed to find shortcut for key {0}", key);
            return false;
        }

        void UpdateAdornerVisualStates()
        {
            foreach (var element in this.adornedElements)
            {
                UpdateAdornerVisualState(element);
            }
        }

        void UpdateAdornerVisualState(UIElement element)
        {
            if (this.AreShortcutAdornmentsVisible && element.IsVisible)
            {
                Key key;
                string mode;

                if (GetShortcutKeyAndMode(element, out key, out mode) && (mode == this.UIMode))
                {
                    SetIsAdornerVisible(element, true);
                }
                else
                {
                    SetIsAdornerVisible(element, false);
                }
            }
            else
            {
                SetIsAdornerVisible(element, false);
            }
        }

        static void OnUIModeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ShortcutManager mgr = obj as ShortcutManager;

            if (mgr != null)
            {
                mgr.UpdateAdornerVisualStates();

                var handler = mgr.UIModeChanged;

                if (handler != null)
                {
                    handler(mgr, EventArgs.Empty);
                }
            }
        }

        static void OnAreShortcutAdornmentsVisibleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ShortcutManager mgr = obj as ShortcutManager;

            if (mgr != null)
            {
                if (mgr.AreShortcutAdornmentsVisible)
                {
                    mgr.timer.Start();
                }
                else
                {
                    mgr.timer.Stop();
                }

                mgr.UpdateAdornerVisualStates();
            }
        }

        static void OnAdornerStatePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = obj as UIElement;

            if (element != null)
            {
                if (e.Property == ShortcutManager.ShortcutProperty && !string.IsNullOrEmpty((string)e.NewValue))
                {
                    var manager = GetInstance(element);

                    if (manager != null)
                    {
                        // Elements must have a key set in order to be "adorned" -- so use this property to trigger registration.
                        manager.adornedElements.Add(element);
                        manager.UpdateAdornerVisualState(element);
                    }
                }

                UpdateAttachedAdorner(element);
            }
        }

        private static void UpdateAttachedAdorner(UIElement element)
        {
            var layer = AdornerLayer.GetAdornerLayer(element);

            if (layer == null)
            {
                var fe = element as FrameworkElement;

                if (fe != null && !fe.IsLoaded)
                {
                    RoutedEventHandler handler = null;

                    handler = (s, e) =>
                    {
                        fe.Loaded -= handler;
                        UpdateAttachedAdorner(fe);
                    };

                    // This element is not loaded yet.  Check again when it is
                    fe.Loaded += handler;
                }
                else
                {
                    Debug.WriteLine("No adorner layer!  Element is not a framework element, or already loaded!  Hands thrown up!");
                }
            }
            else
            {
                bool isVisible = GetIsAdornerVisible(element);
                Key key;
                string mode;
                Point? offset = GetOffset(element);
                HorizontalAlignment? horizontalAlignment = GetHorizontalAlignment(element);
                VerticalAlignment? verticalAlignment = GetVerticalAlignment(element);
                ShortcutAdorner adorner = GetShortcutAdorner(element);

                if (!GetShortcutKeyAndMode(element, out key, out mode))
                {
                    if (adorner != null)
                    {
                        layer.Remove(adorner);
                        SetShortcutAdorner(element, null);
                        adorner = null;
                    }
                }
                else
                {
                    if (adorner == null)
                    {
                        if (isVisible)
                        {
                            adorner = new ShortcutAdorner(element);
                            layer.Add(adorner);
                            SetShortcutAdorner(element, adorner);
                        }
                    }
                    else
                    {
                        if (!isVisible)
                        {
                            layer.Remove(adorner);
                            SetShortcutAdorner(element, null);
                            adorner = null;
                        }
                    }

                    if (adorner != null)
                    {
                        string keyText = key.ToString();

                        if (key >= Key.D0 && key <= Key.D9)
                        {
                            keyText = keyText.Substring(1);
                        }
                        adorner.SetShortcutKey(keyText);
                        adorner.SetPosition(offset, horizontalAlignment, verticalAlignment);
                    }
                }
            }
        }
    }

    public class ShortcutAdorner : Adorner
    {
        ShortcutAdornerControl adornerControl;

        public ShortcutAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            this.adornerControl = new ShortcutAdornerControl(adornedElement);
            this.adornerControl.SetBinding(Control.IsEnabledProperty, new Binding { Source = adornedElement, Path = new PropertyPath(ShortcutManager.IsEnabledProperty) });
            AddVisualChild(this.adornerControl);
        }

        public void SetShortcutKey(string key)
        {
            this.adornerControl.ShortcutKey = key;
        }

        public void SetPosition(Point? offset, HorizontalAlignment? horizontalAlignment, VerticalAlignment? verticalAlignment)
        {
            if (offset.HasValue)
            {
                this.adornerControl.Offset = offset.Value;
            }
            else
            {
                this.adornerControl.ClearValue(ShortcutAdornerControl.OffsetProperty);
            }

            if (horizontalAlignment.HasValue)
            {
                this.adornerControl.HorizontalAlignment = horizontalAlignment.Value;
                this.HorizontalAlignment = horizontalAlignment.Value;
            }
            else
            {
                this.adornerControl.ClearValue(HorizontalAlignmentProperty);
                this.ClearValue(HorizontalAlignmentProperty);
            }

            if (verticalAlignment.HasValue)
            {
                this.adornerControl.VerticalAlignment = verticalAlignment.Value;
                this.VerticalAlignment = verticalAlignment.Value;
            }
            else
            {
                this.adornerControl.ClearValue(VerticalAlignmentProperty);
                this.ClearValue(VerticalAlignmentProperty);
            }
        }

        protected override int VisualChildrenCount { get { return 1; } }

        protected override Visual GetVisualChild(int index)
        {
            return adornerControl;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            adornerControl.Measure(constraint);

            var adornedElement = this.AdornedElement as FrameworkElement;
            var elementSize = adornedElement == null ? adornerControl.DesiredSize : new Size(adornedElement.ActualWidth, adornedElement.ActualHeight);

            return new Size(Math.Max(adornerControl.DesiredSize.Width, elementSize.Width), Math.Max(adornerControl.DesiredSize.Height, elementSize.Height));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            adornerControl.Arrange(new Rect(new Point(0, 0), finalSize));
            return finalSize;
        }
    }

    public class ShortcutAdornerControl : Control
    {
        public static readonly DependencyProperty ShortcutKeyProperty = DependencyProperty.Register(
            "ShortcutKey", typeof(string), typeof(ShortcutAdornerControl));

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            "Offset", typeof(Point), typeof(ShortcutAdornerControl), new FrameworkPropertyMetadata(OnOffsetChanged));

        TranslateTransform transform;

        [SuppressMessage("Microsoft.Usage", "#pw26506")]
        public ShortcutAdornerControl(UIElement adornedElement)
        {
            this.Focusable = false;
            this.AdornedElementType = adornedElement.GetType();
            this.transform = new TranslateTransform();
            this.RenderTransform = transform;
        }

        public string ShortcutKey
        {
            get { return (string)GetValue(ShortcutKeyProperty); }
            set { SetValue(ShortcutKeyProperty, value); }
        }

        public Point Offset
        {
            get { return (Point)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public Type AdornedElementType { get; private set; }

        static void OnOffsetChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ShortcutAdornerControl ctrl = obj as ShortcutAdornerControl;

            if (ctrl != null)
            {
                ctrl.transform.X = ctrl.Offset.X;
                ctrl.transform.Y = ctrl.Offset.Y;
            }
        }
    }
}
