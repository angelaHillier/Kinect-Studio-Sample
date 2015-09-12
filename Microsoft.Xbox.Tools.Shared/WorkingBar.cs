//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Microsoft.Xbox.Tools.Shared
{
    public class WorkingBar : Control
    {
        const double InitialDelayMilliseconds = 500;
        const double BetweenDotsMilliseconds = 100;

        public static readonly DependencyProperty Pos1Property = DependencyProperty.Register("Pos1", typeof(double), typeof(WorkingBar));
        public static readonly DependencyProperty Pos2Property = DependencyProperty.Register("Pos2", typeof(double), typeof(WorkingBar));
        public static readonly DependencyProperty Pos3Property = DependencyProperty.Register("Pos3", typeof(double), typeof(WorkingBar));
        public static readonly DependencyProperty Pos4Property = DependencyProperty.Register("Pos4", typeof(double), typeof(WorkingBar));
        public static readonly DependencyProperty Pos5Property = DependencyProperty.Register("Pos5", typeof(double), typeof(WorkingBar));

        public double Pos1 { get { return (double)GetValue(Pos1Property); } set { SetValue(Pos1Property, value); } }
        public double Pos2 { get { return (double)GetValue(Pos2Property); } set { SetValue(Pos2Property, value); } }
        public double Pos3 { get { return (double)GetValue(Pos3Property); } set { SetValue(Pos3Property, value); } }
        public double Pos4 { get { return (double)GetValue(Pos4Property); } set { SetValue(Pos4Property, value); } }
        public double Pos5 { get { return (double)GetValue(Pos5Property); } set { SetValue(Pos5Property, value); } }

        AnimationObserver observedAnimation;

        public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
            "IsRunning", typeof(bool), typeof(WorkingBar), new FrameworkPropertyMetadata(OnIsRunningChanged));

        public bool IsRunning
        {
            get { return (bool)GetValue(IsRunningProperty); }
            set { SetValue(IsRunningProperty, value); }
        }

        void StartAnimation(DependencyProperty property, TimeSpan beginTime, bool isLast = false)
        {
            DoubleAnimationUsingKeyFrames d = new DoubleAnimationUsingKeyFrames();

            d.KeyFrames.Add(new LinearDoubleKeyFrame(.25d, KeyTime.FromPercent(.10d)));
            d.KeyFrames.Add(new LinearDoubleKeyFrame(.3d, KeyTime.FromPercent(.15d)));
            d.KeyFrames.Add(new LinearDoubleKeyFrame(.7d, KeyTime.FromPercent(.85d)));
            d.KeyFrames.Add(new LinearDoubleKeyFrame(.75d, KeyTime.FromPercent(.90d)));
            d.KeyFrames.Add(new LinearDoubleKeyFrame(1d, KeyTime.FromPercent(1d)));

            d.Duration = TimeSpan.FromSeconds(3);
            d.BeginTime = beginTime;
            d.FillBehavior = FillBehavior.Stop;

            if (isLast)
            {
                if (this.observedAnimation != null)
                {
                    this.observedAnimation.StopObserving();
                }

                this.observedAnimation = new AnimationObserver(this, d);
            }

            this.BeginAnimation(property, null);
            this.BeginAnimation(property, d);
        }

        void OnLastAnimationCompleted(AnimationObserver observer)
        {
            if (observer == this.observedAnimation)
            {
                this.observedAnimation = null;
                StartOrStopAnimations();
            }
        }

        void StartOrStopAnimations()
        {
            if (this.IsRunning)
            {
                StartAnimation(Pos1Property, TimeSpan.FromMilliseconds(InitialDelayMilliseconds));
                StartAnimation(Pos2Property, TimeSpan.FromMilliseconds(InitialDelayMilliseconds + BetweenDotsMilliseconds));
                StartAnimation(Pos3Property, TimeSpan.FromMilliseconds(InitialDelayMilliseconds + BetweenDotsMilliseconds * 2));
                StartAnimation(Pos4Property, TimeSpan.FromMilliseconds(InitialDelayMilliseconds + BetweenDotsMilliseconds * 3));
                StartAnimation(Pos5Property, TimeSpan.FromMilliseconds(InitialDelayMilliseconds + BetweenDotsMilliseconds * 4), isLast: true);
            }
            else
            {
                this.BeginAnimation(Pos1Property, null);
                this.BeginAnimation(Pos2Property, null);
                this.BeginAnimation(Pos3Property, null);
                this.BeginAnimation(Pos4Property, null);
                this.BeginAnimation(Pos5Property, null);
            }
        }

        static void OnIsRunningChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            WorkingBar bar = obj as WorkingBar;

            if (bar != null)
            {
                bar.StartOrStopAnimations();
            }
        }

        class AnimationObserver
        {
            WorkingBar bar;
            DoubleAnimationBase animation;

            public AnimationObserver(WorkingBar bar, DoubleAnimationBase animation)
            {
                this.bar = bar;
                this.animation = animation;
                this.animation.Completed += OnAnimationCompleted;
            }

            void OnAnimationCompleted(object sender, EventArgs e)
            {
                StopObserving();
                this.bar.OnLastAnimationCompleted(this);
            }

            public void StopObserving()
            {
                this.animation.Completed -= OnAnimationCompleted;
            }
        }
    }
}
