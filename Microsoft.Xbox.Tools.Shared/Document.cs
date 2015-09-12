//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public abstract class Document : INotifyPropertyChanged
    {
        DocumentIdentity identity;
        bool isModified;

        public IServiceProvider ServiceProvider { get; private set; }
        protected DocumentManager DocumentManager { get; private set; }
        internal IInternalDocumentManager InternalDocumentManager { get { return (IInternalDocumentManager)this.DocumentManager; } }

        public bool IsModified
        {
            get
            {
                return this.isModified;
            }
            protected set
            {
                if (this.isModified != value)
                {
                    this.isModified = value;
                    Notify("IsModified");
                }
            }
        }
        public virtual string DisplayName { get { return this.identity == null ? string.Empty : this.identity.ShortDisplayName; } }
        public virtual bool AddToRecentDocuments { get { return false; } }

        protected Document()
        {
        }

        public HResult Initialize(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.DocumentManager = serviceProvider.GetService(typeof(DocumentManager)) as DocumentManager;

            HResult hr = this.OnInitialized();

            if (hr.Succeeded)
            {
                this.InternalDocumentManager.OnDocumentCreated(this);
                this.Category = ToolsUIApplication.Instance.DocumentCategories.FirstOrDefault(c => c.DocumentFactoryName == this.DocumentFactoryName);
            }

            return hr;
        }

        protected virtual HResult OnInitialized()
        {
            return HResult.S_OK;
        }

        public DocumentCategory Category { get; private set; }

        public virtual string PrimaryViewName { get { return null; } }

        // You can get this from the identity, but a document does not always have an identity (i.e., during initialization)
        public abstract string DocumentFactoryName { get; }

        public DocumentIdentity Identity
        {
            get
            {
                return this.identity;
            }
            protected set
            {
                this.identity = value;
                Notify("Identity");
            }
        }

        public void Close()
        {
            this.OnClosed();
            this.InternalDocumentManager.OnDocumentClosed(this);

            var handler = this.Closed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnClosed()
        {
        }

        protected void Notify(string property)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));

                if (property == "Identity")
                {
                    // Identity changes implies DisplayName changes
                    handler(this, new PropertyChangedEventArgs("DisplayName"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Closed;
    }
}
