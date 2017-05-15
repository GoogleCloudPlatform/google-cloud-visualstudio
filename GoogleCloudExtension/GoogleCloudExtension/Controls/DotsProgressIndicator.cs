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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// Define a control that shows moving dots as progress indicator.
    /// </summary>
    [TemplatePart(Name = "PART_StaticDotsGrid", Type = typeof(UniformGrid))]
    [TemplatePart(Name = "PART_MovingDotsGrid", Type = typeof(UniformGrid))]
    public class DotsProgressIndicator : Control
    {
        private const int DotsCount = 5;
        private const int Duration = 800;     // in milliseconds
        private readonly List<Ellipse> _dots = new List<Ellipse>();
        private UniformGrid _staticGrid;
        private UniformGrid _movingGrid;

        public static DependencyProperty StepProperty =
            DependencyProperty.Register(
                nameof(Step),
                typeof(int),
                typeof(DotsProgressIndicator),
                new FrameworkPropertyMetadata(-1, OnStepChanged));

        /// <summary>
        /// Gets or sets the command that responds to auto reload timer event.
        /// </summary>
        public int Step
        {
            get { return (int)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        private static void OnStepChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            // WPF framework makes the call and value type must be int. 
            // Does not need to check the type.
            int step = (int)e.NewValue;
            DotsProgressIndicator window = source as DotsProgressIndicator;
            foreach (var dot in window._dots)
            {
                dot.Visibility = Visibility.Hidden;
            }
            if (step >= 0 && step < window._dots.Count)
            {
                window._dots[step].Visibility = Visibility.Visible;
            }
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
                var dot = new Ellipse
                {
                    Style = (Style)TryFindResource("BlueDotStyle"),
                    Visibility = Visibility.Hidden
                };
                _movingGrid.Children.Add(dot);
                _dots.Add(dot);

                var smallerDot = new Ellipse { Style = (Style)TryFindResource("SmallerDotStyle") };
                _staticGrid.Children.Add(smallerDot);
            }

            CreateAndStartAnimation();
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
                DiscreteObjectKeyFrame kf1 = new DiscreteObjectKeyFrame(
                    i,
                    new TimeSpan(0, 0, 0, 0, framePoint));
                animation.KeyFrames.Add(kf1);
                framePoint += frameDuration;
            }

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            this.BeginAnimation(DotsProgressIndicator.StepProperty, animation);
        }
    }
}
