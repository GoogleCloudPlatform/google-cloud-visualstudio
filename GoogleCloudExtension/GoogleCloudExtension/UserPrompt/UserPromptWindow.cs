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
using System.Windows.Media;

namespace GoogleCloudExtension.UserPrompt
{
    /// <summary>
    /// This class represents a user prompt dialog, which replaces the VS built-in dialog with a 
    /// styled one.
    /// </summary>
    public class UserPromptWindow : CommonDialogWindowBase
    {
        /// <summary>
        /// This class contains the options for the dialog being shown.
        /// </summary>
        public class Options
        {
            /// <summary>
            /// The title for the dialog.
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            /// The main prompt for the dialog.
            /// </summary>
            public string Prompt { get; set; }

            /// <summary>
            /// The message shown under the prompt in the dialog.
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// The error details to show in the dialog.
            /// </summary>
            public string ErrorDetails { get; set; }

            /// <summary>
            /// The icon to use in the dialog. Should be 24x24 px.
            /// </summary>
            public ImageSource Icon { get; set; }

            /// <summary>
            /// The caption to use for the action button. If no caption is given then the
            /// action button is hidden.
            /// </summary>
            public string ActionButtonCaption { get; set; }

            /// <summary>
            /// The caption for the cancel button, but default it is "Cancel".
            /// </summary>
            public string CancelButtonCaption { get; set; } = GoogleCloudExtension.Resources.UiCancelButtonCaption;
        }

        private UserPromptWindowViewModel ViewModel { get; }

        private UserPromptWindow(Options options) : base(options.Title)
        {
            ViewModel = new UserPromptWindowViewModel(this, options);
            Content = new UserPromptWindowContent { DataContext = ViewModel };
        }

        /// <summary>
        /// Show the prompt to the user with the given options.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>Returns true if the user pressed the action button, false otherwise.</returns>
        public static bool PromptUser(Options options)
        {
            var dialog = new UserPromptWindow(options);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
