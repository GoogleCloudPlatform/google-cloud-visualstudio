using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// The window for the new pub sub subscription dialog.
    /// </summary>
    public class NewSubscriptionWindow : CommonDialogWindowBase
    {
        public NewSubscriptionViewModel ViewModel { get; }

        public NewSubscriptionWindow(string topicFullName) :
            base(GoogleCloudExtension.Resources.NewSubscriptionWindowTitle, 322, 236)
        {
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