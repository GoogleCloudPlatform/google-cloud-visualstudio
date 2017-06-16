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

namespace GoogleCloudExtension.NamePrompt
{
    /// <summary>
    /// This class represents the dialog used to prompt for a name.
    /// </summary>
    public class NamePromptWindow : CommonDialogWindowBase
    {
        /// <summary>
        /// Options for the dialog.
        /// </summary>
        public class Options
        {
            /// <summary>
            /// The initial name to use in the dialog.
            /// </summary>
            public string InitialName { get; set; }

            /// <summary>
            /// The title to use for the dialog.
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            /// The message to show in the dialog.
            /// </summary>
            public string Message { get; set; }
        }

        private NamePromptViewModel ViewModel { get; }

        private NamePromptWindow(Options options) : base(options?.Title ?? GoogleCloudExtension.Resources.NamePromptCaption)
        {
            ViewModel = new NamePromptViewModel(this, options);
            Content = new NamePromptContent()
            {
                DataContext = ViewModel
            };
        }

        /// <summary>
        /// Prompts the user for a name.
        /// </summary>
        /// <returns>The name chosen by the user.</returns>
        public static string PromptUser(Options options = null)
        {
            var dialog = new NamePromptWindow(options);
            dialog.ShowModal();
            return dialog.ViewModel.Name;
        }
    }
}
