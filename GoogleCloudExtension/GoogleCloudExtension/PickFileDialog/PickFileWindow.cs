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

using GoogleCloudExtension.Theming;
using System.Collections.Generic;

namespace GoogleCloudExtension.PickFileDialog
{
    /// <summary>
    /// Dialog to choose a file.
    /// </summary>
    public class PickFileWindow : CommonDialogWindowBase
    {
        private  PickFileWindowViewModel ViewModel { get; }

        private PickFileWindow(IEnumerable<string> fileList)
            : base(GoogleCloudExtension.Resources.SourceVersionPickFileDialogCaption)
        {
            ViewModel = new PickFileWindowViewModel(this, fileList);
            Content = new PickFileWindowContent { DataContext = ViewModel };
        }

        /// <summary>
        /// Prompt user to choose a file from a list.
        /// </summary>
        /// <param name="fileList">A list of files to be picked.</param>
        /// <returns>
        /// The picked file index.
        /// Or -1 if Cancel button is clicked.
        /// </returns>
        public static int PromptUser(IEnumerable<string> fileList)
        {
            var dialog = new PickFileWindow(fileList);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
