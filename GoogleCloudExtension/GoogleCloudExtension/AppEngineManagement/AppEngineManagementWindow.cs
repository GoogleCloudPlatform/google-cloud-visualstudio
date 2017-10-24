using GoogleCloudExtension.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.AppEngineManagement
{
    public class AppEngineManagementWindow : CommonDialogWindowBase
    {
        private AppEngineManagementViewModel ViewModel { get; }

        private AppEngineManagementWindow(string projectId)
            : base("Select App Engine Region")
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
