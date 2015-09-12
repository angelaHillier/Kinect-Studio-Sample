//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using Microsoft.Xbox.Tools.Shared;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using KinectStudioUtility;

    public class MostRecentlyUsedState
    {
        public MostRecentlyUsedState()
        {
        }

        public string LastBrowsePath { get; set; }

        public string LastBrowseSpec { get; set; }

        // this is a little strange because of the way the session state serialization works

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        
        // this has to be a List because that's what the Microsoft.Xbox.Tools.Shared Session State persistence expects
        public List<string> SaveItems
        {
            get
            {
                DebugHelper.AssertUIThread();

                return new List<string>(this.items);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.items.Clear();

                if (value != null)
                {
                    foreach (string item in value)
                    {
                        if (!String.IsNullOrWhiteSpace(item))
                        {
                            this.items.Add(item);
                        }

                        if (this.items.Count == maxItems)
                        {
                            break;
                        }
                    }
                }
            }
        }

        [IgnoreSessionStateField]
        public IEnumerable<string> Items
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.items;
            }
        }

        public void Add(string item)
        {
            if (item != null)
            {
                item = item.Trim();

                if (!String.IsNullOrWhiteSpace(item))
                {
                    string upperItem = item.ToUpperInvariant();

                    int i = this.items.Count - 1;
                    while (i >= 0)
                    {
                        if (this.items[i].ToUpperInvariant() == upperItem)
                        {
                            this.items.RemoveAt(i);
                        }

                        --i;
                    }

                    this.items.Insert(0, item);

                    while (this.items.Count > maxItems)
                    {
                        this.items.RemoveAt(this.items.Count - 1);
                    }
                }
            }
        }

        private readonly ObservableCollection<string> items = new ObservableCollection<string>();
        private const int maxItems = 10;
    };
}
