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

namespace GoogleCloudExtension.TeamExplorerExtension
{
    /// <summary>
    /// Define interface as Team Explorer connect panel section content.
    /// The real content will be WPF XAML user control.
    /// </summary>
    public interface ISectionView
    {
        /// <summary>
        /// Gets the view model object that controls the view.
        /// </summary>
        ISectionViewModel ViewModel { get; }

        /// <summary>
        /// Gets the title for the section.
        /// </summary>
        string Title { get; }
    }
}
