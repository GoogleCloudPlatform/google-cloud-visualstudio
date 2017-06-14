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

using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// A repository object binding to list view item
    /// </summary>
    public class RepoItem : Model
    {
        private bool _isActiveRepo;

        /// <summary>
        /// Gets the repository name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the repository local path.
        /// </summary>
        public string LocalPath { get; }

        /// <summary>
        /// Gets the repository full name.
        /// </summary>
        public string RepoFullName { get; }

        /// <summary>
        /// Gets if the repository is currently active one.
        /// </summary>
        public bool IsActiveRepo
        {
            get { return _isActiveRepo; }
            set { SetValueAndRaise(ref _isActiveRepo, value); }
        }

        /// <summary>
        /// The command that opens repository url.
        /// </summary>
        public ProtectedCommand VisitUrlCommand { get; }
    }
}
