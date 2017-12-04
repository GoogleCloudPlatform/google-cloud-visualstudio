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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ApiManagement
{
    public interface IApiManager
    {
        /// <summary>
        /// This method will check that all of the given service names are enabled.
        /// </summary>
        /// <param name="serviceNames">The list of services to check.</param>
        /// <returns>A task that will be true if all services are enabled, false otherwise.</returns>
        Task<bool> AreServicesEnabledAsync(IList<string> serviceNames);

        /// <summary>
        /// This method will check that all given services are enabled and if not will prompt the user to enable the
        /// necessary services.
        /// </summary>
        /// <param name="serviceNames">The services to check.</param>
        /// <param name="prompt">The prompt to use in the prompt dialog to ask the user for permission to enable the services.</param>
        /// <returns>A task that will be true if all services where enabled, false if the user cancelled or if the operation failed.</returns>
        Task<bool> EnsureAllServicesEnabledAsync(IEnumerable<string> serviceNames, string prompt);

        /// <summary>
        /// This method will enable the list of services given.
        /// </summary>
        /// <param name="serviceNames">The list of services to enable.</param>
        /// <returns>A task that will be completed once the operation finishes.</returns>
        Task EnableServicesAsync(IEnumerable<string> serviceNames);

    }
}
