// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Shapes;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// Define a progress bar control that displays moving dots.
    /// </summary>
    [TemplatePart(Name = "PART_StaticDotsGrid", Type = typeof(UniformGrid))]
    [TemplatePart(Name = "PART_MovingDotsGrid", Type = typeof(UniformGrid))]
    public class DotsProgressBar : Control
    {
        private const int DotsCount = 5;
        private const int Duration = 1000;     // in milliseconds
        private List<Ellipse> _dots = new List<Ellipse>();
        private UniformGrid _staticGrid;
        private UniformGrid _movingGrid;

        public static DependencyProperty StepProperty =
            DependencyProperty.Register(
                nameof(Step),
                typeof(int),
                typeof(DotsProgressBar),
                new FrameworkPropertyMetadata(-1, OnStepChanged));

        /// <summary>
        /// Gets or sets the Step property value.
        /// </summary>
        public int Step
        {
            get { return (int)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        /// <summary>
        /// Initialize the control template.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _staticGrid = Template.FindName("PART_StaticDotsGrid", this) as UniformGrid;
            _movingGrid = Template.FindName("PART_MovingDotsGrid", this) as UniformGrid;
            if (_staticGrid == null || _movingGrid == null)
            {
                return;
            }

            _staticGrid.Columns = DotsCount;
            _movingGrid.Columns = DotsCount;
            for (int i = 0; i < DotsCount; ++i)
            {
                var dot = new Ellipse { Style = (Style)TryFindResource("BlueDotStyle"), Visibility = Visibility.Hidden };
                _movingGrid.Children.Add(dot);
                _dots.Add(dot);

                var smallerDot = new Ellipse { Style = (Style)TryFindResource("SmallerDotStyle") };
                _staticGrid.Children.Add(smallerDot);
            }

            CreateAndStartAnimation();
        }

        private static void OnStepChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            // WPF framework makes the call and value type must be int. 
            // Does not need to check the type.
            int step = (int)e.NewValue;
            DotsProgressBar window = source as DotsProgressBar;
            foreach (var dot in window._dots)
            {
                dot.Visibility = Visibility.Hidden;
            }
            if (step >= 0 && step < window._dots.Count)
            {
                window._dots[step].Visibility = Visibility.Visible;
            }
        }

        private void CreateAndStartAnimation()
        {
            ObjectAnimationUsingKeyFrames animation = new ObjectAnimationUsingKeyFrames();

            animation.Duration = TimeSpan.FromMilliseconds(Duration);
            animation.RepeatBehavior = RepeatBehavior.Forever;

            var frameDuration = Duration / _dots.Count;
            int framePoint = 0;

            for (int i = 0; i < DotsCount; ++i)
            {
                DiscreteObjectKeyFrame keyFrame = new DiscreteObjectKeyFrame(
                    i,
                    new TimeSpan(0, 0, 0, 0, framePoint));
                animation.KeyFrames.Add(keyFrame);
                framePoint += frameDuration;
            }

            var _storyboard = new Storyboard();
            _storyboard.Children.Add(animation);
            this.BeginAnimation(DotsProgressBar.StepProperty, animation);
        }
    }
}
