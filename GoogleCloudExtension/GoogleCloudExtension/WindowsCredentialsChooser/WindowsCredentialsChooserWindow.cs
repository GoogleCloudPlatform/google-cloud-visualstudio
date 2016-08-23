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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.WindowsCredentialsChooser
{
    /// <summary>
    /// This class represents a dialog that can be used to choose a set of Windows VM credentials.
    /// </summary>
    public class WindowsCredentialsChooserWindow : CommonDialogWindowBase
    {
        /// <summary>
        /// This class contains the options used for the dialog.
        /// </summary>
        public class Options
        {
            /// <summary>
            /// The title of the window.
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            /// The message to show in the dialog.
            /// </summary>
            public string Message { get; set; }
        }

        private WindowsCredentialsChooserViewModel ViewModel { get; }

        private WindowsCredentialsChooserWindow(Instance instance, Options options) :
            base(options.Title, 300, 150)
        {
            ViewModel = new WindowsCredentialsChooserViewModel(instance, options, this);
            Content = new WindowsCredentialsChooserWindowContent { DataContext = ViewModel };
        }

        /// <summary>
        /// Opens the dialog and returns the selected credentials if any.
        /// </summary>
        /// <param name="instance">The Windows VM.</param>
        /// <param name="options">The options for the dialog</param>
        /// <returns>The selected credentials, null if cancelled out.</returns>
        public static WindowsInstanceCredentials PromptUser(Instance instance, Options options)
        {
            var dialog = new WindowsCredentialsChooserWindow(instance, options);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
