using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace GoogleCloudExtension.OauthLoginFlow
{
    /// <summary>
    /// Interaction logic for OauthLoginFlowWindowContent.xaml
    /// </summary>
    public partial class OauthLoginFlowWindowContent : UserControl
    {
        private const string SuccessCodePrefix = "Success code=";

        private readonly OAuthLoginFlowWindow _owner;

        private OAuthLoginFlowViewModel ViewModel => (OAuthLoginFlowViewModel)DataContext;

        public OauthLoginFlowWindowContent(OAuthLoginFlowWindow owner)
        {
            InitializeComponent();

            _owner = owner;
        }

        public void Navigate(string url)
        {
            _webBrowser.Navigate(url);
        }

        private void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            ViewModel.Message = "Navigating...";
        }

        private void WebBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            var self = (WebBrowser)sender;
            dynamic doc = self.Document;
            var title = (string)doc.title;
            ViewModel.Message = $"Navigated, title: {title}";

            Debug.WriteLine($"Navigated, Title: {title}");

            if (IsSuccessCode(title))
            {
                Debug.Write($"Found access code: {GetAccessCode(title)}");
                ViewModel.AccessCode = GetAccessCode(title);
                _owner.Close();
            }
        }

        private string GetAccessCode(string title) => title.Substring(SuccessCodePrefix.Length);

        private bool IsSuccessCode(string title) => title.StartsWith(SuccessCodePrefix);
    }
}
