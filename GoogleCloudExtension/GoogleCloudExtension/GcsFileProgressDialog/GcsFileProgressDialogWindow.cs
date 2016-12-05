using GoogleCloudExtension.Theming;
using System.Collections.Generic;
using System.Threading;

namespace GoogleCloudExtension.GcsFileProgressDialog
{
    internal class GcsFileProgressDialogWindow : CommonDialogWindowBase
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
