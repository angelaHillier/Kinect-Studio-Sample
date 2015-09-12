//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using KinectStudioUtility;

    public class Image3DVisualizationControl : ImageVisualizationControl
    {
        public Image3DVisualizationControl(IServiceProvider serviceProvider, EventType eventType, VisualizationViewSettings viewSettings, IAvailableStreams availableStreamsGetter)
            : base(serviceProvider, eventType, viewSettings, (p) => p is I3DVisualPlugin, availableStreamsGetter)
        {
            DebugHelper.AssertUIThread();
        }

        public void ZoomIn()
        {
            DebugHelper.AssertUIThread();

            if (this.mouseNavigator != null)
            {
                this.mouseNavigator.OnMouseZoom(20);
            }
        }

        public void ZoomOut()
        {
            DebugHelper.AssertUIThread();

            if (this.mouseNavigator != null)
            {
                this.mouseNavigator.OnMouseZoom(-20);
            }
        }

        public void ViewDefault()
        {
            DebugHelper.AssertUIThread();

            if (this.arcBallCamera != null)
            {
                this.arcBallCamera.SetFrontView();
                this.arcBallCamera.Rotate(0.3f, -0.2f);
                this.arcBallCamera.Zoom(0.5f);
            }
        }

        public void ViewFront()
        {
            DebugHelper.AssertUIThread();

            if (this.arcBallCamera != null)
            {
                this.arcBallCamera.SetFrontView();
            }
        }

        public void ViewLeft()
        {
            DebugHelper.AssertUIThread();

            if (this.arcBallCamera != null)
            {
                this.arcBallCamera.SetLeftView();
            }
        }

        public void ViewTop()
        {
            DebugHelper.AssertUIThread();

            if (this.arcBallCamera != null)
            {
                this.arcBallCamera.SetTopView();
            }
        }

        protected override string SettingsTitle
        {
            get
            {
                return Strings.PluginViewSettings_3DTitle;
            }
        }

        protected override viz.Vector ClearColor
        {
            get 
            {
                return Image3DVisualizationControl.clearColor; 
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DebugHelper.AssertUIThread();

                if (this.arcBallCamera != null)
                {
                    this.arcBallCamera.Dispose();
                    this.arcBallCamera = null;
                }

                if (this.mouseNavigator != null)
                {
                    this.mouseNavigator.Dispose();
                    this.mouseNavigator = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override IPluginViewSettings AddView(IPlugin plugin, out Panel hostControl)
        {
            DebugHelper.AssertUIThread();

            hostControl = null;

            IPluginViewSettings value = null;

            I3DVisualPlugin visualPlugin = plugin as I3DVisualPlugin;

            if (visualPlugin != null)
            {
                hostControl = new Grid();

                value = visualPlugin.Add3DView(this.EventType, hostControl);
            }

            return value;
        }

        protected override void OnRenderTargetChanged(viz.D3DImageTexture renderTarget)
        {
            DebugHelper.AssertUIThread();

            if (renderTarget != null)
            {
                viz.MouseNavigator oldMouseNavigator = this.mouseNavigator;

                mouseNavigator = new viz.MouseNavigator(renderTarget); // must get something to setup view matrix

                if (oldMouseNavigator != null)
                {
                    mouseNavigator.Set(oldMouseNavigator);
                }

                viz.ArcBallCamera oldArcBallCamera = this.arcBallCamera;

                this.arcBallCamera = new viz.ArcBallCamera(renderTarget);

                if (oldArcBallCamera == null)
                {
                    this.arcBallCamera.SetFrontView();
                    this.arcBallCamera.Rotate(0.3f, -0.2f);
                    this.arcBallCamera.Zoom(0.5f);
                }
                else
                {
                    this.arcBallCamera.Set(oldArcBallCamera);
                }
            }
        }

        protected override void OnBindCommands(CommandBindingCollection bindings)
        {
            DebugHelper.AssertUIThread();

            base.OnBindCommands(bindings);

            if (bindings != null)
            {
                {
                    ICommand cmd = FindResource("KinectStudioPlugin.CameraViewCommand") as ICommand;
                    if (cmd != null)
                    {
                        bindings.Add(new CommandBinding(cmd,
                            (source2, e2) =>
                            {
                                DebugHelper.AssertUIThread();

                                switch (e2.Parameter.ToString())
                                {
                                    case "Default":
                                        e2.Handled = true;

                                        ViewDefault();
                                        break;

                                    case "Front":
                                        e2.Handled = true;

                                        ViewFront();
                                        break;

                                    case "Left":
                                        e2.Handled = true;

                                        ViewLeft();
                                        break;

                                    case "Top":
                                        e2.Handled = true;

                                        ViewTop();
                                        break;
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
                                    e2.Handled = true;
                                    e2.CanExecute = true;
                                }));
                    }
                }
            }
        }

        protected override void OnLoaded()
        {
            DebugHelper.AssertUIThread();

            base.OnLoaded();

            Window w = Window.GetWindow(this);
            if (w != null)
            {
                w.KeyDown += (source, e) => OnKeyChange(e);
                w.KeyUp += (source, e) => OnKeyChange(e);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                if ((this.mouseNavigator != null) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    e.Handled = true;

                    this.shiftKey = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

                    Point pt = e.GetPosition(this);
                    this.mouseNavigator.OnMouseBegin((int)pt.X, (int)pt.Y);

                    UpdateCursor();
                }
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                if (this.mouseNavigator != null)
                {
                    e.Handled = true;

                    this.mouseNavigator.OnMouseEnd();

                    UpdateCursor();
                }
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (this.mouseNavigator != null)
                {
                    e.Handled = true;

                    Point pt = e.GetPosition(this);

                    bool newShiftKey = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
                    if (newShiftKey != this.shiftKey)
                    {
                        this.shiftKey = newShiftKey;

                        this.mouseNavigator.OnMouseEnd();
                        this.mouseNavigator.OnMouseBegin((int)pt.X, (int)pt.Y);
                    }

                    if (this.shiftKey)
                    {
                        mouseNavigator.OnMouseTranslation((int)pt.X, (int)pt.Y);
                    }
                    else if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        mouseNavigator.OnMouseRotation((int)pt.X, (int)pt.Y);
                    }

                    UpdateCursor();
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            DebugHelper.AssertUIThread();

            if ((e != null) && (this.mouseNavigator != null) && (e.Delta != 0))
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    e.Handled = true;

                    this.mouseNavigator.OnMouseZoom(e.Delta);
                }
                else 
                {
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        this.mouseNavigator.OnMouseBegin(0, 0);
                        this.mouseNavigator.OnMouseTranslation(e.Delta, 0);
                        this.mouseNavigator.OnMouseEnd();
                    }
                    else
                    {
                        this.mouseNavigator.OnMouseBegin(0, 0);
                        this.mouseNavigator.OnMouseTranslation(0, e.Delta);
                        this.mouseNavigator.OnMouseEnd();
                    }
                }
            }

            base.OnMouseWheel(e);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            DebugHelper.AssertUIThread();

            UpdateCursor();

            base.OnMouseEnter(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            DebugHelper.AssertUIThread();

            OnKeyChange(e);

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            DebugHelper.AssertUIThread();

            OnKeyChange(e);

            base.OnKeyUp(e);
        }

        private void OnKeyChange(KeyEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            switch (e.Key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                    {
                        UIElement element = Mouse.DirectlyOver as UIElement;

                        if ((element != null) && element.IsDescendantOf(this))
                        {
                            UpdateCursor();
                        }
                    }
                    break;

                case Key.OemPlus:
                case Key.Add:
                case Key.OemMinus:
                case Key.Subtract:
                    if (!e.IsDown)
                    {
                        UIElement element = Mouse.DirectlyOver as UIElement;

                        if ((element != null) && element.IsDescendantOf(this))
                        {
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

        private void UpdateCursor()
        {
            DebugHelper.AssertUIThread();

            if (this.origCursor == null)
            {
                this.origCursor = this.Cursor;
            }

            if (Mouse.LeftButton.HasFlag(MouseButtonState.Pressed))
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    this.Cursor = ImageVisualizationControl.PanCursor;
                }
                else if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    this.Cursor = ImageVisualizationControl.RotateCursor;
                }
            }
            else
            {
                this.Cursor = this.origCursor;
            }
        }

        private viz.ArcBallCamera arcBallCamera = null;
        private viz.MouseNavigator mouseNavigator = null;
        private Cursor origCursor = null;
        private bool shiftKey = false;

        private static readonly viz.Vector clearColor = new viz.Vector(0.0f, 0.0f, 0.0f, 1.0f);
    }
}
