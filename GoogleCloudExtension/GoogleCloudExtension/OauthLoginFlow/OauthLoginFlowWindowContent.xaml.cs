using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoogleCloudExtension.OauthLoginFlow
{
    /// <summary>
    /// Interaction logic for OauthLoginFlowWindowContent.xaml
    /// </summary>
    public partial class OauthLoginFlowWindowContent : UserControl
    {
        private OauthLoginFlowViewModel ViewModel => (OauthLoginFlowViewModel)DataContext;

        public OauthLoginFlowWindowContent()
        {
            InitializeComponent();
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
            ViewModel.Message = "Navigated...";
        }
    }
}
