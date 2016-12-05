using GoogleCloudExtension.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsFileProgressDialog
{
    internal class GcsFileProgressDialogWindow : CommonDialogWindowBase
    {
        private GcsFileProgressDialogWindow(IEnumerable<GcsFileOperation> operations, CancellationTokenSource tokenSource) :
            base("Uploading Files")
        {
            var viewModel = new GcsFileProgressDialogViewModel(this, operations, tokenSource);
            Content = new GcsFileProgressDialogWindowContent
            {
                DataContext = viewModel
            };
        }

        public static void PromptUser(IEnumerable<GcsFileOperation> operations, CancellationTokenSource tokenSource)
        {
            var dialog = new GcsFileProgressDialogWindow(operations, tokenSource);
            dialog.ShowModal();
        }
    }
}
