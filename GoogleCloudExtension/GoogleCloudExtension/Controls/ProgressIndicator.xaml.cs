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
        /// Duration of the animation in milliseconds.
        /// </summary>
        private const int FullDuration = 500;

        /// <summary>
        /// The definition of the animation for progress.
        /// </summary>
        private static readonly Duration s_animationDuration = new Duration(new TimeSpan(0, 0, 0, 0, FullDuration));
        private static readonly Lazy<ObjectAnimationUsingKeyFrames> s_animationSource = new Lazy<ObjectAnimationUsingKeyFrames>(CreateAnimation);
        private static readonly Lazy<IList<ImageSource>> s_frames = new Lazy<IList<ImageSource>>(LoadAnimationFrames);

        private Storyboard _storyboard;

        public ProgressIndicator()
        {
            InitializeComponent();
        }

        private static ObjectAnimationUsingKeyFrames CreateAnimation()
        {
            // Initialize the animation for this object.
            var result = new ObjectAnimationUsingKeyFrames
            {
                Duration = s_animationDuration,
            };

            // Creates the frames for the animation.
            var frameDuration = FullDuration / s_frames.Value.Count;
            int framePoint = 0;
            var keyFrames = new ObjectKeyFrameCollection();
            foreach (var frame in s_frames.Value)
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
            result.Freeze();

            return result;
        }

        #region ISupportInitialize

        public override void EndInit()
        {
            base.EndInit();

            StartAnimation();
        }

        #endregion

        private void StartAnimation()
        {
            if (_storyboard == null)
            {
                var animation = s_animationSource.Value.Clone();

                _storyboard = new Storyboard();
                _storyboard.Children.Add(animation);

                Storyboard.SetTargetName(animation, "_image");
                Storyboard.SetTargetProperty(animation, new PropertyPath("Source"));

                _storyboard.Completed += (s, e) => StartAnimation();
            }
            _storyboard.Begin(_root);
        }

        private static List<ImageSource> LoadAnimationFrames()
        {
            return Enumerable.Range(1, 12)
                .Select(x => ResourceUtils.LoadImage($"Controls/Resources/step_{x}.png"))
                .ToList();
        }
    }
}
