// Copyright 2018 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Utils.Async;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.MenuBarControls
{
    /// <summary>
    /// Interaction logic for AsyncPropertyContent.xaml
    /// </summary>
    public partial class AsyncPropertyContent : ContentControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
            nameof(Target),
            typeof(IAsyncProperty<Task>),
            typeof(AsyncPropertyContent),
            new PropertyMetadata(new AsyncProperty(null), OnTargetChanged));

        public static readonly DependencyProperty SuccessContentProperty = DependencyProperty.Register(
            nameof(SuccessContent),
            typeof(object),
            typeof(AsyncPropertyContent),
            new PropertyMetadata(OnContentChanged));

        public static readonly DependencyProperty PendingContentProperty = DependencyProperty.Register(
            nameof(PendingContent),
            typeof(object),
            typeof(AsyncPropertyContent),
            new PropertyMetadata(OnContentChanged));

        public static readonly DependencyProperty ErrorContentProperty = DependencyProperty.Register(
            nameof(ErrorContent),
            typeof(object),
            typeof(AsyncPropertyContent),
            new PropertyMetadata(OnContentChanged));

        public static readonly DependencyProperty CanceledContentProperty = DependencyProperty.Register(
            nameof(CanceledContent),
            typeof(object),
            typeof(AsyncPropertyContent),
            new PropertyMetadata(OnContentChanged));

        public AsyncPropertyContent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The <see cref="IAsyncProperty{T}"/> that is the target of this control.
        /// </summary>
        public IAsyncProperty<Task> Target
        {
            get => (IAsyncProperty<Task>)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// The <see cref="ContentControl.Content"/> when the target has successfully completed.
        /// </summary>
        public object SuccessContent
        {
            get => GetValue(SuccessContentProperty);
            set => SetValue(SuccessContentProperty, value);
        }

        /// <summary>
        /// The <see cref="ContentControl.Content"/> when the target is still pending.
        /// </summary>
        public object PendingContent
        {
            get => GetValue(PendingContentProperty);
            set => SetValue(PendingContentProperty, value);
        }

        /// <summary>
        /// The <see cref="ContentControl.Content"/> when the target is faulted.
        /// </summary>
        public object ErrorContent
        {
            get => GetValue(ErrorContentProperty);
            set => SetValue(ErrorContentProperty, value);
        }

        /// <summary>
        /// The <see cref="ContentControl.Content"/> when the target is canceled.
        /// </summary>
        public object CanceledContent
        {
            get => GetValue(CanceledContentProperty);
            set => SetValue(CanceledContentProperty, value);
        }

        private static void OnTargetChanged(DependencyObject self, DependencyPropertyChangedEventArgs args)
        {
            if (self is AsyncPropertyContent contentControl)
            {
                if (args.OldValue is IAsyncProperty<Task> oldAsyncProperty)
                {
                    oldAsyncProperty.PropertyChanged -= contentControl.OnTargetPropertyChanged;
                }

                if (args.NewValue is IAsyncProperty<Task> newAsyncProperty)
                {
                    newAsyncProperty.PropertyChanged += contentControl.OnTargetPropertyChanged;
                    contentControl.UpdateContentFromContext();
                }
            }
        }

        private static void OnContentChanged(DependencyObject self, DependencyPropertyChangedEventArgs args)
        {
            if (self is AsyncPropertyContent content)
            {
                content.UpdateContentFromContext();
            }
        }

        private void OnTargetPropertyChanged(object sender, PropertyChangedEventArgs e) =>
            UpdateContentFromContext();

        private void UpdateContentFromContext()
        {
            if (Target.IsSuccess)
            {
                Content = SuccessContent;
            }
            else if (Target.IsPending)
            {
                Content = PendingContent;
            }
            else if (Target.IsError)
            {
                Content = ErrorContent;
            }
            else if (Target.IsCanceled)
            {
                Content = CanceledContent;
            }
        }
    }
}
