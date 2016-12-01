using GoogleCloudExtension.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.UploadProgressDialog
{
    internal class UploadProgressDialogWindow : CommonDialogWindowBase
    {
        private UploadProgressDialogWindow(IEnumerable<UploadOperation> uploads, CancellationTokenSource tokenSource) :
            base("Uploading Files")
        {
            var viewModel = new UploadProgressDialogViewModel(this, uploads, tokenSource);
            Content = new UploadProgressDialogWindowContent
            {
                DataContext = viewModel
            };
        }

        public static void PromptUser(IEnumerable<UploadOperation> uploads, CancellationTokenSource tokenSource)
        {
            var dialog = new UploadProgressDialogWindow(uploads, tokenSource);
            dialog.ShowModal();
        }
    }
}
