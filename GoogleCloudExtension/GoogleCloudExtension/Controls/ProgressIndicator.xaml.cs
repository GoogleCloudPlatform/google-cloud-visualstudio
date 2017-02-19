// Copyright 2016 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// Interaction logic for ProgressIndicator.xaml
    /// </summary>
    public partial class ProgressIndicator : UserControl, ISupportInitialize
    {
        /// <summary>
        /// The definition of the animation for progress.
        /// </summary>
        private readonly Duration s_animationDuration;

        // The frames for the light, blue and dark themes.
        private static readonly Lazy<IList<ImageSource>> s_lightFrames = new Lazy<IList<ImageSource>>(LoadLightFrames);
        private static readonly Lazy<IList<ImageSource>> s_darkFrames = new Lazy<IList<ImageSource>>(LoadDarkFrames);

        private Storyboard _storyboard;
        private VsTheme _forceTheme;
        private readonly bool _initializing;

        public static readonly DependencyProperty FullDurationProperty = DependencyProperty.Register(
            nameof(FullDuration), typeof(int), typeof(ProgressIndicator), new PropertyMetadata(500));

        /// <summary>
        /// Define the FullDuration custom dependency property. 
        /// This controls the animation moving speed, 
        /// the smaller this value is, the faster the progress indicator moves.
        /// </summary>
        public int FullDuration
        {
            get { return (int)GetValue(FullDurationProperty); }
            set { SetValue(FullDurationProperty, value); }
        }

        /// <summary>
        /// Sets the theme to use instead of trying to detect the theme. The default value is <see cref="VsTheme.Unknown"/>
        /// which means detect the theme.
        /// </summary>
        public VsTheme ForceTheme
        {
            get { return _forceTheme; }
            set { _forceTheme = value; }
        }

        public ProgressIndicator()
        {
            try
            {
                _initializing = true;
                InitializeComponent();
            }
            finally
            {
                _initializing = false;
            }
        }

        private ObjectAnimationUsingKeyFrames CreateAnimation(IList<ImageSource> imageFrames)
        {
            var animationDuration = new Duration(new TimeSpan(0, 0, 0, 0, FullDuration));

            // Initialize the animation for this object.
            var result = new ObjectAnimationUsingKeyFrames
            {
                Duration = animationDuration,
            };

            // Creates the frames for the animation.
            var frameDuration = FullDuration / s_lightFrames.Value.Count;
            int framePoint = 0;
            var keyFrames = new ObjectKeyFrameCollection();
            foreach (var frame in imageFrames)
            {
                var keyFrame = new DiscreteObjectKeyFrame
                {
                    KeyTime = new TimeSpan(0, 0, 0, 0, framePoint),
                    Value = frame,
                };
                keyFrames.Add(keyFrame);

                framePoint += frameDuration;
            }
            result.KeyFrames = keyFrames;

            return result;
        }

        private ObjectAnimationUsingKeyFrames CreateAnimationForTheme()
        {
            // var theme = ForceTheme != VsTheme.Unknown ? ForceTheme : ThemeManager.GetCurrentTheme();
            switch (ForceTheme)
            {
                case VsTheme.Dark:
                    return CreateAnimation(s_darkFrames.Value);
                default:
                    return CreateAnimation(s_lightFrames.Value);
            }
        }

        #region ISupportInitialize

        public override void EndInit()
        {
            base.EndInit();

            // Avoid doing any initializaiton until the constructor is finished.
            if (_initializing)
            {
                return;
            }

            StartAnimation();
        }

        #endregion

        private void StartAnimation()
        {
            if (_storyboard == null)
            {
                var animation = CreateAnimationForTheme();

                _storyboard = new Storyboard();
                _storyboard.Children.Add(animation);

                Storyboard.SetTargetName(animation, "_image");
                Storyboard.SetTargetProperty(animation, new PropertyPath("Source"));

                _storyboard.Completed += (s, e) => StartAnimation();
            }
            _storyboard.Begin(_root);
        }

        private static List<ImageSource> LoadLightFrames()
        {
            return Enumerable.Range(1, 12)
                .Select(x => ResourceUtils.LoadImage($"Controls/Resources/step_{x}.png"))
                .ToList();
        }

        private static IList<ImageSource> LoadDarkFrames()
        {
            return Enumerable.Range(1, 12)
               .Select(x => ResourceUtils.LoadImage($"Controls/Resources/step_{x}_dark.png"))
               .ToList();
        }
    }
}
