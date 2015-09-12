//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Xbox.Tools.Shared
{
    public class ViewSource : DependencyObject
    {
        public static readonly DependencyProperty ShortcutKeyProperty = DependencyProperty.Register(
            "ShortcutKey", typeof(string), typeof(ViewSource));

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title", typeof(string), typeof(ViewSource), new FrameworkPropertyMetadata(OnTitleChanged));

        public string ShortcutKey
        {
            get { return (string)GetValue(ShortcutKeyProperty); }
            set { SetValue(ShortcutKeyProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public string SlotName { get; private set; }
        public int Id { get; private set; }
        public LayoutDefinition Parent { get; private set; }
        public IViewCreationCommand ViewCreator { get; private set; }

        public ViewSource(LayoutDefinition parent, int id, string slotName, IViewCreationCommand creator)
        {
            this.Parent = parent;
            this.Id = id;
            this.SlotName = slotName;
            this.ViewCreator = creator;
            this.Title = this.ViewCreator.DisplayName;
        }

        static void OnTitleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ViewSource viewSource = obj as ViewSource;

            if (viewSource != null && string.IsNullOrWhiteSpace(viewSource.Title))
            {
                viewSource.Title = viewSource.ViewCreator.DisplayName;
            }
        }
    }
}
