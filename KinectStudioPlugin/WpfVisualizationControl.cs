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
    using KinectStudioUtility;

    [TemplatePart(Name = "PART_Panel", Type = typeof(Panel))]
    public class WpfVisualizationControl : VisualizationControl
    {
        public WpfVisualizationControl(IServiceProvider serviceProvider, VisualizationViewSettings viewSettings, EventType eventType, IAvailableStreams availableStreamsGetter)
            : base(serviceProvider, viewSettings, (p) => p is IWpfVisualPlugin, eventType, availableStreamsGetter)
        {
            DebugHelper.AssertUIThread();
        }

        public override void OnApplyTemplate()
        {
            DebugHelper.AssertUIThread();

            base.OnApplyTemplate();

            this.panel = GetTemplateChild("PART_Panel") as Panel;
            if (this.panel == null)
            {
                throw new InvalidOperationException("missing template part");
            }
        }

        protected override string SettingsTitle
        {
            get
            {
                return Strings.PluginViewSettings_WpfTitle;
            }
        }

        protected override void OnLoaded()
        {
            DebugHelper.AssertUIThread();

            base.OnLoaded();

            this.ReloadControls();

            this.ContextMenuOpening += (source, e) =>
            {
                this.ContextMenu.DataContext = this.DataContext;
                this.ContextMenu.CommandBindings.Clear();

                OnBindCommands(this.ContextMenu.CommandBindings);
            };
        }

        protected override void OnSettingsChanged()
        {
            DebugHelper.AssertUIThread();

            base.OnSettingsChanged();

            ReloadControls();
        }

        protected override IPluginViewSettings AddView(IPlugin plugin, out Panel hostControl)
        {
            DebugHelper.AssertUIThread();

            hostControl = null;

            IPluginEditableViewSettings value = null;

            IWpfVisualPlugin visualPlugin = plugin as IWpfVisualPlugin;

            if (visualPlugin != null)
            {
                hostControl = new StackPanel();

                value = visualPlugin.AddWpfView(hostControl);
            }

            return value;
        }

        private void ReloadControls()
        {
            DebugHelper.AssertUIThread();

            if (this.panel != null)
            {
                panel.Children.Clear();

                foreach (PluginViewState viewState in this.PluginViewStates)
                {
                    if (viewState.IsEnabled && (viewState.HostControl != null))
                    {
                        this.panel.Children.Add(viewState.HostControl);
                    }
                }
            }
        }

        private Panel panel = null;
    }
}
