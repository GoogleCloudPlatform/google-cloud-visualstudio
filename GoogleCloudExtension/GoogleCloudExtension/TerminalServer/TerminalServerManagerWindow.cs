using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.TerminalServer
{
    public class TerminalServerManagerWindow : CommonDialogWindowBase
    {
        private TerminalServerManagerViewModel ViewModel { get; }

        private TerminalServerManagerWindow(Instance instance):
            base("Start Terminal Server session", 300, 150)
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
