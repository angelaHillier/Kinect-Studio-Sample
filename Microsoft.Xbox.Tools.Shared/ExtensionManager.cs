//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ExtensionManager
    {
        private string extensionDirectory;
        private string stateFile;
        private Dictionary<string, ExtensionData> extensions;
        private Dictionary<string, ServiceFactoryEntry> serviceTable;
        private Dictionary<string, DocumentFactoryEntry> documentFactoryTable;

        public ExtensionManager(string storageLocation)
        {
            this.extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.stateFile = Path.Combine(storageLocation, "Extensions.xml");
        }

        public IEnumerable<string> DocumentFactoryNames
        {
            get
            {
                return this.documentFactoryTable.Values.Select(e => e.RegisteredName);
            }
        }

        string MapCharacterToShortcutKeyText(string character)
        {
            if (character.Length > 0 && char.IsDigit(character, 0))
            {
                return "D" + character[0];
            }

            return character;
        }

        public List<IViewCreationCommand> BuildListOfViewCommands()
        {
            var factories = this.extensions.Values.SelectMany(e => e.ViewFactories).OrderBy(f => f.DisplayName);
            var usedKeys = new Dictionary<string, ViewFactoryEntry>(StringComparer.OrdinalIgnoreCase);
            bool defaultShortcutKeysComplete = true;

            // First pass -- prime the used keys table with factories' default shortcut keys so they are preferred.
            foreach (var factory in factories)
            {
                if (!string.IsNullOrEmpty(factory.DefaultShortcutKey) && !usedKeys.ContainsKey(factory.DefaultShortcutKey))
                {
                    usedKeys[factory.DefaultShortcutKey] = factory;
                }
                else
                {
                    // Need to do the second pass...
                    defaultShortcutKeysComplete = false;
                }
            }

            if (!defaultShortcutKeysComplete)
            {
                // Second pass -- use characters from the display name until we find an unused shortcut key.
                foreach (var factory in factories)
                {
                    if (!string.IsNullOrEmpty(factory.DefaultShortcutKey) && usedKeys[factory.DefaultShortcutKey] == factory)
                    {
                        // This one got its default.
                        continue;
                    }

                    string displayName = factory.DisplayName;
                    var accessChar = displayName.FirstOrDefault(c => char.IsLetterOrDigit(c) && !usedKeys.ContainsKey(c.ToString(CultureInfo.InvariantCulture)));

                    if (accessChar != default(char))
                    {
                        usedKeys[accessChar.ToString(CultureInfo.InvariantCulture)] = factory;
                    }
                    else
                    {
                        // This one's default shortcut key was in use (or it didn't have one), and no character in its display name
                        // can be used for a shortcut key.  Check the factories that map to each character in this factory's display name 
                        // for one that has unused characters in its display name.  If found, and it wasn't using its default shortcut 
                        // key, use it.  
                        //
                        // If NONE of the factories mapped to ANY of the chars in this one's name can use another character as a
                        // shortcut, we are in a bind and this one can't have a shortcut.
                        //
                        // One way around this:  Pick better default shortcut keys!
                        foreach (char c in displayName)
                        {
                            var cAsString = c.ToString(CultureInfo.InvariantCulture);

                            if (char.IsLetterOrDigit(c))
                            {
                                var otherFactory = usedKeys[cAsString];

                                if (!StringComparer.OrdinalIgnoreCase.Equals(otherFactory.DefaultShortcutKey, cAsString))
                                {
                                    var openChar = otherFactory.DisplayName.FirstOrDefault(ch => char.IsLetterOrDigit(ch) && !usedKeys.ContainsKey(ch.ToString(CultureInfo.InvariantCulture)));
                                    if (openChar != default(char))
                                    {
                                        usedKeys[openChar.ToString(CultureInfo.InvariantCulture)] = usedKeys[cAsString];
                                        usedKeys[cAsString] = factory;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (var pair in usedKeys)
            {
                pair.Value.ShortcutKey = MapCharacterToShortcutKeyText(pair.Key.ToUpperInvariant());
            }

            return factories.ToList<IViewCreationCommand>();
        }

        public IDictionary<string, IViewCreationCommand> BuildViewCreatorDictionary()
        {
            return BuildListOfViewCommands().ToDictionary(c => c.RegisteredName);
        }

        ViewFactoryEntry ParseViewFactoryElement(ExtensionData extensionData, XElement vfElement)
        {
            var entry = new ViewFactoryEntry(extensionData)
            {
                RegisteredName = vfElement.Attribute("RegisteredName").Value,
                DisplayName = vfElement.Attribute("DisplayName").Value,
                IsSingleInstance = bool.Parse(vfElement.Attribute("IsSingleInstance").Value),
                IsSingleInstancePerLayout = bool.Parse(vfElement.Attribute("IsSingleInstancePerLayout").Value),
                IsInternalOnly = bool.Parse(vfElement.Attribute("IsInternalOnly").Value),
                DefaultShortcutKey = vfElement.Attribute("DefaultShortcutKey").Value,
                TypeName = vfElement.Attribute("TypeName").Value
            };

            entry.DocumentFactoryAffinities.AddRange(vfElement.Elements("DocumentAffinity").Select(e => e.Attribute("FactoryName").Value));

            return entry;
        }

        public void LoadState(IEnumerable<string> additionalDefaultExtensions)
        {
            this.extensions = new Dictionary<string, ExtensionData>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(this.stateFile))
            {
                this.AddDefaultExtensions(additionalDefaultExtensions);

                try
                {
                    var doc = XDocument.Load(this.stateFile);
                    foreach (var extElement in doc.Element("Extensions").Elements("Extension"))
                    {
                        string path = extElement.Attribute("AssemblyPath").Value;

                        if (!File.Exists(path))
                            continue;

                        bool isCurrent = false;
                        long currentTimeStamp = File.GetLastWriteTime(path).ToFileTime();
                        XAttribute timeStampAttribute = extElement.Attribute("TimeStamp");
                        long lastTimeStamp;

                        if (timeStampAttribute != null && long.TryParse(timeStampAttribute.Value, out lastTimeStamp))
                        {
                            isCurrent = lastTimeStamp == currentTimeStamp;
                        }

                        XElement viewFactoriesElement = extElement.Element("ViewFactories");
                        XElement serviceFactoriesElement = extElement.Element("ServiceFactories");
                        XElement documentFactoriesElement = extElement.Element("DocumentFactories");
                        List<ViewFactoryEntry> viewFactories = new List<ViewFactoryEntry>();
                        List<ServiceFactoryEntry> serviceFactories = new List<ServiceFactoryEntry>();
                        List<DocumentFactoryEntry> documentFactories = new List<DocumentFactoryEntry>();
                        ExtensionData extensionData = new ExtensionData
                        {
                            AssemblyPath = path,
                            TimeStamp = currentTimeStamp,
                            ViewFactories = viewFactories,
                            ServiceFactories = serviceFactories,
                            DocumentFactories = documentFactories,
                            IsCurrent = isCurrent
                        };

                        this.extensions[path] = extensionData;

                        if (viewFactoriesElement != null)
                        {
                            viewFactories.AddRange(viewFactoriesElement.Elements("ViewFactory").Select(vfElement => ParseViewFactoryElement(extensionData, vfElement)));
                        }

                        if (serviceFactoriesElement != null)
                        {
                            serviceFactories.AddRange(serviceFactoriesElement.Elements("ServiceFactory").Select(sfElement => new ServiceFactoryEntry(extensionData)
                            {
                                ServiceTypeName = sfElement.Attribute("ServiceTypeName").Value,
                                FactoryTypeName = sfElement.Attribute("FactoryTypeName").Value
                            }));
                        }

                        if (documentFactoriesElement != null)
                        {
                            documentFactories.AddRange(documentFactoriesElement.Elements("DocumentFactory").Select(dfElement => new DocumentFactoryEntry(extensionData)
                            {
                                RegisteredName = dfElement.Attribute("RegisteredName").Value,
                                FactoryTypeName = dfElement.Attribute("FactoryTypeName").Value,
                            }));
                        }
                    }
                }
                catch (Exception)
                {
                    // State file is corrupt -- start from scratch...
                    this.extensions.Clear();
                    this.AddDefaultExtensions(additionalDefaultExtensions);
                }
            }
            else
            {
                // No state file -- start from scratch...
                this.AddDefaultExtensions(additionalDefaultExtensions);
            }

            // Make sure all are up to date
            foreach (var ext in this.extensions.Values)
            {
                if (!ext.IsCurrent)
                {
                    ext.UpdateExtension();
                }
            }

            // Build our service table
            this.serviceTable = new Dictionary<string, ServiceFactoryEntry>();
            foreach (var entry in this.extensions.Values.SelectMany(e => e.ServiceFactories))
            {
                this.serviceTable[entry.ServiceTypeName] = entry;
            }

            // ...and our document factory table
            this.documentFactoryTable = new Dictionary<string, DocumentFactoryEntry>();
            foreach (var entry in this.extensions.Values.SelectMany(e => e.DocumentFactories))
            {
                this.documentFactoryTable[entry.RegisteredName] = entry;
            }
        }

        public IDocumentFactory LookupDocumentFactory(string factoryName)
        {
            DocumentFactoryEntry factoryEntry;

            if (this.documentFactoryTable.TryGetValue(factoryName, out factoryEntry))
            {
                return factoryEntry.Factory;
            }

            return null;
        }

        void AddDefaultExtensions(IEnumerable<string> additionalDefaultExtensions)
        {
            var defaultPaths = new List<string>();

            var entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null && entryAssembly.Location != null)
            {
                defaultPaths.Add(entryAssembly.Location);
            }

            defaultPaths.Add(Assembly.GetExecutingAssembly().Location);

            foreach (var path in defaultPaths.Concat(additionalDefaultExtensions ?? Enumerable.Empty<string>()))
            {
                if (File.Exists(path))
                {
                    this.extensions[path] = new ExtensionData
                    {
                        AssemblyPath = path,
                        TimeStamp = File.GetLastWriteTime(path).ToFileTime(),
                        IsCurrent = false
                    };
                }
            }
        }

        public void SaveState()
        {
            var extensions = new XElement("Extensions",
                this.extensions.Values.Select(ext => new XElement("Extension",
                    new XAttribute("AssemblyPath", ext.AssemblyPath),
                    new XAttribute("TimeStamp", ext.TimeStamp),
                    new XElement("ViewFactories",
                        ext.ViewFactories.Select(vf => new XElement("ViewFactory",
                            new XAttribute("RegisteredName", vf.RegisteredName),
                            new XAttribute("DisplayName", vf.DisplayName),
                            new XAttribute("IsSingleInstance", vf.IsSingleInstance),
                            new XAttribute("IsSingleInstancePerLayout", vf.IsSingleInstancePerLayout),
                            new XAttribute("IsInternalOnly", vf.IsInternalOnly),
                            new XAttribute("DefaultShortcutKey", vf.DefaultShortcutKey),
                            new XAttribute("TypeName", vf.TypeName),
                            vf.DocumentFactoryAffinities.Select(name => new XElement("DocumentAffinity", new XAttribute("FactoryName", name)))))),
                    new XElement("DocumentFactories",
                        ext.DocumentFactories.Select(df => new XElement("DocumentFactory",
                            new XAttribute("RegisteredName", df.RegisteredName),
                            new XAttribute("FactoryTypeName", df.FactoryTypeName)))),
                    new XElement("ServiceFactories",
                        ext.ServiceFactories.Select(sf => new XElement("ServiceFactory",
                            new XAttribute("ServiceTypeName", sf.ServiceTypeName),
                            new XAttribute("FactoryTypeName", sf.FactoryTypeName)))))));

            var document = new XDocument(extensions);
            document.Save(this.stateFile);
        }

        public object CreateProvidedService(Type serviceType, IServiceProvider serviceProvider)
        {
            var serviceTypeName = serviceType.AssemblyQualifiedName;
            ServiceFactoryEntry entry;

            if (this.serviceTable.TryGetValue(serviceTypeName, out entry))
            {
                return entry.CreateService(serviceType, serviceProvider);
            }

            return null;
        }

        private class ExtensionData
        {
            public string AssemblyPath { get; set; }
            public long TimeStamp { get; set; }
            public Assembly LoadedAssembly { get; private set; }
            public List<ViewFactoryEntry> ViewFactories { get; set; }
            public List<ServiceFactoryEntry> ServiceFactories { get; set; }
            public List<DocumentFactoryEntry> DocumentFactories { get; set; }
            public bool IsCurrent { get; set; }

            ViewFactoryEntry CreateViewFactoryEntry(ViewFactoryAttribute a, Type factoryType, IViewFactory factory)
            {
                var entry = new ViewFactoryEntry(this)
                {
                    RegisteredName = a.RegisteredName,
                    DisplayName = factory.GetViewDisplayName(a.RegisteredName),
                    TypeName = factoryType.AssemblyQualifiedName,
                    IsSingleInstance = a.IsSingleInstance,
                    IsSingleInstancePerLayout = a.IsSingleInstancePerLayout,
                    IsInternalOnly = a.IsInternalOnly,
                    DefaultShortcutKey = string.IsNullOrEmpty(a.DefaultShortcutKey) ? string.Empty : a.DefaultShortcutKey.ToUpperInvariant(),
                    Factory = factory
                };

                if (!string.IsNullOrEmpty(a.DocumentAffinities))
                {
                    entry.DocumentFactoryAffinities.AddRange(a.DocumentAffinities.Split('|'));
                }

                return entry;
            }

            public void UpdateExtension()
            {
                this.ViewFactories = new List<ViewFactoryEntry>();
                this.ServiceFactories = new List<ServiceFactoryEntry>();
                this.DocumentFactories = new List<DocumentFactoryEntry>();

                try
                {
                    this.LoadAssembly();

                    foreach (var type in this.LoadedAssembly.GetTypes())
                    {
                        var factoryAttributes = type.GetCustomAttributes(typeof(ViewFactoryAttribute), false);

                        if (factoryAttributes != null && factoryAttributes.Length > 0)
                        {
                            IViewFactory factory;

                            if (this.InstantiateFactory(type.AssemblyQualifiedName, out factory))
                            {
                                this.ViewFactories.AddRange(factoryAttributes.OfType<ViewFactoryAttribute>().Select(a => CreateViewFactoryEntry(a, type, factory)));
                            }
                        }

                        factoryAttributes = type.GetCustomAttributes(typeof(ServiceFactoryAttribute), false);

                        if (factoryAttributes != null && factoryAttributes.Length > 0)
                        {
                            IServiceFactory factory;

                            if (this.InstantiateFactory(type.AssemblyQualifiedName, out factory))
                            {
                                this.ServiceFactories.AddRange(factoryAttributes.OfType<ServiceFactoryAttribute>().Select(a => new ServiceFactoryEntry(this)
                                {
                                    ServiceTypeName = a.ServiceType.AssemblyQualifiedName,
                                    FactoryTypeName = type.AssemblyQualifiedName,
                                    Factory = factory
                                }));
                            }
                        }

                        factoryAttributes = type.GetCustomAttributes(typeof(DocumentFactoryAttribute), false);

                        if (factoryAttributes != null && factoryAttributes.Length > 0)
                        {
                            IDocumentFactory factory;

                            if (this.InstantiateFactory(type.AssemblyQualifiedName, out factory))
                            {
                                this.DocumentFactories.AddRange(factoryAttributes.OfType<DocumentFactoryAttribute>().Select(a => new DocumentFactoryEntry(this)
                                {
                                    RegisteredName = a.FactoryName,
                                    FactoryTypeName = type.AssemblyQualifiedName,
                                    Factory = factory
                                }));
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // TODO_LOG
                }
            }

            private void LoadAssembly()
            {
                if (this.LoadedAssembly == null)
                    this.LoadedAssembly = Assembly.LoadFile(this.AssemblyPath);
            }

            public bool InstantiateFactory<T>(string typeName, out T factory) where T : class
            {
                this.LoadAssembly();
                factory = Activator.CreateInstance(Type.GetType(typeName)) as T;
                return factory != null;
            }
        }

        private class ViewFactoryEntry : IViewCreationCommand
        {
            private ExtensionData extensionData;

            public ViewFactoryEntry(ExtensionData extensionData)
            {
                this.extensionData = extensionData;
                this.DocumentFactoryAffinities = new List<string>();
            }

            public string RegisteredName { get; set; }
            public string DisplayName { get; set; }
            public string DisplayNameWithAccessText { get; set; }
            public string DefaultShortcutKey { get; set; }
            public string ShortcutKey { get; set; }
            public string TypeName { get; set; }
            public IViewFactory Factory { get; set; }
            public bool IsSingleInstance { get; set; }
            public bool IsSingleInstancePerLayout { get; set; }
            public bool IsInternalOnly { get; set; }
            public List<string> DocumentFactoryAffinities { get; private set; }

            IEnumerable<string> IViewCreationCommand.DocumentFactoryAffinities { get { return this.DocumentFactoryAffinities; } }

            View IViewCreationCommand.CreateView(IServiceProvider serviceProvider)
            {
                if (this.Factory == null)
                {
                    IViewFactory factory;

                    if (!this.extensionData.InstantiateFactory(this.TypeName, out factory))
                    {
                        return null;
                    }

                    this.Factory = factory;
                }

                object viewOrContent = this.Factory.CreateView(this.RegisteredName, serviceProvider);
                View view = viewOrContent as View;

                if (view == null)
                {
                    view = new DefaultView(viewOrContent);
                }

                view.ViewCreationCommand = this;
                view.Initialize(serviceProvider);
                return view;
            }

            public override string ToString()
            {
                return "View Factory: " + DisplayName;
            }
        }

        private class DefaultView : View
        {
            FrameworkElement viewContent;

            public DefaultView(object content)
            {
                this.viewContent = content as FrameworkElement;

                if (this.viewContent == null)
                {
                    this.viewContent = new ContentPresenter { Content = content };
                }
            }

            protected override FrameworkElement CreateViewContent()
            {
                return this.viewContent;
            }
        }

        private class ServiceFactoryEntry
        {
            private ExtensionData extensionData;

            public ServiceFactoryEntry(ExtensionData extensionData)
            {
                this.extensionData = extensionData;
            }

            public string ServiceTypeName { get; set; }
            public string FactoryTypeName { get; set; }
            public IServiceFactory Factory { get; set; }

            public object CreateService(Type serviceType, IServiceProvider serviceProvider)
            {
                if (this.Factory == null)
                {
                    IServiceFactory factory;

                    if (!this.extensionData.InstantiateFactory(this.FactoryTypeName, out factory))
                    {
                        return null;
                    }

                    this.Factory = factory;
                }

                return this.Factory.CreateService(serviceType, serviceProvider);
            }
        }

        private class DocumentFactoryEntry
        {
            private ExtensionData extensionData;
            private IDocumentFactory factory;

            public DocumentFactoryEntry(ExtensionData extensionData)
            {
                this.extensionData = extensionData;
            }

            public string RegisteredName { get; set; }
            public string FactoryTypeName { get; set; }

            public IDocumentFactory Factory
            {
                get
                {
                    if (this.factory == null)
                    {
                        if (!this.extensionData.InstantiateFactory(this.FactoryTypeName, out this.factory))
                        {
                            return null;
                        }
                    }
                    return this.factory;
                }
                set
                {
                    this.factory = value;
                }
            }
        }
    }
}
