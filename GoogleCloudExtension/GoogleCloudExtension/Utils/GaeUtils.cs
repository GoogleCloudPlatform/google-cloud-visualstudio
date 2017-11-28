// Copyright 2016 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.AppEngineManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.ProgressDialog;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    internal static class GaeUtils
    {
        // The '.' seperator used in app engine urls.
        private static readonly string s_appEngineUrlSeperator = "-dot-";

        // The name of the default app engine service.
        public static readonly string AppEngineDefaultServiceName = "default";

        /// <summary>
        /// Gets an url for an App Engine application.
        /// </summary>
        /// <param name="hostname">The hostname of the App Engine app, normally in the form
        /// [project-id].appsport.com</param>
        /// <param name="serviceId">The App Engine service id.</param>
        /// <param name="versionId">The App Engine service id.</param>
        /// <returns>The properly formatted url to the App Engine application</returns>
        public static string GetAppUrl(string hostname, string serviceId = null, string versionId = null)
        {
            string url = "https://";

            if (!string.IsNullOrWhiteSpace(versionId))
            {
                url += versionId + s_appEngineUrlSeperator;
            }

            if (!string.IsNullOrWhiteSpace(serviceId) && serviceId != AppEngineDefaultServiceName)
            {
                url += serviceId + s_appEngineUrlSeperator;
            }

            url += hostname;
            return url;
        }

        /// <summary>
        /// Sets the app engine to the given project Id.
        /// </summary>
        public static async Task<bool> SetAppRegionAsync(string projectId, IGaeDataSource dataSource)
        {
            string selectedLocation = AppEngineManagementWindow.PromptUser(projectId);
            if (selectedLocation == null)
            {
                Debug.WriteLine("The user cancelled creating a new app.");
                return false;
            }

            try
            {
                await ProgressDialogWindow.PromptUser(
                    dataSource.CreateApplicationAsync(selectedLocation),
                    new ProgressDialogWindow.Options
                    {
                        Title = Resources.GaeUtilsSetAppEngineRegionProgressTitle,
                        Message = Resources.GaeUtilsSetAppEngineRegionProgressMessage,
                        IsCancellable = false
                    });
                return true;
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ExceptionPrompt(ex);
                return false;
            }
        }
    }
}
