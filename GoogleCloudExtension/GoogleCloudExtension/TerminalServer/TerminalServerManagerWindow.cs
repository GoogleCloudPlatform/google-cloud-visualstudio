using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.TerminalServer
{
    public class TerminalServerManagerWindow : CommonDialogWindowBase
    {
        private TerminalServerManagerViewModel ViewModel { get; }

        private TerminalServerManagerWindow(Instance instance):
            base(GoogleCloudExtension.Resources.TerminalServerManagerWindowTitle, 300, 150)
        {
            ViewModel = new TerminalServerManagerViewModel(instance, this);
            Content = new TerminalServerManagerWindowContent { DataContext = ViewModel };
        }

        public static void PromptUser(Instance instance)
        {
            var dialog = new TerminalServerManagerWindow(instance);
            dialog.ShowModal();
        }
    }
}
