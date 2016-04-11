using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.OauthLoginFlow
{
    public class OAuthLoginFlowWindow : DialogWindow
    {
        private OAuthLoginFlowViewModel ViewModel { get; }

        private OAuthLoginFlowWindow(string urlSource)
        {
            Title = "Log in";

            ViewModel = new OAuthLoginFlowViewModel();
            var windowContent = new OauthLoginFlowWindowContent(this)
            {
                DataContext = ViewModel,
            };
            windowContent.Navigate(urlSource);

            Content = windowContent;
        }

        internal static string RunOAuthFlow(string url)
        {
            var window = new OAuthLoginFlowWindow(url);
            window.ShowModal();
            return window.ViewModel.AccessCode;
        }
    }
}
