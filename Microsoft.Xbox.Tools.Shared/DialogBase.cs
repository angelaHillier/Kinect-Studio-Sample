//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Xbox.Tools.Shared
{
    [ContentProperty("MainContent")]
    public class DialogBase : Window
    {
        public static readonly DependencyProperty MainContentProperty = DependencyProperty.Register(
            "MainContent", typeof(object), typeof(DialogBase));

        public static readonly DependencyProperty MainContentTemplateProperty = DependencyProperty.Register(
            "MainContentTemplate", typeof(DataTemplate), typeof(DialogBase));

        public static readonly DependencyProperty FootnoteProperty = DependencyProperty.Register(
            "Footnote", typeof(object), typeof(DialogBase));

        public static readonly DependencyProperty FootnoteTemplateProperty = DependencyProperty.Register(
            "FootnoteTemplate", typeof(DataTemplate), typeof(DialogBase));

        public static readonly DependencyProperty DetailsProperty = DependencyProperty.Register(
            "Details", typeof(object), typeof(DialogBase));

        public static readonly DependencyProperty DetailsTemplateProperty = DependencyProperty.Register(
            "DetailsTemplate", typeof(DataTemplate), typeof(DialogBase));

        public static readonly DependencyProperty ProgressiveDisclosureProperty = DependencyProperty.Register(
            "ProgressiveDisclosure", typeof(bool), typeof(DialogBase));

        public object MainContent
        {
            get { return (object)GetValue(MainContentProperty); }
            set { SetValue(MainContentProperty, value); }
        }

        public DataTemplate MainContentTemplate
        {
            get { return (DataTemplate)GetValue(MainContentTemplateProperty); }
            set { SetValue(MainContentTemplateProperty, value); }
        }

        public bool ProgressiveDisclosure
        {
            get { return (bool)GetValue(ProgressiveDisclosureProperty); }
            set { SetValue(ProgressiveDisclosureProperty, value); }
        }

        public object Details
        {
            get { return (object)GetValue(DetailsProperty); }
            set { SetValue(DetailsProperty, value); }
        }

        public DataTemplate DetailsTemplate
        {
            get { return (DataTemplate)GetValue(DetailsTemplateProperty); }
            set { SetValue(DetailsTemplateProperty, value); }
        }

        public object Footnote
        {
            get { return (object)GetValue(FootnoteProperty); }
            set { SetValue(FootnoteProperty, value); }
        }

        public DataTemplate FootnoteTemplate
        {
            get { return (DataTemplate)GetValue(FootnoteTemplateProperty); }
            set { SetValue(FootnoteTemplateProperty, value); }
        }

        public DialogBase()
        {
            // You might ask yourself why this is happening...
            //
            // It's all because WPF apparently has a bug where if you use the Template property on Window to customize the
            // visual tree of its content (i.e., put the button tray / progressive disclosure / footnote stuff in), everything
            // works great *except* you don't get focus visuals for any of the controls.
            //
            // So to get around that, we don't mess with the template of the window itself, but instead:
            //  1) Programmatically create a ContentPresenter and set it as the content of the Window (dialog)
            //  2) Change the default content property to be "MainContent" (so the xaml for DialogBase derivations looks "normal")
            //  3) Bind the content presenter to the MainContent property (and the template to MainContentTemplate)
            //
            // Viola!  Plus the dialogs are now individually customizable via MainContentTemplate.
            var content = new ContentPresenter() { Focusable = false, Content = new SelfWrapper(this) };
            content.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding { Source = this, Path = new PropertyPath(MainContentTemplateProperty) });

            this.Content = content;
            this.buttons = new ObservableCollection<Button>();
            this.Loaded += OnLoaded;
        }

        private ObservableCollection<Button> buttons;
        public ObservableCollection<Button> Buttons { get { return buttons; } }

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_DLGMODALFRAME = 0x0001;
        const int SWP_NOSIZE = 0x0001;
        const int SWP_NOMOVE = 0x0002;
        const int SWP_NOZORDER = 0x0004;
        const int SWP_FRAMECHANGED = 0x0020;
        const uint WM_SETICON = 0x0080;

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            // Set WS_EX_DLGMODALFRAME to get rid of the icon, and redraw the frame
            SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_DLGMODALFRAME);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
            SendMessage(hwnd, WM_SETICON, IntPtr.Zero, IntPtr.Zero);

            this.Loaded -= OnLoaded;
        }
    }

    public class DialogTextTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is string)
            {
                return this.TextTemplate;
            }

            return null;
        }
    }

    // This simple little class allows us to set the content of a content presenter to the dialog itself
    // with an indirect "hop" -- can't just set the dialog directly because it attempts to visually
    // parent the window into the presenter, which is not only not what we want, but WPF throws because
    // Window must be the top of a visual tree.
    public class SelfWrapper
    {
        public SelfWrapper(DialogBase dialog)
        {
            this.Dialog = dialog;
        }

        public DialogBase Dialog { get; private set; }
    }
}
