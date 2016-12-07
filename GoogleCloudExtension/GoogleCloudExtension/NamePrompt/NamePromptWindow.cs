using GoogleCloudExtension.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.NamePrompt
{
    public class NamePromptWindow : CommonDialogWindowBase
    {
        private NamePromptViewModel ViewModel { get; }

        private NamePromptWindow() : base("Enter name")
        {
            ViewModel = new NamePromptViewModel(this);
            Content = new NamePromptContent
            {
                DataContext = ViewModel
            };
        }

        public static string PromptUser()
        {
            var dialog = new NamePromptWindow();
            dialog.ShowModal();
            return dialog.ViewModel.Name;
        }
    }
}
