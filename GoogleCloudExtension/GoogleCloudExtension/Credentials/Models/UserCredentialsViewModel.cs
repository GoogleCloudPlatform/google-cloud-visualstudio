using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtension.Credentials.Models
{
    public class UserCredentialsViewModel : Model
    {
        public AsyncPropertyValue<ImageSource> ProfilePictureAsync { get; }

        public AsyncPropertyValue<string> NameAsync { get; }

        public string AccountName { get; }

        public UserCredentialsViewModel(UserCredentials userCredentials)
        {
            AccountName = userCredentials.AccountName;

            var profileTask = ProfileManager.GetProfileForCredentialsAsync(userCredentials);

            // TODO: Show the default image while it is being loaded.
            ProfilePictureAsync = new AsyncPropertyValue<ImageSource>(LoadImageAsync(profileTask));
            NameAsync = AsyncPropertyValue<string>.CreateAsyncProperty(profileTask, x => x.DisplayName);
        }

        private async Task<ImageSource> LoadImageAsync(Task<GPlusProfile> profileTask)
        {
            // TODO: If no profile image then return the default image.
            var profile = await profileTask;
            return new BitmapImage { UriSource =new Uri(profile.Image.Url) };
        }
    }
}
