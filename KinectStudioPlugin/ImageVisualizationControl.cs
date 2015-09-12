//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Win32;
    using nui = Microsoft.Xbox.Input.Nui;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using KinectStudioUtility;
    using System.Windows.Threading;
    using System.Text;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Viz")]
    [TemplatePart(Name = "PART_ControlsHost", Type = typeof(Panel))]
    [TemplatePart(Name = "PART_ImageHost", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_Image", Type = typeof(Image))]
    public abstract class ImageVisualizationControl : VisualizationControl
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        static ImageVisualizationControl()
        {
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KinectStudioPlugin.Cursors.RotateCursor.cur");

                if (stream != null)
                {
                    try
                    {
                        ImageVisualizationControl.RotateCursor = new Cursor(stream);
                    }
                    catch (Exception)
                    {
                        // use default
                    }

                    stream.Dispose();
                }
            }

            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KinectStudioPlugin.Cursors.PanCursor.cur");

                if (stream != null)
                {
                    try
                    {
                        ImageVisualizationControl.PanCursor = new Cursor(stream);
                    }
                    catch (Exception)
                    {
                        // use default
                    }

                    stream.Dispose();
                }
            }

            if (ImageVisualizationControl.RotateCursor == null)
            {
                ImageVisualizationControl.RotateCursor = Cursors.SizeAll;
            }

            if (ImageVisualizationControl.PanCursor == null)
            {
                ImageVisualizationControl.PanCursor = Cursors.Hand;
            }
        }

        protected ImageVisualizationControl(IServiceProvider serviceProvider, EventType eventType, VisualizationViewSettings viewSettings, Func<IPlugin, bool> filterFunc, IAvailableStreams availableStreamsGetter)
            : base(serviceProvider, viewSettings, filterFunc, eventType, availableStreamsGetter)
        {
            DebugHelper.AssertUIThread();
        }

        public override void OnApplyTemplate()
        {
            DebugHelper.AssertUIThread();

            base.OnApplyTemplate();

            this.imageHost = GetTemplateChild("PART_ImageHost") as FrameworkElement;

            if (this.imageHost is Border)
            {
                this.imageHost.Loaded += ImageHost_Loaded;
                this.imageHost.SizeChanged += ImageHost_SizeChanged;
            }

            this.image = GetTemplateChild("PART_Image") as Image;
            this.controlsPanel = GetTemplateChild("PART_ControlsHost") as Panel;
        }

        protected FrameworkElement ImageHost
        {
            get
            {
                return this.imageHost;
            }
        }

        protected Image Image
        {
            get
            {
                return this.image;
            }
        }

        protected Panel ControlsPanel
        {
            get
            {
                return this.controlsPanel;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DebugHelper.AssertUIThread();

                if (this.image != null)
                {
                    this.image.Source = null;
                }

                if (this.renderTarget != null)
                {
                    this.renderTarget.Dispose();
                    this.renderTarget = null;
                    this.renderTargetWidth = 0;
                    this.renderTargetHeight = 0;
                }
            }

            base.Dispose(disposing);
        }

        protected abstract viz.Vector ClearColor { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Viz")]        
        protected virtual void OnNuiVizInitialize(viz.Context context) { }

        protected virtual void OnDisplaySettingsChanging() { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "renderTarget")]
        protected virtual void OnRenderTargetChanged(viz.D3DImageTexture renderTarget) { }

        protected virtual void OnBeginRender(viz.Context context) { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#")]
        protected virtual void OnGetLayout(viz.Context context, viz.Texture texture, ref float layoutWidth, ref float layoutHeight) { }

        protected virtual void OnEndRender(viz.Context context, viz.Texture texture, float width, float height) { }

        protected virtual void OnFixLayout() { }

        protected override void OnLoaded()
        {
            DebugHelper.AssertUIThread();

            base.OnLoaded();

            this.ContextMenuOpening += (source, e) =>
                {
                    this.ContextMenu.DataContext = this.DataContext;
                    this.ContextMenu.CommandBindings.Clear();

                    this.OnBindCommands(this.ContextMenu.CommandBindings);
                };

            if (this.image != null)
            {
                this.InitNuiViz();

                this.FixLayout();
            }

            this.ReloadControls();

#if LOG_INFO
            this.stopwatch.Start();
#endif // LOG_INFO

            CompositionTarget.Rendering += CompositionTarget_Rendering;
            SystemEvents.DisplaySettingsChanging += SystemEvents_DisplaySettingsChanging;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        protected override void OnSettingsChanged()
        {
            DebugHelper.AssertUIThread();

            base.OnSettingsChanged();

            this.ReloadControls();
        }

        protected void FixLayout()
        {
            DebugHelper.AssertUIThread();

            this.OnFixLayout();
        }

#if LOG_INFO
        Stopwatch stopwatch = new Stopwatch(); 
#endif // LOG_INFO

        protected override void OnRender(DrawingContext drawingContext)
        {
            DebugHelper.AssertUIThread();
            
            this.needsRender = true;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (this.displaySettingsChanging)
            {
                return;
            }

#if SEPARATE_PRESENT
            if (this.needsPresent)
            {
                if (this.renderTarget == null)
                {
                    this.needsPresent = false;
                }
                else if (this.renderTarget.CheckReadyForPresent())
                {
#if LOG_INFO
                    string str = String.Format("{0:10} {1}: {2}", this.GetHashCode(), "PRESENT  ", this.stopwatch.ElapsedMilliseconds);
                    Trace.WriteLine(str);
#endif // LOG_INFO
                    this.needsPresent = false;
                    this.renderTarget.Present(this.image);
                }
#if LOG_INFO
                else
                {
                    string str = String.Format("{0:10} {1}: {2}", this.GetHashCode(), "NOT READY", this.stopwatch.ElapsedMilliseconds);
                    Trace.WriteLine(str);
                }
#endif // LOG_INFO
            } else 
#endif // SEPARATE_PRESENT
            if (this.needsRender)
            {
#if LOG_INFO
                string str = String.Format("{0:10} {1}: {2}", this.GetHashCode(), "RENDER   ", this.stopwatch.ElapsedMilliseconds);
                Trace.WriteLine(str);
#endif // LOG_INFO
                this.needsRender = false;
                this.needsPresent = this.DoRender();
#if !SEPARATE_PRESENT
                if (this.needsPresent)
                {
                    this.needsPresent = false;
                    if (this.renderTarget != null)
                    {
                        this.renderTarget.Present(this.image);
                    }
                }
#endif //!SEPARATE_PRESENT
            }
        }

        private void SystemEvents_DisplaySettingsChanging(object sender, EventArgs e)
        {
            DebugHelper.AssertUIThread();

#if LOG_INFO
            string str = String.Format("{0:10} {1}: {2}", this.GetHashCode(), "DSP-CHG  ", this.stopwatch.ElapsedMilliseconds);
            Trace.WriteLine(str);
#endif // LOG_INFO

            this.displaySettingsChanging = true;

            if (this.image != null)
            {
                this.image.Source = null;
            }

            this.OnDisplaySettingsChanging();

            if (this.renderTarget != null)
            {
                this.renderTarget.Dispose();
                this.renderTarget = null;
                this.renderTargetWidth = 0;
                this.renderTargetHeight = 0;
            }
        }

        void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            DebugHelper.AssertUIThread();

#if LOG_INFO
            string str = String.Format("{0:10} {1}: {2}", this.GetHashCode(), "DSP-CHGD ", this.stopwatch.ElapsedMilliseconds);
            Trace.WriteLine(str);
#endif // LOG_INFO

            this.displaySettingsChanging = false;
            this.FixImageSize();

            viz.Context context = null;

            if (this.PluginService != null)
            {
                context = this.PluginService.GetContext(this.EventType);
            }

            if (context != null)
            {
                this.OnNuiVizInitialize(context);
            }
        }

        private void ImageHost_Loaded(object sender, RoutedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.FixImageSize();
        }

        private void ImageHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (!this.displaySettingsChanging)
            {
                this.FixImageSize();
            }
        }

        private void FixImageSize()
        {
            DebugHelper.AssertUIThread();

            uint w = 0;
            uint h = 0;

            if (this.imageHost != null)
            {
                w = (uint)this.imageHost.ActualWidth;
                h = (uint)this.imageHost.ActualHeight;
            }

            if (this.image != null)
            {
                this.image.Width = w;
                this.image.Height = h;
            }

            this.FixRenderTargetSize(w, h);
        }

        private void FixRenderTargetSize(uint w, uint h)
        {
            DebugHelper.AssertUIThread();

            if ((this.renderTargetWidth != w) || (this.renderTargetHeight != h))
            {
                if (this.image != null)
                {
                    this.image.Source = null;
                }

                if (this.renderTarget != null)
                {
                    this.renderTarget.Dispose();
                    this.renderTarget = null;
                    this.renderTargetWidth = 0;
                    this.renderTargetHeight = 0;
                }

                if ((this.PluginService != null) && (this.imageHost != null) && (w > 0) && (h > 0))
                {
                    viz.Context context = this.PluginService.GetContext(this.EventType);
                    viz.D3DImageContext imageContext = this.PluginService.GetImageContext(this.EventType);
                    if ((context != null) && (imageContext != null))
                    {
#if LOG_INFO
                        string str = String.Format("{0:10} {1}: {2}", this.GetHashCode(), "RNDRTARG ", this.stopwatch.ElapsedMilliseconds);
                        Trace.WriteLine(str);
#endif // LOG_INFO
                        this.renderTarget = new viz.D3DImageTexture(context, imageContext, w, h);
                        this.renderTargetWidth = w;
                        this.renderTargetHeight = h;
                    }
                }

                this.OnRenderTargetChanged(this.renderTarget);
            }
        }

        private bool DoRender()
        {
            DebugHelper.AssertUIThread();

            bool result = false;

            {
                if ((this.image != null) && (this.Visibility == System.Windows.Visibility.Visible))
                {
                    result = true;

                    viz.Context context = null;

                    if (this.PluginService != null)
                    {
                        context = this.PluginService.GetContext(this.EventType);
                    }

                    if (this.image != null)
                    {
                        if (context != null)
                        {
                            if (!context.BeginRender())
                            {
                                context = null;
                            }
                        }
                    }

                    if (context != null)
                    {
                        this.OnBeginRender(context);

                        viz.Texture texture = null;

                        foreach (PluginViewState pluginViewState in this.PluginViewStates)
                        {
                            if (pluginViewState.IsEnabled)
                            {
                                IImageVisualPlugin visualPlugin = pluginViewState.Plugin as IImageVisualPlugin;
                                if (visualPlugin != null)
                                {
                                    texture = visualPlugin.GetTexture(this.EventType, pluginViewState.PluginViewSettings);

                                    if (texture != null)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        float scaledWidth = 0;
                        float scaledHeight = 0;

                        this.OnGetLayout(context, texture, ref scaledWidth, ref scaledHeight);

                        if (this.imageHost is Canvas)
                        {
                            uint w = (uint)scaledWidth;
                            uint h = (uint)scaledHeight;

                            if (this.image != null)
                            {
                                this.image.Width = w;
                                this.image.Height = h;
                            }

                            this.FixRenderTargetSize(w, h);
                        }

                        if (this.renderTarget != null)
                        {
                            if (this.renderTarget.BeginRender(this.ClearColor, 1))
                            {
                                foreach (PluginViewState pluginViewState in this.PluginViewStates)
                                {
                                    if (pluginViewState.IsEnabled)
                                    {
                                        IImageVisualPlugin visualPlugin = pluginViewState.Plugin as IImageVisualPlugin;
                                        if (visualPlugin != null)
                                        {
                                            I2DVisualPlugin visualPlugin2d = visualPlugin as I2DVisualPlugin;
                                            if (visualPlugin2d != null)
                                            {
                                                visualPlugin2d.Render2D(this.EventType, pluginViewState.PluginViewSettings, context, texture, 0, 0, scaledWidth, scaledHeight);
                                            }

                                            I3DVisualPlugin visualPlugin3d = visualPlugin as I3DVisualPlugin;
                                            if (visualPlugin3d != null)
                                            {
                                                visualPlugin3d.Render3D(this.EventType, pluginViewState.PluginViewSettings, context, texture);
                                            }
                                        }
                                    }
                                }

                                this.OnEndRender(context, texture, scaledWidth, scaledHeight);

                                this.renderTarget.EndRender();
                            }
                        }

                        context.EndRender();
                    }
                }
            }

            return result;
        }

        protected override void OnUnloaded()
        {
            DebugHelper.AssertUIThread();

            base.OnUnloaded();

            if (this.updateTimer != null)
            {
                this.updateTimer.Tick -= UpdateTimer_Tick;
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            DebugHelper.AssertUIThread();

            this.InvalidateVisual();
        }

        private void InitNuiViz()
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(this.image != null);

            viz.Context context = null;

            if (this.PluginService != null)
            {
                context = this.PluginService.GetContext(this.EventType);
            }

            if (context != null)
            {
                this.OnNuiVizInitialize(context);

                this.updateTimer = new DispatcherTimer()
                    {
                        Interval = ImageVisualizationControl.cUpdateTime,
                    };

                this.updateTimer.Tick += UpdateTimer_Tick;

                this.updateTimer.Start();
            }
        }

        private void ReloadControls()
        {
            DebugHelper.AssertUIThread();

            if (this.controlsPanel != null)
            {
                controlsPanel.Children.Clear();

                foreach (PluginViewState viewState in this.PluginViewStates)
                {
                    if (viewState.IsEnabled && (viewState.HostControl != null))
                    {
                        this.controlsPanel.Children.Add(viewState.HostControl);
                    }
                }
            }
        }

        private FrameworkElement imageHost = null;
        private Image image = null;
        private viz.D3DImageTexture renderTarget = null;
        private uint renderTargetWidth = 0;
        private uint renderTargetHeight = 0;
        private Panel controlsPanel = null;
        private DispatcherTimer updateTimer = null;
        private bool needsPresent = false;
        private bool needsRender = true;
        private bool displaySettingsChanging = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected readonly static Cursor RotateCursor = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected readonly static Cursor PanCursor = null;

        private readonly static TimeSpan cUpdateTime = TimeSpan.FromMilliseconds(30);
    }
}
