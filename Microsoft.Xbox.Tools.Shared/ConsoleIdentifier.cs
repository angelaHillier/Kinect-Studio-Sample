using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xbox.XTF.Console;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ConsoleIdentifier : INotifyPropertyChanged
    {
        static List<WeakReference> outstandingIdentifiers = new List<WeakReference>();

        string alias;
        string address;
        bool isDefault;

        public ConsoleIdentifier(string alias, string address, bool isDefault)
        {
            this.alias = alias;
            this.address = address;
            this.isDefault = isDefault;

            outstandingIdentifiers.Add(new WeakReference(this));
        }

        public string Alias
        {
            get { return this.alias; }
            set
            {
                if (this.alias != value)
                {
                    this.alias = value;
                    Notify("Alias");
                    Notify("DisplayName");
                }
            }
        }

        public string Address
        {
            get { return this.address; }
            set
            {
                if (this.address != value)
                {
                    this.address = value;
                    Notify("Address");
                    Notify("DisplayName");
                }
            }
        }

        public bool IsDefault
        {
            get { return this.isDefault; }
            set
            {
                if (this.isDefault != value)
                {
                    this.isDefault = value;
                    Notify("IsDefault");
                }
            }
        }

        public string DisplayName
        {
            get
            {
                if (this.alias == null)
                {
                    return this.address;
                }

                return string.Format("{0} ({1})", this.alias, this.address);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        static void AssignAlias(ConsoleIdentifier identifier, List<XtfConsole> consoles)
        {
            var console = consoles.FirstOrDefault(c => c.Address == identifier.Address);

            if (console == null)
            {
                identifier.Alias = null;
            }
            else
            {
                identifier.Alias = console.Alias;
            }
        }

        public static ConsoleIdentifier CreateConsole(string nameOrAddress)
        {
            using (var manager = new ConsoleManager())
            {
                var consoles = manager.GetConsoles();
                var defaultConsole = manager.GetDefaultConsole();
                XtfConsole console;

                if (string.IsNullOrEmpty(nameOrAddress))
                {
                    // Null means use the default console
                    console = defaultConsole;
                    if (console == null)
                    {
                        return null;
                    }
                }
                else
                {
                    // Try to find a console with an alias matching that given
                    console = consoles.FirstOrDefault(c => StringComparer.OrdinalIgnoreCase.Equals(c.Alias, nameOrAddress));

                    if (console == null)
                    {
                        // Not found by alias, try by address.
                        console = consoles.FirstOrDefault(c => StringComparer.OrdinalIgnoreCase.Equals(c.Address, nameOrAddress));

                        if (console == null)
                        {
                            // Couldn't find one, so we'll assume the value given is an address
                            return new ConsoleIdentifier(nameOrAddress, nameOrAddress, false);
                        }
                    }
                }

                return new ConsoleIdentifier(console.Alias, console.Address, StringComparer.OrdinalIgnoreCase.Equals(console.Alias, defaultConsole.Alias));
            }
        }

        public static void UpdateOutstandingIdentifiers(XtfConsole defaultConsole, List<XtfConsole> consoles)
        {
            var identifiers = outstandingIdentifiers.Where(r => r.Target is ConsoleIdentifier).Select(r => r.Target as ConsoleIdentifier).ToArray();

            foreach (var id in identifiers)
            {
                if (string.IsNullOrEmpty(id.Alias))
                {
                    // This one no longer had an alias -- assign it a new one (if possible)
                    AssignAlias(id, consoles);
                }
                else
                {
                    // This one has an alias.  If the alias still exists, it may be the same address or a different one.  We always keep
                    // the address of outstanding console ids, so if the address is different, we clear the Alias of the outstanding id.
                    var console = consoles.FirstOrDefault(c => c.Alias == id.Alias);

                    if (console == null || console.Address != id.Address)
                    {
                        // Either the alias is gone, or its address changed.  Either way, assign a new alias
                        AssignAlias(id, consoles);
                    }
                }

                id.IsDefault = (defaultConsole != null && id.Alias == defaultConsole.Alias);
            }

            // Prune the list to the live identifiers (just re-create)
            outstandingIdentifiers = identifiers.Select(i => new WeakReference(i)).ToList();
        }

        public static ConsoleIdentifier GetDefaultConsole()
        {
            using (var manager = new ConsoleManager())
            {
                var defaultConsole = manager.GetDefaultConsole();

                if (defaultConsole == null)
                {
                    return null;
                }

                return new ConsoleIdentifier(defaultConsole.Alias, defaultConsole.Address, true);
            }
        }

        void Notify(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
