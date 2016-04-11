using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.OauthLoginFlow
{
    class OauthLoginFlowWindow : DialogWindow
    {
        public OauthLoginFlowWindow(string urlSource)
        {
            Title = "Log in";

            var windowContent = new OauthLoginFlowWindowContent
            {
                DataContext = new OauthLoginFlowViewModel(),
            };
            windowContent.Navigate(urlSource);

            Content = windowContent;
        }
    }
}
