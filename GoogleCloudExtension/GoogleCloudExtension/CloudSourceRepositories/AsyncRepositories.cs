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

using Google.Apis.CloudSourceRepositories.v1.Data;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// Wrapper on top of AsyncProperty.
    /// Retrieves a list of <seealso cref="Repo"/> objects by calling async task.
    /// </summary>
    public class AsyncRepositories : Model
    {
        private AsyncProperty<IList<Repo>> _latest;

        /// <summary>
        /// Gets the list of Repo objects
        /// It can be null if list task is not started or not completed.
        /// </summary>
        public IList<Repo> Value => _latest?.Value;

        /// <summary>
        /// Returns the current repo list display option. Refer to <seealso cref="DisplayOptions"/>.
        /// </summary>
        public DisplayOptions DisplayState
        {
            get
            {
                if (_latest?.Value == null || _latest.IsPending)
                {
                    return DisplayOptions.Pending;
                }
                return _latest.Value.Any() ? DisplayOptions.HasItems : DisplayOptions.NoItems;
            }
        }

        /// <summary>
        /// Start an async task to get the list of repos.
        /// Note: In the case of reentrancy, _latest is reset. Prior task values are abandoned.
        /// </summary>
        /// <param name="projectId">GCP project id.</param>
        public async Task StartListRepoTaskAsync(string projectId)
        {
            Debug.WriteLine(nameof(StartListRepoTaskAsync));
            _latest = AsyncPropertyUtils.CreateAsyncProperty(CsrUtils.GetCloudReposAsync(projectId));
            RaiseAllPropertyChanged();
            await _latest.ValueTask;
            RaiseAllPropertyChanged();
        }

        /// <summary>
        /// The async property display options.
        /// </summary>
        public enum DisplayOptions
        {
            /// <summary>
            /// Task is not started or not completed, show it as pending
            /// </summary>
            Pending,

            /// <summary>
            /// Show the list is empty
            /// </summary>
            NoItems,

            /// <summary>
            /// Show the list
            /// </summary>
            HasItems
        }
    }
}
