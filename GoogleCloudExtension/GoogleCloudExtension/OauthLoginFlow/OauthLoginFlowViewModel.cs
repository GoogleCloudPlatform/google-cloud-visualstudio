using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.OauthLoginFlow
{
    class OAuthLoginFlowViewModel : ViewModelBase
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
