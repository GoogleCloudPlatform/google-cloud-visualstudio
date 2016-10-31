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
using System.Threading.Tasks;

namespace GoogleCloudExtension.ProgressDialog
{
    public class ProgressDialogWindow : CommonDialogWindowBase
    {
        public class Options
        {
            public string Message { get; set; }

            public string Title { get; set; }

            public string CancelToolTip { get; set; }

            public bool IsCancellable { get; set; } = true;
        }

        public ProgressDialogWindowViewModel ViewModel { get; }

        private ProgressDialogWindow(Task task, Options options) : base(options.Title)
        {
            ViewModel = new ProgressDialogWindowViewModel(this, options, task);
            Content = new ProgressDialogWindowContent { DataContext = ViewModel };

            IsCloseButtonEnabled = options.IsCancellable;
        }

        public static async Task PromptUser(Task task, Options options)
        {
            var dialog = new ProgressDialogWindow(task, options);
            dialog.ShowModal();
            if (!dialog.ViewModel.WasCancelled)
            {
                await task;
            }
        }

        public static async Task<T> PromptUser<T>(Task<T> task, Options options) where T : class
        {
            var dialog = new ProgressDialogWindow(task, options);
            dialog.ShowModal();

            // Check if the dialog closed because the task completed or because the user cancelled the operation.
            if (dialog.ViewModel.WasCancelled)
            {
                return default(T);
            }
            else
            {
                return await task; // So the inner exception is thrown.
            }
        }
    }
}
