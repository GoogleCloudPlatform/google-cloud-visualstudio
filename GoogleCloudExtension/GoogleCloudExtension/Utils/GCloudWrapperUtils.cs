using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.LinkPrompt;
using System;
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
                    message: String.Format(Resources.GCloudWrapperUtilsOldCloudSdkMessage, result.CloudSdkVersion),
                    title: Resources.GCloudWrapperUtilsOldCloudSdkTitle);
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
