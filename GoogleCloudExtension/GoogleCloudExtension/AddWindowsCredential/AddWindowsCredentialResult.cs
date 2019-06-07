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

namespace GoogleCloudExtension.AddWindowsCredential
{
    public class AddWindowsCredentialResult
    {
        /// <summary>
        /// The password the user selected.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// The user name for which to set the password.
        /// </summary>
        public string User { get; private set; }

        /// <summary>
        /// Whether the user requested the password to be generated. This is mutually exclusive with
        /// specifying the password.
        /// </summary>
        public bool GeneratePassword => string.IsNullOrEmpty(Password);

        public AddWindowsCredentialResult(string user, string password = null)
        {
            Password = password;
            User = user;
        }
    }
}
