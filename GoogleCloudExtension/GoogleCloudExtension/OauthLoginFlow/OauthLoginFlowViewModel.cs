using GoogleCloudExtension.Utils;
using System.Windows.Input;

namespace GoogleCloudExtension.OauthLoginFlow
{
    internal class OAuthLoginFlowViewModel : ViewModelBase
    {
        private readonly OAuthLoginFlowWindow _owner;
        private string _message;

        public string Message
        {
            get { return _message; }
            set { SetValueAndRaise(ref _message, value); }
        }

        public ICommand CloseCommand { get; }

        public string RefreshCode { get; set; }

        public OAuthLoginFlowViewModel(OAuthLoginFlowWindow owner)
        {
            _owner = owner;

            CloseCommand = new WeakCommand(OnCloseCommand);
        }

        private void OnCloseCommand()
        {
            _owner.CancelOperation();
        }
    }
}
