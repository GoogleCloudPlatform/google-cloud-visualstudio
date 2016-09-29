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

namespace GoogleCloudExtension.Utils
{
    internal static class GaeUtils
    {
        // The '.' seperator used in app engine urls.
        private static readonly string AppEngineUrlSeperator = "-dot-";

        // The name of the default app engine service.
        private static readonly string AppEngineDefaultServiceName = "default";


        /// <summary>
        /// Gets an url for an App Engine application.
        /// </summary>
        /// <param name="hostname">The hostname of the App Engine app, normally in the form
        /// [project-id].appsport.com</param>
        /// <param name="serviceId">The App Engine service id.</param>
        /// <param name="versionId">The App Engine service id.</param>
        /// <returns>The properly formatted url to the App Engine application</returns>
        public static string GetAppUrl(string hostname, string serviceId = null, string versionId = null) {
            string url = "https://";
       
            if (!string.IsNullOrWhiteSpace(versionId))
            {
                url += versionId + AppEngineUrlSeperator;
            }

            if (!string.IsNullOrWhiteSpace(serviceId) && serviceId != AppEngineDefaultServiceName)
            {
                url += serviceId + AppEngineUrlSeperator;
            }

            url += hostname;
            return url;
        }
    }
}
