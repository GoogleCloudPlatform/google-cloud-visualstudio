using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CredentialsManagement.Models
{
    public class UserCredentialsViewModel : Model
    {
        public AsyncPropertyValue<ImageSource> ProfilePictureAsync { get; }

        public AsyncPropertyValue<string> NameAsync { get; }

        public string AccountName { get; }

        public UserCredentialsViewModel(UserCredentials userCredentials)
        {
            AccountName = userCredentials.AccountName;
            ProfilePictureAsync = new AsyncPropertyValue<ImageSource>(ProfileManager.GetProfilePictureForCredentialsAsync(userCredentials));
            NameAsync = new AsyncPropertyValue<string>(ProfileManager.GetProfileNameForCredentialsAsync(userCredentials));
        }
    }
}
