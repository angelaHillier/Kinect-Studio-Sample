//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Kinect.Tools;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class WritableMetadataProxy : ObservableCollection<MetadataKeyValuePair>
    {
        public WritableMetadataProxy(KStudioEventFile file, KStudioMetadata metadata)
        {
            DebugHelper.AssertUIThread();

            if (metadata == null)
            {
                throw new ArgumentNullException("metadata");
            }

            if (metadata.IsReadOnly)
            {
                throw new ArgumentOutOfRangeException("metadata");
            }

            this.file = file as KStudioWritableEventFile;
            this.stream = null;
            this.metadata = metadata;
            this.metadata.CollectionChanged += Notify_CollectionChanged;

            lock (this.metadata)
            {
                LoadCollection();
            }
        }

        public WritableMetadataProxy(KStudioEventFile file, KStudioEventStream stream, KStudioMetadata metadata)
        {
            DebugHelper.AssertUIThread();

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (metadata == null)
            {
                throw new ArgumentNullException("metadata");
            }

            this.file = file as KStudioWritableEventFile;
            this.stream = stream;
            this.metadata = metadata;
            this.metadata.CollectionChanged += Notify_CollectionChanged;

            lock (this.metadata)
            {
                LoadCollection();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public object this[string key]
        {
            get
            {
                DebugHelper.AssertUIThread();

                Debug.Assert(this.metadata != null);

                lock (this.metadata)
                {
                    MetadataKeyValuePair found = this.FirstOrDefault((kv) => kv.Key == key);
                    if (found == null)
                    {
                        throw new ArgumentOutOfRangeException("key");
                    }

                    return found.Value;
                }
            }
            set
            {
                DebugHelper.AssertUIThread();

                Debug.Assert(this.metadata != null);

                lock (this.metadata)
                {
                    bool flush = false;
                    MetadataKeyValuePair found = this.FirstOrDefault((kv) => kv.Key == key);
                    if (found == null)
                    {
                        if (value != null)
                        {
                            this.metadata.Add(key, value);
                            flush = true;
                        }
                    }
                    else
                    {
                        if (value == null)
                        {
                            this.metadata.Remove(key);
                            flush = true;
                        }
                        else
                        {
                            found.Value = value;
                            flush = true;
                        }
                    }

                    if (flush && this.file != null)
                    {
                        this.file.FlushIndex();
                    }
                }
            }
        }

        public bool ContainsKey(string key)
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(this.metadata != null);

            bool value = false;

            lock (this.metadata)
            {
                MetadataKeyValuePair found = this.FirstOrDefault((kv) => kv.Key == key);
                value = (found != null);
            }

            return value;
        }

        internal void GetStreamIds(out Guid dataTypeId, out Guid semanticId)
        {
            if (this.stream == null)
            {
                throw new InvalidOperationException("trying to treat file metadata as stream metadata");
            }

            dataTypeId = this.stream.DataTypeId;
            semanticId = this.stream.DataTypeId;
        }

        internal void SetMetadata(string key, object value)
        {
            Debug.Assert(this.metadata != null);

            lock (this.metadata)
            {
                this.metadata[key] = value;
                if (this.file != null)
                {
                    this.file.FlushIndex();
                }
            }
        }

        private void Notify_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(this.metadata != null);

            if (e != null)
            {
                lock (this.metadata)
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            for (int i = 0; i < e.NewItems.Count; ++i)
                            {
                                Debug.Assert(e.NewItems[i] is KeyValuePair<string, object>);

                                KeyValuePair<string, object> newItem = (KeyValuePair<string, object>)e.NewItems[i];

                                MetadataKeyValuePair found = this.FirstOrDefault((kv) => kv.Key == newItem.Key);
                                if (found == null)
                                {
                                    this.Add(new MetadataKeyValuePair(this, newItem.Key, newItem.Value));
                                }
                                else
                                {
                                    found.SetValue(newItem.Value);
                                }
                            }
                            break;

                        case NotifyCollectionChangedAction.Move:
                            // ignore
                            break;

                        case NotifyCollectionChangedAction.Remove:
                            for (int i = 0; i < e.OldItems.Count; ++i)
                            {
                                Debug.Assert(e.OldItems[i] is KeyValuePair<string, object>);

                                KeyValuePair<string, object> oldItem = (KeyValuePair<string, object>)e.OldItems[i];

                                MetadataKeyValuePair found = this.FirstOrDefault((kv) => kv.Key == oldItem.Key);
                                if (found != null)
                                {
                                    this.Remove(found);
                                }
                            }
                            break;

                        case NotifyCollectionChangedAction.Replace:
                            {
                                Debug.Assert(e.OldItems.Count == e.NewItems.Count);

                                int count = Math.Min(e.OldItems.Count, e.NewItems.Count);

                                for (int i = 0; i < count; ++i)
                                {
                                    Debug.Assert(e.OldItems[i] is KeyValuePair<string, object>);
                                    Debug.Assert(e.NewItems[i] is KeyValuePair<string, object>);

                                    KeyValuePair<string, object> oldItem = (KeyValuePair<string, object>)e.OldItems[i];
                                    KeyValuePair<string, object> newItem = (KeyValuePair<string, object>)e.NewItems[i];

                                    Debug.Assert(oldItem.Key == newItem.Key);

                                    MetadataKeyValuePair found = this.FirstOrDefault((kv) => kv.Key == newItem.Key);
                                    if (found == null)
                                    {
                                        this.Add(new MetadataKeyValuePair(this, newItem.Key, newItem.Value));
                                    }
                                    else
                                    {
                                        found.SetValue(newItem.Value);
                                    }
                                }
                            }
                            break;

                        case NotifyCollectionChangedAction.Reset:
                            this.LoadCollection();
                            break;
                    }
                }
            }
        }

        // should be locked
        private void LoadCollection()
        {
            Debug.Assert(this.metadata != null);

            this.Clear();

            if (this.metadata != null)
            {
                foreach (KeyValuePair<string, object> kv in this.metadata)
                {
                    this.Add(new MetadataKeyValuePair(this, kv.Key, kv.Value));
                }
            }
        }

        private readonly KStudioWritableEventFile file;
        private readonly KStudioEventStream stream;
        private readonly KStudioMetadata metadata;
    }
}
