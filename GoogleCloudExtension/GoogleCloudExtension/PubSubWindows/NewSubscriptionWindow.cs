using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.Theming;
using System.Windows;

namespace GoogleCloudExtension.PubSubWindows
{
    public class NewSubscriptionWindow : CommonDialogWindowBase
    {
        public NewSubscriptionViewModel ViewModel { get; }

        public NewSubscriptionWindow(string topicFullName) :
            base(GoogleCloudExtension.Resources.NewSubscriptionWindowTitle, 0, 0)
        {

            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            HasMinimizeButton = false;
            HasMaximizeButton = false;

            Subscription model = new Subscription { Topic = topicFullName };

            ViewModel = new NewSubscriptionViewModel(model, this);
            Content = new NewSubscriptionWindowContent(ViewModel);
        }

        public static Subscription PromptUser(string topicFullName)
        {
            var dialog = new NewSubscriptionWindow(topicFullName);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}