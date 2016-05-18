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

using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace GoogleCloudExtension.Accounts
{
    /// <summary>
    /// This class stores the complete data for a user account. The class can be serialize to a .json
    /// string with an schema compatible with gcloud by design. The serialized form of this class
    /// can be consumed by gcloud via the --credential-file-override parameter.
    /// The serialize form can also be consumed as the "application default credentials" by apps using
    /// Google's client NuGet packages.
    /// </summary>
    public class UserAccount
    {
        [JsonProperty("account")]
        public string AccountName { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("type")]
        public string Type => "authorized_user";

        public GoogleCredential GetGoogleCredential()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8, 100, leaveOpen: true))
                {
                    var serialized = JsonConvert.SerializeObject(this);
                    writer.Write(serialized);
                }

                stream.Position = 0;
                return GoogleCredential.FromStream(stream);
            }
        }
    }
}
