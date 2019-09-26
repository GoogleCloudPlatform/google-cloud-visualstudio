// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading.Tasks;
using GoogleCloudExtension.CopyablePrompt;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.LinkPrompt;
using GoogleCloudExtension.Services;

namespace GoogleCloudExtension.Utils
{
    internal static class GCloudWrapperUtils
    {
        /// <summary>
        /// Override to use for testing.
        /// </summary>
        internal static Action<string, string, string> ShowCopyablePromptOverride { private get; set; }

        private static Action<string, string, string> ShowCopyablePrompt =>
            ShowCopyablePromptOverride ?? CopyablePromptDialogWindow.PromptUser;

        private static IGCloudWrapper GCloudWrapper =>
            GoogleCloudExtensionPackage.Instance.GetMefService<IGCloudWrapper>();

        /// <summary>
        /// Verify that the Cloud SDK is installed and at the right version. Optionally also verify that the given
        /// component is installed.
        /// </summary>
        /// <param name="component">The component to check, optional.</param>
        /// <returns>True if the Cloud SDK installation is valid.</returns>
        public static async Task<bool> VerifyGCloudDependenciesAsync(GCloudComponent component = GCloudComponent.None)
        {
            GCloudValidationResult result = await GCloudWrapper.ValidateGCloudAsync(component);
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
            else if (result.IsObsolete)
            {
                UserPromptService.Default.ErrorPrompt(
                    message: string.Format(
                        Resources.GCloudWrapperUtilsOldCloudSdkMessage,
                        result.CloudSdkVersion,
                        GCloud.GCloudWrapper.GCloudSdkMinimumVersion),
                    title: Resources.GCloudWrapperUtilsOldCloudSdkTitle);
            }
            else
            {
                ShowCopyablePrompt(
                    Resources.GcloudMissingComponentTitle,
                    string.Format(Resources.GcloudMissingComponentErrorMessage, component),
                    string.Format(Resources.GcloudMissingComponentInstallCommand, component.ToString().ToLower()));
            }

            return false;
        }
    }
}
