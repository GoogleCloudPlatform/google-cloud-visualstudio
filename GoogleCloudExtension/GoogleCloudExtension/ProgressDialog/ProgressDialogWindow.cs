using GoogleCloudExtension.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ProgressDialog
{
    public class ProgressDialogWindow : CommonDialogWindowBase
    {
        public ProgressDialogWindowViewModel ViewModel { get; }

        private ProgressDialogWindow(string title, string message, Task task) : base(title)
        {
            ViewModel = new ProgressDialogWindowViewModel(this, message, task);
            Content = new ProgressDialogWindowContent { DataContext = ViewModel };
        }

        public static void PromptUser(string title, string message, Task task)
        {
            var dialog = new ProgressDialogWindow(title: title, message: message, task: task);
            dialog.ShowModal();
            if (!dialog.ViewModel.WasCancelled)
            {
                task.Wait(); // So the inner exception is thrown.
            }
        }

        public static T PromptUser<T>(string title, string message, Task<T> task) where T : class
        {
            var dialog = new ProgressDialogWindow(title: title, message: message, task: task);
            dialog.ShowModal();

            // Check if the dialog closed because the task completed or because the user cancelled the operation.
            if (dialog.ViewModel.WasCancelled)
            {
                return null;
            }
            else
            {
                return task.Result; // So the inner exception is thrown.
            }
        }
    }
}
