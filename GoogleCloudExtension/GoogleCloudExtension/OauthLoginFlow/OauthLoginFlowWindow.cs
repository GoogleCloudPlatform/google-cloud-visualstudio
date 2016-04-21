using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.OauthLoginFlow
{
    public class OAuthLoginFlowWindow : DialogWindow
    {
        private const int InternetOptionSupressBehavior = 81;
        private const int InternetSupressCookiePersist = 3;
        private const int InternetEnableCookiePersist = 4;

        private OAuthLoginFlowViewModel ViewModel { get; }

        private OAuthLoginFlowWindow(string urlSource)
        {
            Title = "Log in";
            Width = 500;
            Height = 500;

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
            try
            {
                DisableCookieStorage();
                var window = new OAuthLoginFlowWindow(url);
                window.ShowModal();
                return window.ViewModel.AccessCode;
            }
            finally
            {
                EnableCookieStorage();
            }
        }

        [System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetOption(int hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        private static void DisableCookieStorage()
        {
            unsafe
            {
                int option = InternetSupressCookiePersist;
                int* optionPtr = &option;

                bool success = InternetSetOption(0, InternetOptionSupressBehavior, new IntPtr(optionPtr), sizeof(int));
                if (success)
                {
                    Debug.WriteLine("Supressed storing cookies for the process.");
                }
                else
                {
                    Debug.WriteLine("Failed suppressing storage of cookies.");
                }
            }
        }

        private static void EnableCookieStorage()
        {
            unsafe
            {
                int option = InternetEnableCookiePersist;
                int* optionPtr = &option;

                bool success = InternetSetOption(0, InternetOptionSupressBehavior, new IntPtr(optionPtr), sizeof(int));
                if (success)
                {
                    Debug.WriteLine("Supressed storing cookies for the process.");
                }
                else
                {
                    Debug.WriteLine("Failed suppressing storage of cookies.");
                }
            }
        }
    }
}
