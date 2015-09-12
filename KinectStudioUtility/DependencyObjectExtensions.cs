//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public static class DependencyObjectExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static bool HasVisualParent<T>(this DependencyObject obj, DependencyObject stop) where T : Visual
        {
            bool result = false;

            while ((obj != null) && (obj != stop))
            {
                if (obj.GetType() == typeof(T))
                {
                    result = true;
                    break;
                }

                obj = VisualTreeHelper.GetParent(obj);
            }

            return result;
        }

        public static T GetVisualParent<T>(this DependencyObject obj) where T : Visual
        {
            T result = null;

            while (obj != null)
            {
                if (obj.GetType() == typeof(T))
                {
                    result = (T)obj;
                    break;
                }

                obj = VisualTreeHelper.GetParent(obj);
            }

            return result;
        }

        public static T GetVisualChild<T>(this DependencyObject obj) where T : Visual
        {
            T result = default(T);

            int count = VisualTreeHelper.GetChildrenCount(obj);

            for (int i = 0; i < count; ++i)
            {
                DependencyObject child = (Visual)VisualTreeHelper.GetChild(obj, i);

                result = child as T;

                if (result == null)
                {
                    result = child.GetVisualChild<T>();
                }

                if (result != null)
                {
                    break;
                }
            }

            return result;
        }

        public static T GetVisualChild<T>(this DependencyObject obj, Func<T, Boolean> filter) where T : Visual
        {
            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }

            T result = default(T);

            int count = VisualTreeHelper.GetChildrenCount(obj);

            for (int i = 0; i < count; ++i)
            {
                DependencyObject child = (Visual)VisualTreeHelper.GetChild(obj, i);

                T temp = child as T;

                if (temp == null)
                {
                    result = child.GetVisualChild<T>(filter);
                }
                else
                {
                    if (filter(temp))
                    {
                        result = temp;
                    }
                    else
                    {
                        result = child.GetVisualChild<T>(filter);
                    }
                }

                if (result != null)
                {
                    break;
                }
            }

            return result;
        }

        public static bool HasErrors(this DependencyObject obj)
        {
            bool result = false;
            if (obj != null)
            {
                result = Validation.GetHasError(obj);

                int count = VisualTreeHelper.GetChildrenCount(obj);

                for (int i = 0; !result && (i < count); ++i)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    result = child.HasErrors();
                }
            }

            return result;
        }
    }
}