//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Xbox.Tools.Shared
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Collections;
    using System.Windows.Input;

    /// <summary>
    /// This class is internally used by TreeGridView (subclassed from ListView); 
    /// External user of TreeGridView assign a hierarchical data structure to TreeGridView.TreeItemsSource; 
    /// However the real flat list that ListView expects (assigned to ListView.ItemsSource) contains this class
    /// which wraps the user data plus a few additional visual-related properties. 
    /// 
    /// TreeGridView handles the process to transform hierachical data to flat list as items are expanded/collapsed,
    /// and though internally it's a flat list of items, each item is properly indented according to nesting level, to
    /// make a tree-like visual.
    /// 
    /// Similar properties will be also found in TreeGridViewItem; however TreeGridViewItem is a UI element,
    /// due to virtualization it is not a good place to store these properties. The major use for TreeGridViewItem to have
    /// these (dependency) properties are to use them in data-binding scenarios
    /// </summary>
    
    public class TreeGridViewItemInfo
    {
        /// <summary>
        /// Construct a TreeGridViewItemInfo from a data object
        /// </summary>
        /// <param name="data">The data object to be wrapped</param>
        /// <param name="level">The nesting level of the item</param>
        public TreeGridViewItemInfo(object data, int level)
        {
            this.Data = data;
            this.Level = level;
        }

        /// <summary>
        /// Gets the original data object from the hierarchy data structure
        /// </summary>
        public object Data
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the nesting level of this item in the tree
        /// </summary>
        public int Level
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the item is expanded or not
        /// </summary>
        public bool IsExpanded
        {
            get;
            set;
        }

    }

}