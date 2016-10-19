using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// The window for the new pub sub topic dialog.
    /// </summary>
    public class NewTopicWindow : CommonDialogWindowBase
    {
        public NewTopicViewModel ViewModel { get; }

        public NewTopicWindow(string projectId) :
            base(GoogleCloudExtension.Resources.NewTopicWindowTitle, 303, 138)
        {
            ViewModel = new NewTopicViewModel(projectId, this);
            Content = new NewTopicWindowContent(ViewModel);
        }

        public static string PromptUser(string projectId)
        {
            var dialog = new NewTopicWindow(projectId);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}