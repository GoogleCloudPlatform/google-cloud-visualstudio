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

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Plus.v1;
using Google.Apis.Plus.v1.Data;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class wraps a PlusService API service, which can be used to find information
    /// from the user's profile.
    /// </summary>
    public class GPlusDataSource : DataSourceBase<PlusService>, IGPlusDataSource
    {
        public GPlusDataSource(GoogleCredential credential, string appName)
            : base(credential, init => new PlusService(init), appName)
        { }

        /// <summary>
        /// Fetches the profile for the authenticated user.
        /// </summary>
        public async Task<Person> GetProfileAsync()
        {
            try
            {
                return await Service.People.Get("me").ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get person: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
