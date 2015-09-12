//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.Xbox.Tools.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;

    public static class WpfUtilities
    {
        static Matrix transformFromScreen = Matrix.Identity;
        static bool transformAcquired;

        public static T FindParent<T>(this DependencyObject element) where T : class
        {
            // NOTE:  This doesn't actually find the *parent* of e if e itself is a T...
            while (element != null)
            {
                T v = element as T;
                if (v != null)
                    return v;

                element = GetVisualOrLogicalParent(element);
            }

            return null;
        }

        public static T FindParentSkippingThis<T>(this DependencyObject element) where T : class
        {
            if (element == null)
                return null;

            return FindParent<T>(GetVisualOrLogicalParent(element));
        }

        static DependencyObject GetVisualOrLogicalParent(DependencyObject element)
        {
            if (element is Visual)
            {
                var pe = VisualTreeHelper.GetParent(element);
                if (pe != null)
                    return pe;
            }

            return LogicalTreeHelper.GetParent(element);
        }

        public static T FindVisualChild<T>(this DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    T grandchild = FindVisualChild<T>(child);

                    if (grandchild != null)
                    {
                        return grandchild;
                    }
                }
            }
            return null;
        }

        static void AddVisualChildren<T>(this DependencyObject obj, Func<DependencyObject, T> filter, List<T> children) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null)
                {
                    T typedChild = filter(child);

                    AddVisualChildren(child, filter, children);
                    if (typedChild != null)
                    {
                        children.Add(typedChild);
                    }
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject obj, Func<DependencyObject, T> filter = null) where T : DependencyObject
        {
            List<T> children = new List<T>();

            if (filter == null)
            {
                filter = (c) => c as T;
            }

            AddVisualChildren(obj, filter, children);
            return children;
        }

        public static bool IsParentOf(this DependencyObject parent, DependencyObject child)
        {
            if (parent == null)
                return false;

            while (child != null)
            {
                if (object.ReferenceEquals(parent, child))
                    return true;

                child = GetVisualOrLogicalParent(child);
            }

            return false;
        }

        public static Point PointToScreenIndependent(this Visual visual, Point point)
        {
            if (!transformAcquired)
            {
                // Determine the mapping matrix from the presentation source of the dragged item.
                // Only need to do this once -- the same transform will work for all visuals.
                var source = PresentationSource.FromVisual(visual);

                if (source != null)
                {
                    transformFromScreen = source.CompositionTarget.TransformFromDevice;
                    transformAcquired = true;
                }
                else
                {
                    Debug.Fail("Visual must be loaded/visible; composition target unavailable!");
                }
            }

            return (Point)transformFromScreen.Transform((Vector)visual.PointToScreen(point));
        }

        public static bool SetClipboardText(string text)
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetText(text);
                return true;
            }
            catch (Exception)
            {
                // Sometimes the clipboard throws an exception.  It is usually benign (the text actually
                // does get copied to the clipboard), but even if it doesn't, there's no real data loss
                // as the user can try again.  We indicate failure with our return value so the caller
                // can warn the user if desired -- the key point is to not crash.
                return false;
            }
        }
    }
}
