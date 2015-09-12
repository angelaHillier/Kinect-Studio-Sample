using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;

#if !KINECT_FOR_WINDOWS
using Microsoft.Xbox.XTF;
using Microsoft.Xbox.XTF.Console;
#endif

using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Xbox.Tools.Shared
{
    /// <summary>
    /// Interaction logic for SelectDevkitDialog.xaml
    /// </summary>
    public partial class SelectDevkitDialog : DialogBase
    {
    #if KINECT_FOR_WINDOWS
        public SelectDevkitDialog(IServiceProvider sp) { }
        void OnOkButtonClicked(object sender, RoutedEventArgs e) { }
        void OnCancelClicked(object sender, RoutedEventArgs e) { }
        void OnAddClicked(object sender, RoutedEventArgs e) { }
        void OnRemoveClicked(object sender, RoutedEventArgs e) { }
        void OnEditClicked(object sender, RoutedEventArgs e) { }
        void OnSetAsDefaultClicked(object sender, RoutedEventArgs e) { }
        void OnRefreshClicked(object sender, RoutedEventArgs e) { }
    #else
        public static readonly DependencyProperty DefaultKitProperty = DependencyProperty.Register(
            "DefaultKit", typeof(string), typeof(SelectDevkitDialog));

        public static readonly DependencyProperty SpecificKitProperty = DependencyProperty.Register(
            "SpecificKit", typeof(string), typeof(SelectDevkitDialog));

        public string DefaultKit
        {
            get { return (string)GetValue(DefaultKitProperty); }
            set { SetValue(DefaultKitProperty, value); }
        }

        public string SpecificKit
        {
            get { return (string)GetValue(SpecificKitProperty); }
            set { SetValue(SpecificKitProperty, value); }
        }

        public ObservableCollection<ConsoleWrapper> Consoles { get; private set; }
        public ConsoleIdentifier SelectedConsole { get; private set; }

        IUserNotificationService notificationService;
        ConsoleWrapper consoleInEditMode;
        string oldEditedConsoleAlias;
        string oldEditedConsoleAddress;
        ListView listview;
        ConsoleManager consoleManager;
        Button addButton;
        Button removeButton;
        Button editButton;
        Button setAsDefaultButton;
        List<ConsoleWrapper> removals;
        bool changesMade;

        public SelectDevkitDialog(IServiceProvider sp)
        {
            this.Consoles = new ObservableCollection<ConsoleWrapper>();
            InitializeComponent();
            this.DataContext = this;
            this.Loaded += SelectDevkitDialog_Loaded;
            this.notificationService = sp.GetService(typeof(IUserNotificationService)) as IUserNotificationService;

            this.consoleManager = new ConsoleManager();
            this.removals = new List<ConsoleWrapper>();
            InitConsolesList();

            this.consoleManager.ConsoleAdded += OnConsolesChanged;
            this.consoleManager.ConsoleRemoved += OnConsolesChanged;
            this.consoleManager.DefaultConsoleChanged += OnConsolesChanged;
        }

        void OnConsolesChanged(object sender, ConsoleEventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)(() => { InitConsolesList(); }));
        }

        void InitConsolesList()
        {
            if (changesMade)
            {
                // (any non-null value will work -- the display is controlled by FootnoteTemplate in the xaml...)
                this.Footnote = true;
                return;
            }

            var defaultConsole = this.consoleManager.GetDefaultConsole();
            string defaultConsoleAlias = defaultConsole == null ? null : defaultConsole.Alias;
            string selectedConsoleAlias = null;
            ConsoleWrapper selectedConsole = null;

            if (this.listview != null)
            {
                var selected = this.listview.SelectedItem as ConsoleWrapper;

                if (selected != null)
                {
                    selectedConsoleAlias = selected.Alias;
                }
            }

            this.Consoles.Clear();
            this.consoleInEditMode = null;

            var consoles = this.consoleManager.GetConsoles();

            ConsoleIdentifier.UpdateOutstandingIdentifiers(defaultConsole, consoles);

            foreach (var c in consoles)
            {
                var newWrapper = new ConsoleWrapper(c, c.Alias == defaultConsoleAlias);
                this.Consoles.Add(newWrapper);
                if (c.Alias == selectedConsoleAlias)
                {
                    selectedConsole = newWrapper;
                }
            }

            if (selectedConsole != null)
            {
                this.listview.SelectedItem = selectedConsole;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            var selectedConsole = this.listview.SelectedItem as ConsoleWrapper;

            if (e.Key == Key.Escape)
            {
                if (this.consoleInEditMode != null)
                {
                    if (this.consoleInEditMode.IsNew)
                    {
                        this.Consoles.Remove(this.consoleInEditMode);
                    }

                    SetConsoleInEditMode(null, e.Key == Key.Escape);
                    e.Handled = true;
                    return;
                }

                this.DialogResult = false;
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Enter)
            {
                if (this.consoleInEditMode != null)
                {
                    SetConsoleInEditMode(null, false);
                    e.Handled = true;
                    return;
                }
            }
            else if (e.Key == Key.F2)
            {
                if (this.consoleInEditMode == null && selectedConsole != null)
                {
                    SetConsoleInEditMode(selectedConsole, false);
                    e.Handled = true;
                    return;
                }
            }
            else if (e.Key == Key.Tab && this.consoleInEditMode != null)
            {
                // Tabbing out of address text box (or shift-tabbing out of alias text box) drops out of edit mode
                var focus = e.KeyboardDevice.FocusedElement as TextBox;
                string textBoxName = ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) ? "aliasTextBox" : "addressTextBox";

                if (focus != null && focus.Name == textBoxName)
                {
                    SetConsoleInEditMode(null, false);
                    e.Handled = true;
                    return;
                }
            }

            base.OnPreviewKeyDown(e);
        }

        void SelectDevkitDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.dialogContent != null && this.dialogContent.Template != null)
            {
                this.listview = this.dialogContent.Template.FindName("consoleListView", this.dialogContent) as ListView;

                if (this.listview == null)
                {
                    // Fix your template...
                    throw new InvalidOperationException();
                }

                this.listview.SelectionChanged += OnConsoleListSelectionChanged;
                this.addButton = this.dialogContent.Template.FindName("addButton", this.dialogContent) as Button;
                this.removeButton = this.dialogContent.Template.FindName("removeButton", this.dialogContent) as Button;
                this.editButton = this.dialogContent.Template.FindName("editButton", this.dialogContent) as Button;
                this.setAsDefaultButton = this.dialogContent.Template.FindName("setAsDefaultButton", this.dialogContent) as Button;
                SetButtonStates();
            }
        }

        void SetButtonStates()
        {
            var selectedConsole = this.listview.SelectedItem as ConsoleWrapper;

            if (selectedConsole == null)
            {
                this.removeButton.IsEnabled = false;
                this.editButton.IsEnabled = false;
                this.setAsDefaultButton.IsEnabled = false;
                this.okButton.IsEnabled = false;
            }
            else
            {
                this.removeButton.IsEnabled = true;
                this.editButton.IsEnabled = !selectedConsole.InEditMode;
                this.setAsDefaultButton.IsEnabled = !selectedConsole.IsDefault;
                this.okButton.IsEnabled = true;
            }
        }

        void OnConsoleListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetConsoleInEditMode(null, false);
            SetButtonStates();
        }

        void SetConsoleInEditMode(ConsoleWrapper console, bool revertChanges)
        {
            if (this.consoleInEditMode != null)
            {
                this.consoleInEditMode.InEditMode = false;
                if (revertChanges)
                {
                    this.consoleInEditMode.Alias = oldEditedConsoleAlias;
                    this.consoleInEditMode.Address = oldEditedConsoleAddress;
                }
                else
                {
                    this.consoleInEditMode.IsNew = false;
                }
            }

            this.consoleInEditMode = console;

            if (this.consoleInEditMode != null)
            {
                this.changesMade = true;

                console.InEditMode = true;
                this.oldEditedConsoleAlias = console.Alias;
                this.oldEditedConsoleAddress = console.Address;

                EventHandler handler = null;
                handler = (s, e) =>
                {
                    if (this.listview.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        var container = this.listview.ItemContainerGenerator.ContainerFromItem(this.consoleInEditMode);

                        if (container != null)
                        {
                            this.listview.ItemContainerGenerator.StatusChanged -= handler;

                            var textBoxes = container.FindVisualChildren<TextBox>();
                            bool focused = false;

                            foreach (var textBox in textBoxes)
                            {
                                if (!focused)
                                {
                                    focused = textBox.Focus();
                                }
                                textBox.SelectAll();
                            }
                        }
                    }
                };

                this.listview.ItemContainerGenerator.StatusChanged += handler;
                handler(null, EventArgs.Empty);
            }

            SetButtonStates();
        }

        void OnAddClicked(object sender, RoutedEventArgs e)
        {
            var newWrapper = new ConsoleWrapper(null, false);
            this.Consoles.Add(newWrapper);
            this.listview.SelectedItem = newWrapper;
            SetConsoleInEditMode(newWrapper, false);
            SetButtonStates();
        }

        void OnRemoveClicked(object sender, RoutedEventArgs e)
        {
            var console = this.listview.SelectedItem as ConsoleWrapper;

            if (console != null)
            {
                int index = this.listview.SelectedIndex;

                this.changesMade = true;
                if (console.RealConsole != null)
                {
                    this.removals.Add(console);
                }

                SetConsoleInEditMode(null, false);
                this.Consoles.Remove(this.listview.SelectedItem as ConsoleWrapper);
                this.listview.SelectedIndex = Math.Min(this.Consoles.Count - 1, index);
            }
        }

        void OnEditClicked(object sender, RoutedEventArgs e)
        {
            var console = this.listview.SelectedItem as ConsoleWrapper;

            if (console != null)
            {
                SetConsoleInEditMode(console, false);
            }

            SetButtonStates();
        }

        void OnSetAsDefaultClicked(object sender, RoutedEventArgs e)
        {
            if (this.listview == null)
                return;

            this.changesMade = true;

            var currentDefault = this.Consoles.FirstOrDefault(c => c.IsDefault);

            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
            }

            var newDefault = this.listview.SelectedItem as ConsoleWrapper;

            if (newDefault != null)
            {
                newDefault.IsDefault = true;
            }

            SetButtonStates();
        }

        void OnRefreshClicked(object sender, RoutedEventArgs e)
        {
            this.Footnote = null;
            this.changesMade = false;
            InitConsolesList();
        }

        void OnOkButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.consoleInEditMode != null)
            {
                SetConsoleInEditMode(null, false);
                return;
            }

            var selectedConsole = this.listview.SelectedItem as ConsoleWrapper;

            if (selectedConsole == null)
            {
                return;
            }

            if (this.changesMade)
            {
                var dupeTable = new Dictionary<string, ConsoleWrapper>();

                // Check the console aliases for duplicates
                foreach (var c in this.Consoles)
                {
                    ConsoleWrapper dupe;

                    if (dupeTable.TryGetValue(c.Alias, out dupe))
                    {
                        this.notificationService.ShowError(string.Format(StringResources.ConsoleAliasAlreadyExistsFmt, c.Alias), HResult.FromErrorText(StringResources.ConsoleAliasesMustBeUnique));
                        this.listview.SelectedItem = c;
                        return;
                    }
                    dupeTable[c.Alias] = c;
                }

                // No duplicates... let's try to make the console manager look like what we've got.
                try
                {
                    // First, nuke the pure removals
                    foreach (var c in this.removals)
                    {
                        // NOTE:  Remove by the real console alias.  The user might have changed the local one...
                        this.consoleManager.RemoveConsole(c.RealConsole.Alias);
                    }

                    // Now remove anything that has changed.  The replacements list ends up being all consoles that
                    // need to be written, either because they're new, or changed (removed + added).
                    var replacements = new List<ConsoleWrapper>();

                    foreach (var c in this.Consoles)
                    {
                        if (c.RealConsole != null && (c.RealConsole.Alias != c.Alias || c.RealConsole.Address != c.Address))
                        {
                            this.consoleManager.RemoveConsole(c.RealConsole.Alias);
                            replacements.Add(c);
                        }
                        else if (c.RealConsole == null)
                        {
                            replacements.Add(c);
                        }
                    }

                    // Re-add everything in replacements list
                    foreach (var c in replacements)
                    {
                        this.consoleManager.AddConsole(c.Alias, c.Address);
                    }

                    // Set the default kit
                    var defaultConsole = this.Consoles.FirstOrDefault(c => c.IsDefault);

                    if (defaultConsole != null)
                    {
                        // NOTE:  If the user deletes the default console and doesn't set a new one, the console
                        // manager automatically clears the (now bogus) default console specification.
                        this.consoleManager.SetDefaultConsole(defaultConsole.Alias);
                    }
                }
                catch (Exception ex)
                {
                    this.notificationService.ShowError(HResult.FromException(ex));

                    // This is somewhat bad... we possibly made *some* of our changes, so chances are that 
                    // trying again will end up failing again, even if the user fixed the original problem.  
                    // Best hope is to reset the dialog to whatever state the actual list is in.
                    InitConsolesList();
                    return;
                }
            }

            if (selectedConsole.IsDefault)
            {
                this.SpecificKit = null;
            }
            else
            {
                this.SpecificKit = selectedConsole.Address;
            }

            this.SelectedConsole = new ConsoleIdentifier(selectedConsole.Alias, selectedConsole.Address, selectedConsole.IsDefault);

            // Re-update the outstanding console identifiers
            ConsoleIdentifier.UpdateOutstandingIdentifiers(this.consoleManager.GetDefaultConsole(), this.consoleManager.GetConsoles());
            DialogResult = true;
        }

        void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            if (this.consoleInEditMode != null)
            {
                SetConsoleInEditMode(null, true);
                return;
            }

            DialogResult = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            this.consoleManager.ConsoleAdded -= OnConsolesChanged;
            this.consoleManager.ConsoleRemoved -= OnConsolesChanged;
            this.consoleManager.DefaultConsoleChanged -= OnConsolesChanged;

            this.consoleManager.Dispose();
            this.consoleManager = null;
            base.OnClosed(e);
        }

        public static string XtfDefaultAddress
        {
            get
            {
                return ConsoleManager.GetDefaultAddress();
            }
        }

        public class ConsoleWrapper : DependencyObject
        {
            public static readonly DependencyProperty AliasProperty = DependencyProperty.Register(
                "Alias", typeof(string), typeof(ConsoleWrapper));

            public static readonly DependencyProperty AddressProperty = DependencyProperty.Register(
                "Address", typeof(string), typeof(ConsoleWrapper));

            public static readonly DependencyProperty InEditModeProperty = DependencyProperty.Register(
                "InEditMode", typeof(bool), typeof(ConsoleWrapper));

            public static readonly DependencyProperty IsDefaultProperty = DependencyProperty.Register(
                "IsDefault", typeof(bool), typeof(ConsoleWrapper));

            public string Alias
            {
                get { return (string)GetValue(AliasProperty); }
                set { SetValue(AliasProperty, value); }
            }

            public string Address
            {
                get { return (string)GetValue(AddressProperty); }
                set { SetValue(AddressProperty, value); }
            }

            public bool InEditMode
            {
                get { return (bool)GetValue(InEditModeProperty); }
                set { SetValue(InEditModeProperty, value); }
            }

            public bool IsDefault
            {
                get { return (bool)GetValue(IsDefaultProperty); }
                set { SetValue(IsDefaultProperty, value); }
            }

            public bool IsNew { get; set; }     // Used in code only... if true, ESC key removes the entry altogether
            public XtfConsole RealConsole { get; private set; }

            public ConsoleWrapper(XtfConsole console, bool isDefault)
            {
                this.RealConsole = console;
                if (console != null)
                {
                    this.Alias = console.Alias;
                    this.Address = console.Address;
                }
                else
                {
                    this.Alias = "New Console";
                    this.Address = "0.0.0.0";
                    this.IsNew = true;
                }

                this.IsDefault = isDefault;
            }
        }
#endif
    }
}
