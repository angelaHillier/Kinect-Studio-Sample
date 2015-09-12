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
using System.Xml.Linq;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ViewManager : INotifyPropertyChanged
    {
        ObservableCollection<View> viewList;
        View activeView;

        public ViewManager()
        {
            this.viewList = new ObservableCollection<View>();
        }

        public IEnumerable<View> Views { get { return this.viewList; } }

        public View ActiveView
        {
            get
            {
                return this.activeView;
            }

            set
            {
                if (this.activeView != value)
                {
                    this.activeView = value;
                    Notify("ActiveView");
                }
            }
        }

        public void OnViewCreated(View view)
        {
            this.viewList.Add(view);
            view.Closed += OnViewClosed;
            view.Activated += OnViewActivated;
        }

        void OnViewClosed(object sender, EventArgs e)
        {
            var view = sender as View;

            if (view != null)
            {
                this.viewList.Remove(view);
                if (view == this.ActiveView)
                {
                    this.ActiveView = this.viewList.FirstOrDefault();
                }

                NotifyViewsOrderChanged();
            }
        }

        void OnViewActivated(object sender, EventArgs e)
        {
            var view = sender as View;

            if (view == this.ActiveView || view == null)
                return;

            this.viewList.Remove(view);
            this.viewList.Insert(0, view);
            this.ActiveView = view;

            NotifyViewsOrderChanged();
        }


        void Notify(string property)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        void NotifyViewsOrderChanged()
        {
            var handler = this.ViewsOrderChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler ViewsOrderChanged;
    }
}
