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

using System.Collections.Generic;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.AddTrafficSplit
{
    /// <summary>
    /// The dialog implementation.
    /// </summary>
    public class AddTrafficSplitWindow : CommonDialogWindowBase
    {
        /// <summary>
        /// The view model for the dialog.
        /// </summary>
        private AddTrafficSplitViewModel ViewModel { get; }

        private AddTrafficSplitWindow(IEnumerable<string> versions)
            : base(GoogleCloudExtension.Resources.AddGaeTrafficSplitTitle)
        {
            ViewModel = new AddTrafficSplitViewModel(this, versions);
            Content = new AddTrafficSplitWindowContent
            {
                DataContext = ViewModel
            };
        }

        /// <summary>
        /// Prompts the user to choose from a list of versions the allocation for that version.
        /// </summary>
        /// <param name="versions">The list of version ids from which to choose.</param>
        /// <returns>The <seealso cref="AddTrafficSplitResult"/> if the user accepted the changes, null otherwise.</returns>
        public static AddTrafficSplitResult PromptUser(IEnumerable<string> versions)
        {
            var dialog = new AddTrafficSplitWindow(versions);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
