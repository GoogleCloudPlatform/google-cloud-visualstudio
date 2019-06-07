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

using System.Threading.Tasks;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.ProgressDialog
{
    /// <summary>
    /// This class represents the ProgressDialog window, which shows a progress indicator
    /// to the user while a <seealso cref="Task"/> is running.
    /// </summary>
    public class ProgressDialogWindow : CommonDialogWindowBase
    {
        /// <summary>
        /// The options for the dialog.
        /// </summary>
        public class Options
        {
            /// <summary>
            /// What message to display inside of the progress dialog.
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// The title for the progress dialog.
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            /// Whether the operation can be cancelled. Settings this to true will disable the cancel
            /// button and the close button in the dialog. By default the operations are not cancellable.
            /// </summary>
            public bool IsCancellable { get; set; }
        }

        private ProgressDialogWindowViewModel ViewModel { get; }

        private ProgressDialogWindow(Task task, Options options) : base(options.Title)
        {
            ViewModel = new ProgressDialogWindowViewModel(this, options, task);
            Content = new ProgressDialogWindowContent { DataContext = ViewModel };

            IsCloseButtonEnabled = options.IsCancellable;
        }

        /// <summary>
        /// Prompts the user with the progress dialog and awaits the end of the <seealso cref="Task"/> to close
        /// itself.
        /// </summary>
        /// <param name="task">The <seealso cref="Task"/> instance to show progress for.</param>
        /// <param name="options">The options for the dialog.</param>
        /// <returns>A task that can be awaited to get the result of the operation.</returns>
        public static async Task PromptUserAsync(Task task, Options options)
        {
            var dialog = new ProgressDialogWindow(task, options);
            dialog.ShowModal();
            if (!dialog.ViewModel.WasCancelled)
            {
                // Await the task to get the value it holds or to force it to throw
                // the exception it holds if it failed.
                await task;
            }
        }

        /// <summary>
        /// Prompts the user with the progress dialog and awaits the end of the <seealso cref="Task{TResult}"/> to close
        /// itself.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task">The <seealso cref="Task{TResult}"/> instance to show progress for.</param>
        /// <param name="options">The options for the dialog.</param>
        /// <returns>A task that can be awaited to get the result of the operation.</returns>
        public static async Task<T> PromptUserAsync<T>(Task<T> task, Options options)
        {
            var dialog = new ProgressDialogWindow(task, options);
            dialog.ShowModal();

            // Check if the dialog closed because the task completed or because the user cancelled the operation.
            if (dialog.ViewModel.WasCancelled)
            {
                return default;
            }
            else
            {
                // Await the task to get the value it holds or to force it to throw
                // the exception it holds if it failed.
                return await task;
            }
        }
    }
}
