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

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.LinkPrompt;
using System;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    internal static class GCloudWrapperUtils
    {
        /// <summary>
        /// Verify that the Cloud SDK is installed and at the right version. Optionally also verify that the given
        /// component is installed.
        /// </summary>
        /// <param name="component">The component to check, optional.</param>
        /// <returns>True if the Cloud SDK installation is valid.</returns>
        public static async Task<bool> VerifyGCloudDependencies(GCloudComponent component = GCloudComponent.None)
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
                    message: String.Format(
                        Resources.GCloudWrapperUtilsOldCloudSdkMessage,
                        result.CloudSdkVersion,
                        GCloudWrapper.GCloudSdkMinimumVersion),
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
