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

namespace GoogleCloudExtension.Utils.Wpf
{
    /// <summary>
    /// Interaction logic for AsyncPropertyContent.xaml
    /// </summary>
    public class AsyncPropertyContent : ContentControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
            nameof(Target),
            typeof(IAsyncProperty<Task>),
            typeof(AsyncPropertyContent),
            new PropertyMetadata(new AsyncProperty<object>(Task.FromResult<object>(null)), OnTargetChanged));

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

        protected static readonly DependencyProperty CurrentContentProperty =
            DependencyProperty.Register(
                nameof(CurrentContent),
                typeof(DependencyProperty),
                typeof(AsyncPropertyContent),
                new PropertyMetadata(OnCurrentContentChanged));

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

        private DependencyProperty CurrentContent
        {
            set => SetValue(CurrentContentProperty, value);
        }

        static AsyncPropertyContent()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AsyncPropertyContent),
                new FrameworkPropertyMetadata(typeof(AsyncPropertyContent)));
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
                }

                contentControl.CurrentContent = contentControl.GetExpectedCurrentContent();
            }
        }

        private void OnTargetPropertyChanged(object sender, PropertyChangedEventArgs e) =>
            CurrentContent = GetExpectedCurrentContent();

        private static void OnContentChanged(DependencyObject self, DependencyPropertyChangedEventArgs args)
        {
            if (self.GetValue(CurrentContentProperty) is DependencyProperty currentContent &&
                args.Property == currentContent)
            {
                self.SetValue(ContentProperty, self.GetValue(currentContent));
            }
        }

        private static void OnCurrentContentChanged(DependencyObject self, DependencyPropertyChangedEventArgs args)
        {
            if (args.Property == CurrentContentProperty && args.NewValue is DependencyProperty currentContent)
            {
                self.SetValue(ContentProperty, self.GetValue(currentContent));
            }
        }

        private DependencyProperty GetExpectedCurrentContent()
        {
            if (Target == null)
            {
                return null;
            }
            else if (Target.IsPending)
            {
                return PendingContentProperty;
            }
            else if (Target.IsSuccess)
            {
                return SuccessContentProperty;
            }
            else if (Target.IsCanceled)
            {
                return CanceledContentProperty;
            }
            else if (Target.IsError)
            {
                return ErrorContentProperty;
            }
            else
            {
                return null;
            }
        }
    }
}
