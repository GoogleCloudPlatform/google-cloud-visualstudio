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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.GCloud;
using System.Collections.Generic;

namespace GoogleCloudExtension.Accounts
{
    /// <summary>
    /// Extracted interface from <see cref="WindowsCredentialsStore"/> for testing purposes.
    /// </summary>
    internal interface IWindowsCredentialsStore
    {
        /// <summary>
        /// Adds a Windows credential to the store for the given <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The GCE VM.</param>
        /// <param name="credentials">The credentials to store.</param>
        void AddCredentialsToInstance(Instance instance, WindowsInstanceCredentials credentials);

        /// <summary>
        /// Deletes the given credentials from the list of associated credenials for <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The GCE VM.</param>
        /// <param name="credentials">The credentials.</param>
        void DeleteCredentialsForInstance(Instance instance, WindowsInstanceCredentials credentials);

        /// <summary>
        /// Loads the list of Windows credentials associated with <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The GCE VM</param>
        /// <returns>The list of <seealso cref="WindowsInstanceCredentials"/> associated with The GCE VM. It might be
        /// empty if no credentials are found.</returns>
        IEnumerable<WindowsInstanceCredentials> GetCredentialsForInstance(Instance instance);

        /// <summary>
        /// Returns the path where to store credential related information for a GCE VM.
        /// </summary>
        /// <param name="instance">The GCE VM.</param>
        /// <returns>The full path where to store information for the instance.</returns>
        string GetStoragePathForInstance(Instance instance);
    }
}
