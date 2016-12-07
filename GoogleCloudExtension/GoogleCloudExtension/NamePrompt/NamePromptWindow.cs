using GoogleCloudExtension.Theming;

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
