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

namespace GoogleCloudExtension.ShowPassword
{
    /// <summary>
    /// This class represents the dialog that will show the user the Windows credentials
    /// obtained after a password reset.
    /// </summary>
    public class ShowPasswordWindow : CommonDialogWindowBase
    {
        public class Options
        {
            public string Title { get; set; }

            public string Password { get; set; }

            public string Message { get; set; }
        }

        private ShowPasswordWindow(Options options) : base(options.Title)
        {
            Content = new ShowPasswordWindowContent
            {
                DataContext = new ShowPasswordViewModel(options)
            };
        }

        /// <summary>
        /// Shows the given credentials to the user.
        /// </summary>
        public static void PromptUser(Options options)
        {
            var dialog = new ShowPasswordWindow(options);
            dialog.ShowModal();
        }
    }
}
