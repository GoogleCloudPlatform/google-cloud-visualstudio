using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.OauthLoginFlow
{
    internal class OAuthLoginFlowViewModel : ViewModelBase
    {
        private string _message;

        public string Message
        {
            get { return _message; }
            set { SetValueAndRaise(ref _message, value); }
        }

        public string AccessCode { get; set; }
    }
}
