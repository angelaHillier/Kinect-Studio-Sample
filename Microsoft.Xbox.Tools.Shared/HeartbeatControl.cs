using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Xbox.Tools.Shared
{
    public class HeartbeatControl : Control
    {
        public static readonly DependencyProperty IsAliveProperty = DependencyProperty.Register(
            "IsAlive", typeof(bool), typeof(HeartbeatControl));

        static readonly DependencyPropertyKey isRunningPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsRunning", typeof(bool), typeof(HeartbeatControl), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsRunningProperty = isRunningPropertyKey.DependencyProperty;

        static bool areAnimationsEnabled = true;

        public static bool AreAnimationsEnabled
        {
            get { return areAnimationsEnabled; }
            set
            {
                if (areAnimationsEnabled != value)
                {
                    areAnimationsEnabled = value;

                    var handler = AreAnimationsEnabledChanged;
                    if (handler != null)
                    {
                        handler(null, EventArgs.Empty);
                    }
                }
            }
        }

        public HeartbeatControl()
        {
            this.IsEnabledChanged += OnIsEnabledOrIsVisibleChanged;
            this.IsVisibleChanged += OnIsEnabledOrIsVisibleChanged;
            AreAnimationsEnabledChanged += OnAreAnimationsEnabledChanged;
        }

        public static event EventHandler AreAnimationsEnabledChanged;

        public bool IsRunning
        {
            get { return (bool)GetValue(IsRunningProperty); }
            private set { SetValue(isRunningPropertyKey, value); }
        }

        public bool IsAlive
        {
            get { return (bool)GetValue(IsAliveProperty); }
            set { SetValue(IsAliveProperty, value); }
        }

        void UpdateIsRunning()
        {
            // All this because you can't do EnterAction/ExitAction with a MultiTrigger...
            this.IsRunning = this.IsVisible && this.IsEnabled && AreAnimationsEnabled;
        }

        void OnAreAnimationsEnabledChanged(object sender, EventArgs e)
        {
            UpdateIsRunning();
        }

        void OnIsEnabledOrIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateIsRunning();
        }
    }
}
