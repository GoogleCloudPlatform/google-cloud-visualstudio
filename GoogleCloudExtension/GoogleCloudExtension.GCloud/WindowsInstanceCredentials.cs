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

using GoogleCloudExtension.Utils;
using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class contains the credentials for a Windows VM.
    /// </summary>
    public sealed class WindowsInstanceCredentials
    {
        [JsonProperty("username")]
        public string User { get; }

        [JsonProperty("password")]
        public string Password { get; }

        [JsonConstructor]
        public WindowsInstanceCredentials(string user, string password)
        {
            user.ThrowIfNull(nameof(user));
            User = user;
            Password = password;
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => JsonConvert.SerializeObject(this);

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            else if (!(obj is WindowsInstanceCredentials))
            {
                return false;
            }
            else
            {
                var credentials = (WindowsInstanceCredentials)obj;
                return User == credentials.User &&
                    Password == credentials.Password;
            }
        }

        public override int GetHashCode()
        {
            int hashCode = -1879510246;
            hashCode = hashCode * -1521134295 + User.GetHashCode();
            hashCode = hashCode * -1521134295 + (Password?.GetHashCode() ?? 0);
            return hashCode;
        }
    }
}
