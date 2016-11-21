using GoogleCloudExtension.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.AddTrafficSplit
{
    public class AddTrafficSplitWindow : CommonDialogWindowBase
    {
        public AddTrafficSplitViewModel ViewModel { get; }

        private AddTrafficSplitWindow(IEnumerable<string> versions) : base("Add Traffic Split")
        {
            ViewModel = new AddTrafficSplitViewModel(this, versions);
            Content = new AddTrafficSplitWindowContent
            {
                DataContext = ViewModel
            };
        }

        public AddTrafficSplitResult PromptUser(IEnumerable<string> versions)
        {
            var dialog = new AddTrafficSplitWindow(versions);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
