using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CredentialsManagement.Models
{
    public class UserCredentials : Model
    {
        private AsyncPropertyValue<ImageSource> _profilePictureAsync;
        private AsyncPropertyValue<string> _nameAsync;
        private string _account;

        public AsyncPropertyValue<ImageSource> ProfilePictureAsync
        {
            get { return _profilePictureAsync; }
            set { SetValueAndRaise(ref _profilePictureAsync, value); }
        }

        public AsyncPropertyValue<string> NameAsync
        {
            get { return _nameAsync; }
            set { SetValueAndRaise(ref _nameAsync, value); }
        }

        public string Account
        {
            get { return _account; }
            set { SetValueAndRaise(ref _account, value); }
        }
    }
}
