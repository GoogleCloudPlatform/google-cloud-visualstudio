using GoogleCloudExtension.PublishDialogSteps.ChoiceStep;
using GoogleCloudExtension.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PublishDialog
{
    public class PublishDialogWindow : CommonDialogWindowBase
    {
        private PublishDialogWindowViewModel ViewModel { get; }

        private PublishDialogWindow() : base("Publish Application", 500, 400)
        {
            var initialStep = ChoiceStepViewModel.CreateStep();

            ViewModel = new PublishDialogWindowViewModel(initialStep, this);
            Content = new PublishDialogWindowContent { DataContext = ViewModel };
        }

        public static void PromptUser()
        {
            var dialog = new PublishDialogWindow();
            dialog.ShowModal();
        }
    }
}
