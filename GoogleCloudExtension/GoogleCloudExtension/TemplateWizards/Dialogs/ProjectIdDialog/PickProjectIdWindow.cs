using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.ProjectIdDialog
{
    public interface IPickProjectIdWindow
    {
        void Close();
    }

    public class PickProjectIdWindow : CommonDialogWindowBase, IPickProjectIdWindow
    {
        private PickProjectIdViewModel ViewModel { get; }

        private PickProjectIdWindow() : base(GoogleCloudExtension.Resources.WizardPickProjectIdTitle)
        {
            ViewModel = new PickProjectIdViewModel(this);
            Content = new PickProjectIdWindowContent { DataContext = ViewModel };
        }

        public static string PromptUser()
        {
            var dialog = new PickProjectIdWindow();
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
