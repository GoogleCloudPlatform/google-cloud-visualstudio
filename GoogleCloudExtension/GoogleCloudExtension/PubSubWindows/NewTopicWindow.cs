using GoogleCloudExtension.Theming;
using System.Windows;

namespace GoogleCloudExtension.PubSubWindows
{
    public class NewTopicWindow : CommonDialogWindowBase
    {
        public NewTopicViewModel ViewModel { get; }

        public NewTopicWindow(string projectId) :
            base(GoogleCloudExtension.Resources.NewTopicWindowTitle, 0, 0)
        {
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            HasMinimizeButton = false;
            HasMaximizeButton = false;
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