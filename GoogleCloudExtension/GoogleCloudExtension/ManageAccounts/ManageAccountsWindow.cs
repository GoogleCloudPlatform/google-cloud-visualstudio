using Microsoft.VisualStudio.PlatformUI;

namespace GoogleCloudExtension.ManageAccounts
{
    public class ManageAccountsWindow : DialogWindow
    {
        public ManageAccountsWindow()
        {
            Title = "Manage Accounts";
            Width = 500;
            Height = 400;
            ResizeMode = System.Windows.ResizeMode.NoResize;
            Content = new ManageAccountsWindowContent { DataContext = new ManageAccountsViewModel(this) };
        }
    }
}
