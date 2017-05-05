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

using GoogleCloudExtension.GcsUtils;
using GoogleCloudExtension.Theming;
using System.Collections.Generic;
using System.Threading;

namespace GoogleCloudExtension.GcsFileProgressDialog
{
    /// <summary>
    /// Class that represents the GCS progress dialog for file operations.
    /// </summary>
    public class GcsFileProgressDialogWindow : CommonDialogWindowBase
    {
        private GcsFileProgressDialogWindow(
            string caption,
            string message,
            IEnumerable<GcsFileOperation> operations,
            CancellationTokenSource tokenSource) : base(caption)
        {
            var viewModel = new GcsFileProgressDialogViewModel(message, this, operations, tokenSource);
            Content = new GcsFileProgressDialogWindowContent
            {
                DataContext = viewModel
            };
        }

        /// <summary>
        /// Opens and shows the progress dialog.
        /// </summary>
        /// <param name="caption">The caption to use for the dialog.</param>
        /// <param name="message">The message to use in the dialog.</param>
        /// <param name="operations">The list of operations to track.</param>
        /// <param name="cancellationTokenSource">The <seealso cref="CancellationTokenSource"/> to be used to cancel the operations.</param>
        public static void PromptUser(
            string caption,
            string message,
            IEnumerable<GcsFileOperation> operations,
            CancellationTokenSource cancellationTokenSource)
        {
            var dialog = new GcsFileProgressDialogWindow(caption, message, operations, cancellationTokenSource);
            dialog.ShowModal();
        }
    }
}
