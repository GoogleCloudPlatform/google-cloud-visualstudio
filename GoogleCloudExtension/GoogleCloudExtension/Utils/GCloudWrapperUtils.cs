using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.LinkPrompt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    internal static class GCloudWrapperUtils
    {
        public static async Task<bool> VerifyGCloudDependencies(string component = null)
        {
            var result = await GCloudWrapper.ValidateGCloudAsync(component);
            if (result.IsValid)
            {
                return true;
            }

            if (!result.IsCloudSdkInstalled)
            {
                LinkPromptDialogWindow.PromptUser(
                        Resources.GcloudMissingGcloudErrorTitle,
                        Resources.GcloudMissingCloudSdkErrorMessage,
                        new LinkInfo(link: "https://cloud.google.com/sdk/", caption: Resources.GcloudInstallLinkCaption));
            }
            else if (!result.IsCloudSdkUpdated)
            {
                UserPromptUtils.ErrorPrompt(
                    message: $"The version of Cloud SDK {result.CloudSdkVersion} needs to be updated. Please update your Cloud SDK installation by running: gcloud components update",
                    title: "Google Cloud SDK too old");
            }
            else
            {
                UserPromptUtils.ErrorPrompt(
                       message: String.Format(Resources.GcloudMissingComponentErrorMessage, component),
                       title: Resources.GcloudMissingComponentTitle);
            }

            return false;
        }
    }
}
