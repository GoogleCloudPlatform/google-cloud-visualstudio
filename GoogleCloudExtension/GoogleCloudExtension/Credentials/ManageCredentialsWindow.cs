using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Credentials
{
    class ManageCredentialsWindow : DialogWindow
    {
        public ManageCredentialsWindow()
        {
            Title = "Manage Google Credentials";
            Content = new ManageCredentialsWindowContent { DataContext = new ManageCredentialsViewModel() };
        }
    }
}
