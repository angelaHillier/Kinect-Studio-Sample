//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class RecentDocumentService : ServiceBase
    {
        const int MaxRecentDocuments = 20;

        DocumentManager serviceFieldDocumentManager;
        ISessionStateService serviceFieldSessionStateService;
        ObservableCollection<DocumentIdentity> documentList;
        ObservableCollection<DirectoryInfo> recentFolders;
        ReadOnlyObservableCollection<DocumentIdentity> readOnlyDocumentList;
        ReadOnlyObservableCollection<DirectoryInfo> readOnlyRecentFolders;

        public DocumentManager DocumentManager { get { return EnsureService(ref this.serviceFieldDocumentManager); } }
        public ISessionStateService SessionStateService { get { return EnsureService(ref this.serviceFieldSessionStateService); } }

        public ReadOnlyObservableCollection<DocumentIdentity> DocumentIdentities
        {
            get
            {
                if (this.documentList == null)
                {
                    this.documentList = new ObservableCollection<DocumentIdentity>();
                    this.readOnlyDocumentList = new ReadOnlyObservableCollection<DocumentIdentity>(this.documentList);

                    this.DocumentManager.DocumentCreated += OnDocumentCreated;

                    foreach (var doc in this.DocumentManager.Documents)
                    {
                        ObserveDocument(doc);
                    }

                    this.SessionStateService.StateSaveRequested += OnStateSaveRequested;

                    ReadState();
                }

                return this.readOnlyDocumentList;
            }
        }

        public ReadOnlyObservableCollection<DirectoryInfo> RecentFolders
        {
            get
            {
                if (this.readOnlyRecentFolders == null)
                {
                    this.readOnlyRecentFolders = new ReadOnlyObservableCollection<DirectoryInfo>(this.recentFolders);
                }

                return this.readOnlyRecentFolders;
            }
        }

        public void RemoveDocument(DocumentIdentity documentIdentity)
        {
            this.documentList.Remove(documentIdentity);
        }

        void ObserveDocument(Document document)
        {
            document.Closed += OnDocumentClosed;
            document.PropertyChanged += OnDocumentPropertyChanged;
            UpdateList(document);
        }

        void OnDocumentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var doc = sender as Document;

            if (doc != null && e.PropertyName == "Identity")
            {
                UpdateList(doc);
            }
        }

        void OnDocumentClosed(object sender, EventArgs e)
        {
            var doc = sender as Document;

            if (doc != null)
            {
                doc.Closed -= OnDocumentClosed;
                doc.PropertyChanged -= OnDocumentPropertyChanged;
            }
        }

        void OnDocumentCreated(object sender, DocumentCreatedEventArgs e)
        {
            ObserveDocument(e.Document);
        }

        void UpdateList(Document document)
        {
            if (document.Identity == null || !document.AddToRecentDocuments)
            {
                return;
            }

            var existing = this.documentList.FirstOrDefault(d => d.Equals(document.Identity));

            if (existing == null)
            {
                this.documentList.Insert(0, document.Identity);
                if (this.documentList.Count > MaxRecentDocuments)
                {
                    this.documentList.RemoveAt(this.documentList.Count - 1);
                }
            }
            else
            {
                this.documentList.Remove(existing);
                this.documentList.Insert(0, existing);
            }

            var factory = this.DocumentManager.LookupDocumentFactory(document.Identity.FactoryName);
            string fileName;

            if (factory != null && factory.TryGetFileName(document.Identity, out fileName))
            {
                UpdateRecentFoldersList(fileName, true);
            }
        }

        void UpdateRecentFoldersList(string fileName, bool reorder)
        {
            var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(fileName));
            var existing = this.recentFolders.Select((di, i) => new { Info = di, Index = i }).FirstOrDefault(e => StringComparer.OrdinalIgnoreCase.Equals(e.Info.FullName, directoryInfo.FullName));

            if (existing == null)
            {
                this.recentFolders.Insert(0, directoryInfo);
                while (this.recentFolders.Count > 9)
                {
                    this.recentFolders.RemoveAt(9);
                }
            }
            else if (reorder)
            {
                this.recentFolders.RemoveAt(existing.Index);
                this.recentFolders.Insert(0, directoryInfo);
            }
        }

        void ReadState()
        {
            try
            {
                var docsElement = this.SessionStateService.GetSessionState("RecentDocuments");

                if (docsElement != null)
                {
                    this.documentList.Clear();
                    foreach (var doc in docsElement.Elements("Document"))
                    {
                        string factoryName = doc.Attribute("FactoryName").Value;
                        string moniker = doc.Attribute("Moniker").Value;
                        string fileName;
                        var factory = this.DocumentManager.LookupDocumentFactory(factoryName);
                        var identity = factory.CreateDocumentIdentity(moniker);

                        this.documentList.Add(identity);
                        if (factory.TryGetFileName(identity, out fileName))
                        {
                            UpdateRecentFoldersList(fileName, false);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        void OnStateSaveRequested(object sender, EventArgs e)
        {
            this.SessionStateService.SetSessionState("RecentDocuments", new XElement("RecentDocuments",
                this.documentList.Select(d => new XElement("Document",
                    new XAttribute("FactoryName", d.FactoryName),
                    new XAttribute("Moniker", d.Moniker)))));
        }

        internal RecentDocumentService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.recentFolders = new ObservableCollection<DirectoryInfo>();
        }
    }
}
