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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// Retrieves a list of <seealso cref="Repo"/> objects by calling async task.
    /// 
    /// The main goal is to allow user to choose different project while waiting for the list to be populated.
    /// Anytime, user can change project and then it calls StartListRepoTaskAsync to update the repo list.
    /// </summary>
    public class AsyncRepositories : Model
    {
        internal static Func<string, Task<IList<Repo>>> GetCloudReposAsync = CsrUtils.GetCloudReposAsync;
        private Task<IList<Repo>> _latestTask;

        /// <summary>
        /// Gets the list of Repo objects
        /// It can be null if list task is not started or not completed.
        /// </summary>
        public IList<Repo> Value => _latestTask?.IsCompleted ?? false ? _latestTask.Result : null;

        /// <summary>
        /// Returns the current repo list display option. Refer to <seealso cref="DisplayOptions"/>.
        /// </summary>
        public DisplayOptions DisplayState
        {
            get
            {
                if (Value == null)
                {
                    return DisplayOptions.Pending;
                }
                return Value.Any() ? DisplayOptions.HasItems : DisplayOptions.NoItems;
            }
        }

        /// <summary>
        /// Clear the list of Repositories.
        /// </summary>
        public void ClearList()
        {
            _latestTask = null;
            UpdateValueAndDisplayState();
        }

        /// <summary>
        /// Start an async task to get the list of repos.
        /// Note: In the case of reentrancy, _latestTask is reset. Prior task values are abandoned.
        /// </summary>
        /// <param name="projectId">GCP project id.</param>
        public async Task StartListRepoTaskAsync(string projectId)
        {
            Debug.WriteLine(nameof(StartListRepoTaskAsync));
            _latestTask = GetCloudReposAsync(projectId);
            UpdateValueAndDisplayState();
            await _latestTask;
            UpdateValueAndDisplayState();
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

        private void UpdateValueAndDisplayState()
        {
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(DisplayState));
        }
    }
}
