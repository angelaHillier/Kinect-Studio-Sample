//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using nui = Microsoft.Xbox.Input.Nui;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using System.Xml.Linq;
    using KinectStudioUtility;

    public class Image2DVisualizationControl : ImageVisualizationControl
    {
        public Image2DVisualizationControl(IServiceProvider serviceProvider, EventType eventType, VisualizationViewSettings viewSettings, IAvailableStreams availableStreamsGetter)
            : base(serviceProvider, eventType, viewSettings, (p) => (p is I2DVisualPlugin), availableStreamsGetter) 
        {
            DebugHelper.AssertUIThread();
        }

        public bool IsZoomToFit
        {
            get
            {
                return (bool)GetValue(IsZoomToFitProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                this.SetValue(IsZoomToFitProperty, value);
            }
        }

        public int Zoom
        {
            get
            {
                return (int)this.GetValue(ZoomProperty);
            }
            set
            {
                DebugHelper.AssertUIThread();

                if (value < 10)
                {
                    value = 10;
                }
                else if (value > 2500)
                {
                    value = 2500;
                }

                this.ignoreZoom++;
                this.IsZoomToFit = false;
                this.ignoreZoom--;

                this.SetValue(ZoomProperty, value);
            }
        }

        public void ZoomIn()
        {
            DebugHelper.AssertUIThread(); 

            this.Zoom = (int)(this.Zoom * 1.10);
        }

        public void ZoomOut()
        {
            DebugHelper.AssertUIThread();

            this.Zoom = (int)(this.Zoom * 0.90);
        }

        public override void OnApplyTemplate()
        {
            DebugHelper.AssertUIThread();

            base.OnApplyTemplate();

            if (this.ViewSettings != null)
            {
                XElement viewSettingsElement = this.ViewSettings.ViewSettingsElement;

                this.IsZoomToFit = XmlExtensions.GetAttribute(viewSettingsElement, "zoomToFit", true);

                if (!this.IsZoomToFit)
                {
                    this.Zoom = XmlExtensions.GetAttribute(viewSettingsElement, "zoomLevel", 100);
                }
            }

            this.scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
            if (this.scrollViewer != null)
            {
                this.scrollViewer.PreviewMouseWheel += (source, e) => this.OnMouseWheel(e);
            }

            this.scaleTransform = GetTemplateChild("PART_ScaleTransform") as ScaleTransform;
        }

        public event EventHandler ZoomChanged;

        protected override string SettingsTitle
        {
            get
            {
                return Strings.PluginViewSettings_2DTitle;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void Do2DPropertyView()
        {
            var image = this.Image;

            if (image != null)
            {
                try
                {
                    Point mousePoint = Mouse.GetPosition(image);

                    if ((mousePoint.X >= 0.0) && (mousePoint.Y >= 0.0) && (mousePoint.X < image.ActualWidth) && (mousePoint.Y < image.ActualHeight))
                    {
                        double scale = (IsZoomToFit) ? CalcScaleOnZoomToFit(image.Width, image.Height) : (Zoom / 100.0);

                        Point pt = new Point(mousePoint.X / scale, mousePoint.Y / scale);

                        this.PluginService.Update2DPropertyView(this.EventType, pt.X, pt.Y, this.imageWidth, this.imageHeight);
                    }
                    else
                    {
                        this.PluginService.Clear2DPropertyView();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        protected override void OnRefreshSettings(XElement element)
        {
            base.OnRefreshSettings(element);

            if (element != null)
            {
                element.SetAttributeValue("zoomToFit", this.IsZoomToFit.ToString());
                element.SetAttributeValue("zoomLevel", this.Zoom.ToString(CultureInfo.InvariantCulture));
            }
        }

        protected override viz.Vector ClearColor
        {
            get 
            {
                return Image2DVisualizationControl.clearColor; 
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DebugHelper.AssertUIThread(); 

                if (this.overlay != null)
                {
                    this.overlay.Dispose();
                    this.overlay = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override IPluginViewSettings AddView(IPlugin plugin, out Panel hostControl)
        {
            DebugHelper.AssertUIThread(); 

            hostControl = null;
            IPluginViewSettings value = null;

            I2DVisualPlugin visualPlugin = plugin as I2DVisualPlugin;

            if (visualPlugin != null)
            {
                hostControl = new Canvas();

                value = visualPlugin.Add2DView(this.EventType, hostControl);
            }

            return value;
        }

        protected override void OnNuiVizInitialize(viz.Context context)
        {
            DebugHelper.AssertUIThread(); 

            base.OnNuiVizInitialize(context);

            if (context != null)
            {
                this.overlay = new viz.Overlay(context);
            }
        }

        protected override void OnDisplaySettingsChanging()
        {
            DebugHelper.AssertUIThread();
            
            base.OnDisplaySettingsChanging();

            if (this.overlay != null)
            {
                this.overlay.Dispose();
                this.overlay = null;
            }
        }

        protected override void OnBindCommands(CommandBindingCollection bindings)
        {
            DebugHelper.AssertUIThread(); 

            base.OnBindCommands(bindings);

            if (bindings != null)
            {
                {
                    ICommand cmd = FindResource("KinectStudioPlugin.ZoomPercentageCommand") as ICommand;
                    if (cmd != null)
                    {
                        bindings.Add(new CommandBinding(cmd,
                            (source2, e2) =>
                                {
                                    DebugHelper.AssertUIThread(); 

                                    string str = e2.Parameter as string;
                                    if (str == null)
                                    {
                                        this.IsZoomToFit = true;
                                    }
                                    else
                                    {
                                        int newZoom;
                                        if (int.TryParse(str, out newZoom))
                                        {
                                            e2.Handled = true;

                                            Zoom = newZoom;
                                        }
                                    }
                                },
                            (source2, e2) =>
                                {
                                    e2.Handled = true;
                                    e2.CanExecute = true;
                                }));
                    }
                }

                {
                    ICommand cmd = FindResource("KinectStudioPlugin.ZoomInOutCommand") as ICommand;
                    if (cmd != null)
                    {
                        bindings.Add(new CommandBinding(cmd,
                            (source2, e2) =>
                                {
                                    DebugHelper.AssertUIThread(); 

                                    string str = e2.Parameter as string;
                                    switch (str)
                                    {
                                        case "+":
                                            e2.Handled = true;
                                            this.ZoomIn();
                                            break;

                                        case "-":
                                            e2.Handled = true;
                                            this.ZoomOut();
                                            break;
                                    }
                                },
                            (source2, e2) =>
                                {
                                    string str = e2.Parameter as string;
                                    switch (str)
                                    {
                                        case "+":
                                            e2.Handled = true;
                                            e2.CanExecute = (this.Zoom <= 2273);
                                            break;

                                        case "-":
                                            e2.Handled = true;
                                            e2.CanExecute = (this.Zoom >= 11);
                                            break;
                                    }
                                }));
                    }
                }
            }
        }

        protected override void OnGetLayout(viz.Context context, viz.Texture texture, ref float layoutWidth, ref float layoutHeight)
        {
            DebugHelper.AssertUIThread(); 

            bool fixZoom = (this.imageWidth == 0) || (this.imageHeight == 0);

            if (texture == null)
            {
                this.imageWidth = Image2DVisualizationControl.defaultWidth;
                this.imageHeight = Image2DVisualizationControl.defaultHeight;
            }
            else
            {
                this.imageWidth = texture.GetWidth();
                this.imageHeight = texture.GetHeight();
            }

            double scale = 1.0;

            if (this.IsZoomToFit)
            {
                if (this.scrollViewer != null)
                {
                    this.scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    this.scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;

                    scale = CalcScaleOnZoomToFit(this.scrollViewer.ActualWidth, this.scrollViewer.ActualHeight);
                }

                int zoom = (int)(100.0 * scale);

                if (this.Zoom != zoom)
                {
                    if (this.scaleTransform != null)
                    {
                        this.scaleTransform.ScaleX = scale;
                        this.scaleTransform.ScaleY = scale;
                    }

                    fixZoom = true;

                    this.ignoreZoom++;
                    this.SetValue(Image2DVisualizationControl.ZoomProperty, zoom);
                    this.ignoreZoom--;
                }
            }
            else
            {
                scale = this.Zoom / 100.0;

                if (this.scrollViewer != null)
                {
                    this.scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    this.scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                }
            }

            if (fixZoom)
            {
                this.OnZoomChanged();
            }

            layoutWidth = (float)(this.imageWidth * scale);
            layoutHeight = (float)(this.imageHeight * scale);
        }

        protected override void OnEndRender(viz.Context context, viz.Texture texture, float width, float height)
        {
            DebugHelper.AssertUIThread();

            base.OnEndRender(context, texture, width, height);

            if ((this.overlay != null) && (texture != null))
            {
                overlay.DrawTexture(texture, 0, 0, (int)width, height, 1.0f);
            }

            this.Do2DPropertyView();
        }

        protected override void OnLoaded()
        {
            DebugHelper.AssertUIThread();

            base.OnLoaded();

            Window w = Window.GetWindow(this);
            if (w != null)
            {
                w.KeyDown += Visualization2DControl_KeyChange;
                w.KeyUp += Visualization2DControl_KeyChange;
            }

            this.OnZoomChanged();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            DebugHelper.AssertUIThread();

            base.OnMouseLeave(e);

            this.PluginService.Clear2DPropertyView();
        }

        protected override void OnFixLayout()
        {
            DebugHelper.AssertUIThread(); 

            base.OnFixLayout();

            viz.Context context = null;
            
            if (this.PluginService != null)
            {
                context = this.PluginService.GetContext(this.EventType);
            }

            FrameworkElement imageHost = this.ImageHost;

            if ((context != null) && (imageHost != null) && (this.scrollViewer != null))
            {
                double scale = this.Zoom / 100.0;

                double newWidth = this.imageWidth * scale;
                double newHeight = this.imageHeight * scale;

                if (imageHost.Width != newWidth)
                {
                    imageHost.Width = newWidth;
                }

                if (imageHost.Height != newHeight)
                {
                    imageHost.Height = this.imageHeight * scale;
                }
            }
        }

        private void Visualization2DControl_KeyChange(object sender, KeyEventArgs e)
        {
            DebugHelper.AssertUIThread(); 

            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            switch (e.Key)
            {
                case Key.OemPlus:
                case Key.Add:
                case Key.OemMinus:
                case Key.Subtract:
                    if (!e.IsDown && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        UIElement element = Mouse.DirectlyOver as UIElement;
                        if ((element != null) && element.IsDescendantOf(this))
                        {
                            e.Handled = true;

                            if ((e.Key == Key.OemPlus) || (e.Key == Key.Add))
                            {
                                this.ZoomIn();
                            }
                            else
                            {
                                this.ZoomOut();
                            }
                        }
                    }
                    break;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            DebugHelper.AssertUIThread(); 

            if (e != null)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    if (e.Delta > 0)
                    {
                        e.Handled = true;

                        this.ZoomIn();
                    }
                    else if (e.Delta < 0)
                    {
                        e.Handled = true;

                        this.ZoomOut();
                    }
                }
                else if (this.scrollViewer != null)
                {
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        if (this.scrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                        {
                            if (e.Delta > 0)
                            {
                                e.Handled = true;
                                this.scrollViewer.LineLeft();
                            }
                            else if (e.Delta < 0)
                            {
                                e.Handled = true;
                                this.scrollViewer.LineRight();
                            }
                        }
                    }
                    else
                    {
                        if (this.scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                        {
                            if (e.Delta > 0)
                            {
                                e.Handled = true;
                                this.scrollViewer.LineUp();
                            }
                            else if (e.Delta < 0)
                            {
                                e.Handled = true;
                                this.scrollViewer.LineDown();
                            }
                        }
                    }
                }
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            DebugHelper.AssertUIThread();

            base.OnRenderSizeChanged(sizeInfo);

            if (this.IsZoomToFit)
            {
                this.imageWidth = 0;
                this.imageHeight = 0;
            }
        }

        private void OnZoomChanged()
        {
            DebugHelper.AssertUIThread();

            if (this.ignoreZoom > 0)
            {
                return;
            }

            this.FixLayout();

            if (this.scaleTransform != null)
            {
                double scale = this.Zoom / 100.0;

                this.scaleTransform.ScaleX = scale;
                this.scaleTransform.ScaleY = scale;
            }

            EventHandler handler = this.ZoomChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            Image2DVisualizationControl control = d as Image2DVisualizationControl;
            if (control != null)
            {
                control.OnZoomChanged();
            }
        }

        private double CalcScaleOnZoomToFit(double width, double height)
        {
            DebugHelper.AssertUIThread();

            double scale = 1.0;

            double aspectRatio = ((double)this.imageWidth) / this.imageHeight;

            if (width > (aspectRatio * height))
            {
                scale = aspectRatio * height / this.imageWidth;
            }
            else if (width < (aspectRatio * height))
            {
                scale = width / aspectRatio / this.imageHeight;
            }

            return scale;
        }

        private ScrollViewer scrollViewer = null;
        private ScaleTransform scaleTransform = null;
        private viz.Overlay overlay = null;
        private uint imageWidth = 0;
        private uint imageHeight = 0;
        private uint ignoreZoom = 0;

        public static readonly DependencyProperty IsZoomToFitProperty = DependencyProperty.Register("IsZoomToFit", typeof(bool), typeof(Image2DVisualizationControl), new PropertyMetadata(true, OnZoomChanged));
        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", typeof(int), typeof(Image2DVisualizationControl), new PropertyMetadata(100, OnZoomChanged));

        private const uint defaultWidth = nui.Constants.STREAM_IR_WIDTH;
        private const uint defaultHeight = nui.Constants.STREAM_IR_HEIGHT;
        private static readonly viz.Vector clearColor = new viz.Vector(0.3f, 0.3f, 0.3f, 1.0f);
    }
}

