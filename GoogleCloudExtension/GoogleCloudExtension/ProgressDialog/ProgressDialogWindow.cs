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
