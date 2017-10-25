using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.AppEngineManagement
{
    public class AppEngineManagementWindow : CommonDialogWindowBase
    {
        private AppEngineManagementViewModel ViewModel { get; }

        private AppEngineManagementWindow(string projectId)
            : base(GoogleCloudExtension.Resources.AppEngineManagementWindowCaption)
        {
            ViewModel = new AppEngineManagementViewModel(this, projectId);
            Content = new AppEngineManagementWindowContent { DataContext = ViewModel };
        }

        public static string PromptUser(string projectId)
        {
            var dialog = new AppEngineManagementWindow(projectId);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
