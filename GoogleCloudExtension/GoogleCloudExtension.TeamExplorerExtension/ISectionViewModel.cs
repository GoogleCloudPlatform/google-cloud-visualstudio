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


using System.Threading.Tasks;

namespace GoogleCloudExtension.TeamExplorerExtension
{
    /// <summary>
    /// Define interfance for Team Explorer section view model.
    /// </summary>
    public interface ISectionViewModel
    {
        /// <summary>
        /// Responds to refresh event.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Initializes the view model with <paramref name="teamExplorerService"/> input.
        /// </summary>
        Task InitializeAsync(ITeamExplorerUtils teamExplorerService);

        /// <summary>
        /// Notifies that the current active repository changed
        /// </summary>
        /// <param name="newRepoLocalPath">
        /// The new active repository local path.
        /// When the value is null, it means currently there is no active repository.
        /// </param>
        void UpdateActiveRepo(string newRepoLocalPath);

        /// <summary>
        /// Called by ITeamExplorerSection to do final resource clean up.
        /// </summary>
        void Cleanup();
    }
}
