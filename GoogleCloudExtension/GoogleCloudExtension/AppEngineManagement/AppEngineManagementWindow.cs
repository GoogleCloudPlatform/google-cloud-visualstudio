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

        private AppEngineManagementWindow()
            : base("App Engine App")
        {
            ViewModel = new AppEngineManagementViewModel(this);
            Content = new AppEngineManagementWindowContent { DataContext = ViewModel };
        }

        public static string PromptUser()
        {
            var dialog = new AppEngineManagementWindow();
            dialog.ShowModal();
            return dialog.ViewModel.SelectedLocation;
        }
    }
}
