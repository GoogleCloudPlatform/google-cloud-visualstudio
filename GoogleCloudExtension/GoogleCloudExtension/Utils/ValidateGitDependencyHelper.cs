using GoogleCloudExtension.GitUtils;
using GoogleCloudExtension.LinkPrompt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    public static class ValidateGitDependencyHelper
    {
        public static bool ValidateGitForWindowsInstalled()
        {
            if (String.IsNullOrWhiteSpace(GitRepository.GetGitPath()))
            {
                LinkPromptDialogWindow.PromptUser(
                        Resources.GcloudMissingGcloudErrorTitle,
                        Resources.GcloudMissingCloudSdkErrorMessage,
                        new LinkInfo(link: "https://git-scm.com/download/win", caption: Resources.GcloudInstallLinkCaption));
                return false;
            }
            return true;
        }
    }
}
