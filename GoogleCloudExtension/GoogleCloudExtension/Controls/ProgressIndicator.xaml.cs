// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

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
                .Select(x => ResourceUtils.LoadResource($"Controls/Resources/step_{x}.png"))
                .ToList();
        }
    }
}
