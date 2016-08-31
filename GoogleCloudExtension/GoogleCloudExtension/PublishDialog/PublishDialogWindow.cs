using EnvDTE;
using GoogleCloudExtension.PublishDialogSteps.ChoiceStep;
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.PublishDialog
{
    public class PublishDialogWindow : CommonDialogWindowBase
    {
        private PublishDialogWindowViewModel ViewModel { get; }

        private PublishDialogWindow(Project project) : base("Publish Application", 500, 400)
        {
            var initialStep = ChoiceStepViewModel.CreateStep();

            ViewModel = new PublishDialogWindowViewModel(project, initialStep, this);
            Content = new PublishDialogWindowContent { DataContext = ViewModel };
        }

        public static void PromptUser(Project project)
        {
            var dialog = new PublishDialogWindow(project);
            dialog.ShowModal();
        }

        public static bool CanPublish(Project project)
        {
            var type = project.GetProjectType();
            return type == KnownProjectTypes.WebApplication;
        }
    }
}
