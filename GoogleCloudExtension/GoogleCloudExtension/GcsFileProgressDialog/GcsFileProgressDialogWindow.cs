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
using System.Collections.Generic;
using System.Threading;

namespace GoogleCloudExtension.GcsFileProgressDialog
{
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

        public static void PromptUser(string caption, string message, IEnumerable<GcsFileOperation> operations, CancellationTokenSource tokenSource)
        {
            var dialog = new GcsFileProgressDialogWindow(caption, message, operations, tokenSource);
            dialog.ShowModal();
        }
    }
}
