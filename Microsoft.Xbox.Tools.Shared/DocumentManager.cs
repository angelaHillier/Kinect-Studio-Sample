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
using System.Linq;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class DocumentManager : IInternalDocumentManager, INotifyPropertyChanged
    {
        private ObservableCollection<Document> documentList;
        private ExtensionManager extensionManager;
        private IServiceProvider serviceProvider;

        public DocumentManager(IServiceProvider serviceProvider)
        {
            this.documentList = new ObservableCollection<Document>();
            this.Documents = new ReadOnlyObservableCollection<Document>(this.documentList);
            this.serviceProvider = serviceProvider;
        }

        public ReadOnlyObservableCollection<Document> Documents { get; private set; }

        public IDocumentFactory LookupDocumentFactory(string factoryName)
        {
            if (this.extensionManager == null)
            {
                this.extensionManager = (ExtensionManager)this.serviceProvider.GetService(typeof(ExtensionManager));
            }

            return this.extensionManager.LookupDocumentFactory(factoryName);
        }

        void IInternalDocumentManager.OnDocumentCreated(Document document)
        {
            this.documentList.Add(document);

            var handler = this.DocumentCreated;

            if (handler != null)
            {
                handler(this, new DocumentCreatedEventArgs(document));
            }
        }

        void IInternalDocumentManager.OnDocumentClosed(Document document)
        {
            this.documentList.Remove(document);
        }

        void Notify(string property)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DocumentCreatedEventArgs> DocumentCreated;
    }

    public class DocumentCreatedEventArgs : EventArgs
    {
        public Document Document { get; private set; }

        public DocumentCreatedEventArgs(Document document)
        {
            this.Document = document;
        }
    }
}
