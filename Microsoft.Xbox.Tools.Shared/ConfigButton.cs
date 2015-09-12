//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ConfigButton : Button
    {
        public static readonly DependencyProperty PopupContentProperty = DependencyProperty.Register(
            "PopupContent", typeof(object), typeof(ConfigButton));

        public static readonly DependencyProperty PopupContentTemplateProperty = DependencyProperty.Register(
            "PopupContentTemplate", typeof(DataTemplate), typeof(ConfigButton));

        public static readonly DependencyProperty PopupContentTemplateSelectorProperty = DependencyProperty.Register(
            "PopupContentTemplateSelector", typeof(DataTemplateSelector), typeof(ConfigButton));

        public static readonly DependencyProperty CommitsConfigButtonProperty = DependencyProperty.RegisterAttached(
            "CommitsConfigButton", typeof(bool), typeof(ConfigButton), new FrameworkPropertyMetadata(OnCommitsConfigButtonChanged));

        public static readonly DependencyProperty DismissesConfigButtonProperty = DependencyProperty.RegisterAttached(
            "DismissesConfigButton", typeof(bool), typeof(ConfigButton));

        public static readonly DependencyProperty GetsFocusOnPopupOpenProperty = DependencyProperty.RegisterAttached(
            "GetsFocusOnPopupOpen", typeof(bool), typeof(ConfigButton), new FrameworkPropertyMetadata(OnGetsFocusOnPopupOpenChanged));

        public static readonly DependencyProperty RequiresValidationProperty = DependencyProperty.RegisterAttached(
            "RequiresValidation", typeof(bool), typeof(ConfigButton), new FrameworkPropertyMetadata(OnRequiresValidationChanged));

        public static readonly DependencyProperty CanCommitProperty = DependencyProperty.Register(
            "CanCommit", typeof(bool), typeof(ConfigButton), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty IsPopupOpenProperty = DependencyProperty.Register(
            "IsPopupOpen", typeof(bool), typeof(ConfigButton), new FrameworkPropertyMetadata(OnIsPopupOpenChanged));

        // This event is for child controls in the popup, i.e., buttons that close/dismiss the popup when pressed
        public static readonly RoutedEvent ClosePopupEvent = EventManager.RegisterRoutedEvent("ClosePopup", RoutingStrategy.Bubble, typeof(EventHandler<ClosePopupEventArgs>), typeof(ConfigButton));

        // These events are for observers of the ConfigButton itself
        public static readonly RoutedEvent PopupOpenedEvent = EventManager.RegisterRoutedEvent("PopupOpened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ConfigButton));
        public static readonly RoutedEvent PopupClosedEvent = EventManager.RegisterRoutedEvent("PopupClosed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ConfigButton));

        static ConfigButton currentlyOpenConfigButton;

        Popup popup;
        List<FrameworkElement> validatedElements = new List<FrameworkElement>();

        public object PopupContent
        {
            get { return (object)GetValue(PopupContentProperty); }
            set { SetValue(PopupContentProperty, value); }
        }

        public DataTemplate PopupContentTemplate
        {
            get { return (DataTemplate)GetValue(PopupContentTemplateProperty); }
            set { SetValue(PopupContentTemplateProperty, value); }
        }

        public DataTemplateSelector PopupContentTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(PopupContentTemplateSelectorProperty); }
            set { SetValue(PopupContentTemplateSelectorProperty, value); }
        }

        public bool CanCommit
        {
            get { return (bool)GetValue(CanCommitProperty); }
            set { SetValue(CanCommitProperty, value); }
        }

        public bool IsPopupOpen
        {
            get { return (bool)GetValue(IsPopupOpenProperty); }
            set { SetValue(IsPopupOpenProperty, value); }
        }

        public event RoutedEventHandler PopupOpened
        {
            add
            {
                this.AddHandler(PopupOpenedEvent, value);
            }
            remove
            {
                this.RemoveHandler(PopupOpenedEvent, value);
            }
        }

        public event RoutedEventHandler PopupClosed
        {
            add
            {
                this.AddHandler(PopupClosedEvent, value);
            }
            remove
            {
                this.RemoveHandler(PopupClosedEvent, value);
            }
        }

        public ConfigButton()
        {
            this.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnButtonClick));
            this.AddHandler(ClosePopupEvent, new EventHandler<ClosePopupEventArgs>(OnClosePopup));
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (this.IsPopupOpen && (e.Key == Key.Escape || e.Key == Key.Return))
            {
                this.IsPopupOpen = false;
                if (e.Key == Key.Return)
                {
                    this.OnClick();
                }
                e.Handled = true;
            }
            else if (!this.IsPopupOpen && (e.Key == Key.Down || e.SystemKey == Key.Down))
            {
                this.IsPopupOpen = true;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        void OnClosePopup(object sender, ClosePopupEventArgs e)
        {
            this.IsPopupOpen = false;

            if (e.RaiseClickEvent)
            {
                this.OnClick();
            }
        }

        protected override void OnClick()
        {
            if (this.CanCommit)
            {
                base.OnClick();
            }
            else
            {
                this.IsPopupOpen = true;
            }
        }

        void OnButtonClick(object sender, RoutedEventArgs e)
        {
            // If any button click events escape to this level, don't let them cause this button to be "clicked"...
            if (e.OriginalSource != this)
            {
                var dependencyObj = e.OriginalSource as DependencyObject;

                if (dependencyObj != null)
                {
                    // ...unless they indicate dismiss/commit via attached properties
                    bool commits = ConfigButton.GetCommitsConfigButton(dependencyObj);

                    if (commits || ConfigButton.GetDismissesConfigButton(dependencyObj))
                    {
                        this.RaiseEvent(new ClosePopupEventArgs(this, commits));
                    }
                }

                e.Handled = true;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.popup = this.GetTemplateChild("PART_Popup") as Popup;

            if (this.popup != null)
            {
                // If any mouse down events are unhandled to here, don't let them dismiss the tip.
                this.popup.AddHandler(Popup.MouseDownEvent, (RoutedEventHandler)((o, e) => e.Handled = true));

                this.popup.Opened += (o, e) => { this.RaiseEvent(new RoutedEventArgs(PopupOpenedEvent)); };
                this.popup.Closed += (o, e) => { this.RaiseEvent(new RoutedEventArgs(PopupClosedEvent)); };
            }
        }

        public static bool GetCommitsConfigButton(DependencyObject obj)
        {
            return (bool)obj.GetValue(CommitsConfigButtonProperty);
        }

        public static void SetCommitsConfigButton(DependencyObject obj, bool value)
        {
            obj.SetValue(CommitsConfigButtonProperty, value);
        }

        public static bool GetDismissesConfigButton(DependencyObject obj)
        {
            return (bool)obj.GetValue(DismissesConfigButtonProperty);
        }

        public static void SetDismissesConfigButton(DependencyObject obj, bool value)
        {
            obj.SetValue(DismissesConfigButtonProperty, value);
        }

        public static bool GetRequiresValidation(DependencyObject obj)
        {
            return (bool)obj.GetValue(RequiresValidationProperty);
        }

        public static void SetRequiresValidation(DependencyObject obj, bool value)
        {
            obj.SetValue(RequiresValidationProperty, value);
        }

        public static bool GetGetsFocusOnPopupOpen(DependencyObject obj)
        {
            return (bool)obj.GetValue(GetsFocusOnPopupOpenProperty);
        }

        public static void SetGetsFocusOnPopupOpen(DependencyObject obj, bool value)
        {
            obj.SetValue(GetsFocusOnPopupOpenProperty, value);
        }

        static void OnIsPopupOpenChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ConfigButton button = obj as ConfigButton;

            if (button != null)
            {
                if ((bool)e.NewValue)
                {
                    if (currentlyOpenConfigButton != null)
                    {
                        currentlyOpenConfigButton.IsPopupOpen = false;
                    }
                    currentlyOpenConfigButton = button;

                    if (button.popup != null)
                    {
                        button.popup.IsOpen = true;
                    }
                }
                else
                {
                    if (button.popup != null)
                    {
                        button.popup.IsOpen = false;
                    }

                    if (currentlyOpenConfigButton == button)
                    {
                        currentlyOpenConfigButton = null;
                    }
                }
            }
        }

        static void OnGetsFocusOnPopupOpenChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = obj as FrameworkElement;

            if (element != null)
            {
                element.Loaded += (sender, args) =>
                {
                    if (!element.IsKeyboardFocusWithin)
                    {
                        element.Focus();
                        if (element is TextBox)
                        {
                            ((TextBox)element).SelectAll();
                        }
                    }
                };
            }
        }

        static void OnCommitsConfigButtonChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = obj as FrameworkElement;

            if (element != null && (bool)e.NewValue)
            {
                if (element.IsLoaded)
                {
                    BindCommitControlToValidatedProperty(element);
                }
                else
                {
                    RoutedEventHandler handler = null;

                    handler = (sender, args) =>
                    {
                        BindCommitControlToValidatedProperty(element);
                        element.Loaded -= handler;
                    };

                    element.Loaded += handler;
                }
            }
        }

        static void BindCommitControlToValidatedProperty(FrameworkElement element)
        {
            ConfigButton button = element.FindParent<ConfigButton>();

            if (button != null)
            {
                element.SetBinding(IsEnabledProperty, new Binding { Source = button, Path = new PropertyPath(CanCommitProperty) });
            }
        }

        static void AddOrRemoveValidatedElement(FrameworkElement element, bool add)
        {
            ConfigButton button = element.FindParent<ConfigButton>();

            if (button != null)
            {
                if (add)
                {
                    button.validatedElements.Add(element);
                }
                else
                {
                    button.validatedElements.Remove(element);
                }

                if (button.validatedElements.Count > 0)
                {
                    var mb = new MultiBinding { Converter = HasErrorsToCanCommitConverter.Instance };

                    foreach (var validatedElement in button.validatedElements)
                    {
                        mb.Bindings.Add(new Binding { Source = validatedElement, Path = new PropertyPath(Validation.HasErrorProperty) });
                    }

                    button.SetBinding(CanCommitProperty, mb);
                }
                else
                {
                    button.CanCommit = true;
                }
            }
        }

        static void OnRequiresValidationChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = obj as FrameworkElement;

            if (element != null)
            {
                if (element.IsLoaded)
                {
                    AddOrRemoveValidatedElement(element, (bool)e.NewValue);
                }
                else
                {
                    RoutedEventHandler handler = null;

                    handler = (sender, args) =>
                    {
                        AddOrRemoveValidatedElement(element, (bool)e.NewValue);
                        element.Loaded -= handler;
                    };

                    element.Loaded += handler;
                }
            }
        }

        class HasErrorsToCanCommitConverter : IMultiValueConverter
        {
            public static HasErrorsToCanCommitConverter Instance { get; private set; }

            static HasErrorsToCanCommitConverter()
            {
                Instance = new HasErrorsToCanCommitConverter();
            }

            public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return !values.OfType<bool>().Any(b => b);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class ConfigButtonPopup : Popup
    {
        static ConfigButtonPopup()
        {
            // Standard WPF Popup doesn't deal with automation IDs correctly.  The actual underlying Window that they live in is what gets
            // picked up by UI automation, but the Popup object is where you want to set the automation ID.
            // Here is how we cope with that: when AutomationId is set on the Popup, we push it to the actual window (which is the visual root
            // of the logical child of the Popup...)
            AutomationProperties.AutomationIdProperty.OverrideMetadata(typeof(ConfigButtonPopup), new FrameworkPropertyMetadata(OnAutomationIdChanged));
        }

        void PropagateAutomationIdToChild()
        {
            var child = LogicalTreeHelper.GetChildren(this).OfType<FrameworkElement>().FirstOrDefault();

            if (child != null)
            {
                Visual popupRoot = FindVisualRoot(child);
                if (popupRoot != null)
                {
                    popupRoot.SetValue(AutomationProperties.AutomationIdProperty, GetValue(AutomationProperties.AutomationIdProperty));
                }
            }
        }

        static Visual FindVisualRoot(Visual child)
        {
            for (var parent = VisualTreeHelper.GetParent(child) as Visual; parent != null; child = parent, parent = VisualTreeHelper.GetParent(child) as Visual)
                ;

            return child;
        }

        static void OnAutomationIdChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ConfigButtonPopup popup = obj as ConfigButtonPopup;

            if (popup != null)
            {
                if (popup.IsOpen)
                {
                    popup.PropagateAutomationIdToChild();
                }
                else
                {
                    EventHandler handler = null;

                    handler = (sender, args) =>
                    {
                        popup.PropagateAutomationIdToChild();
                        popup.Opened -= handler;
                    };

                    popup.Opened += handler;
                }
            }
        }
    }

    public class ClosePopupEventArgs : RoutedEventArgs
    {
        public bool RaiseClickEvent { get; private set; }

        public ClosePopupEventArgs(object source, bool raiseClick)
            : base(ConfigButton.ClosePopupEvent, source)
        {
            this.RaiseClickEvent = raiseClick;
        }
    }
}
