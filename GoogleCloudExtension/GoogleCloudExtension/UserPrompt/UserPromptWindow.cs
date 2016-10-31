using GoogleCloudExtension.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.UserPrompt
{
    public class UserPromptWindow : CommonDialogWindowBase
    {
        public class Options
        {
            public string Title { get; set; }

            public string Prompt { get; set; }

            public string Message { get; set; }

            public ImageSource Icon { get; set; }

            public string ActionButtonCaption { get; set; }
        }

        private UserPromptWindowViewModel ViewModel { get; }

        private UserPromptWindow(Options options) : base(options.Title)
        {
            ViewModel = new UserPromptWindowViewModel(this, options);
            Content = new UserPromptWindowContent { DataContext = ViewModel };
        }

        public static bool PromptUser(Options options)
        {
            var dialog = new UserPromptWindow(options);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
