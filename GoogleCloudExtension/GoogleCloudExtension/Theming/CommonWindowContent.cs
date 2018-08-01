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

using System.Windows.Controls;

namespace GoogleCloudExtension.Theming
{
    public abstract class CommonWindowContent<T> : UserControl, ICommonWindowContent<T>
    {
        /// <summary>
        /// The title of the window.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The view model backing the content.
        /// </summary>
        public T ViewModel { get; }

        /// <summary>
        /// Creates a new common window content.
        /// </summary>
        /// <param name="viewModel">The <see cref="ViewModel"/>.</param>
        /// <param name="title">The title of the parent window.</param>
        /// <remarks>
        /// This constructor sets the <see cref="System.Windows.FrameworkElement.DataContext"/> to
        /// <paramref name="viewModel"/>.
        /// </remarks>
        protected CommonWindowContent(T viewModel, string title)
        {
            ViewModel = viewModel;
            Title = title;
            DataContext = ViewModel;
        }
    }
}