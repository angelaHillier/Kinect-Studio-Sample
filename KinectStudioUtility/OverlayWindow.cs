//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Windows;

    public class OverlayWindow : Window
    {
        public OverlayWindow(Window owner, FrameworkElement match)
        {
            DebugHelper.AssertUIThread();

            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            this.match = match;

            this.Owner = owner;
            this.ShowInTaskbar = false;
            this.AllowsTransparency = true;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.None;
            this.Background = null;

            owner.LocationChanged += (source, e) =>
                {
                    this.FixWindow(true);
                };

            match.LayoutUpdated += (source, e) =>
                {
                    this.FixWindow(true);
                };

            match.SizeChanged += (source, e) =>
                {
                    this.FixWindow(true);
                };

            this.FixWindow(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void FixWindow(bool doDefer)
        {
            DebugHelper.AssertUIThread();

            if (doDefer)
            {
                this.defer++;
                uint deferred = this.defer;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (this.defer == deferred)
                    {
                        this.FixWindow(false);
                    }
                }));
            }
            else
            {
                if (this.Owner != null)
                {
                    try
                    {
                        Point pt = match.TransformToAncestor(this.Owner).Transform(new Point(0, 0));

                        this.Left = pt.X + this.Owner.Left;
                        this.Top = pt.Y + this.Owner.Top;
                    }
                    catch (Exception)
                    {
                    }
                }

                if ((this.match != null) && this.match.IsLoaded && (this.Owner.WindowState != System.Windows.WindowState.Minimized) && this.Owner.IsVisible)
                {
                    if (this.Width != match.ActualWidth)
                    {
                        this.Width = match.ActualWidth;
                    }

                    if (this.Height != match.ActualHeight)
                    {
                        this.Height = match.ActualHeight;
                    }

                    this.Show();
                }
                else
                {
                    this.Hide();
                }
            }
        }

        private FrameworkElement match = null;
        private uint defer = 0; 
    }
}
