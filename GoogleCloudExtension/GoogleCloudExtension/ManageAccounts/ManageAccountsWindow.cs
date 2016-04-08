using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ManageAccounts
{
    public class ManageAccountsWindow : DialogWindow
    {
        public ManageAccountsWindow()
        {
            Title = "Manage Accounts";
            Content = new ManageAccountsWindowContent { DataContext = new ManageAccountsViewModel(this) };
        }
    }
}
