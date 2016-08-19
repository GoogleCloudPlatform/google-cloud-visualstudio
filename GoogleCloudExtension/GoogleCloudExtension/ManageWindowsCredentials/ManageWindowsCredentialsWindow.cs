using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.ManageWindowsCredentials
{
    public class ManageWindowsCredentialsWindow : CommonDialogWindowBase
    {
        private ManageWindowsCredentialsWindow(Instance instance) : base("Manage Windows Credentials", 500, 400)
        {
            Content = new ManageWindowsCredentialsWindowContent { DataContext = new ManageWindowsCredentialsViewModel(instance, this) };
        }

        public static void PromptUser(Instance instance)
        {
            var dialog = new ManageWindowsCredentialsWindow(instance);
            dialog.ShowModal();
        }
    }
}
