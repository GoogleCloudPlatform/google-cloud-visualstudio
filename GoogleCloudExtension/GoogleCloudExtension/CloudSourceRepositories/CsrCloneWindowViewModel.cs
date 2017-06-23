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

using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.CloudSourceRepositories.v1.Data;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model for user control CsrCloneWindowContent.xaml.
    /// </summary>
    public class CsrCloneWindowViewModel : CsrCloneCreateViewModelBase
    {
        private Repo _selectedRepo;

        /// <summary>
        /// The list of repositories that belong to the project
        /// </summary>
        public AsyncRepositories RepositoriesAsync { get; } = new AsyncRepositories();

        /// <summary>
        /// Currently selected repository
        /// </summary>
        public Repo SelectedRepository
        {
            get { return _selectedRepo; }
            set
            {
                SetValueAndRaise(ref _selectedRepo, value);
                ValidateInputs();
            }
        }

        public override ProtectedCommand OkCommand { get; }

        protected override string RepoName => SelectedRepository?.GetRepoName();

        public CsrCloneWindowViewModel(CsrCloneWindow owner, IList<Project> projects) : base(owner, projects)
        {
            OkCommand = new ProtectedAsyncCommand(
                () => ExecuteAsync(() => CloneAsync(SelectedRepository)), 
                canExecuteCommand: false);
            RepositoriesAsync.PropertyChanged += RepositoriesAsyncPropertyChanged;
        }

        protected override void OnSelectedProjectChanged(string projectId)
        {
            ErrorHandlerUtils.HandleAsyncExceptions(() =>
                RepositoriesAsync.StartListRepoTaskAsync(projectId));
        }

        private void RepositoriesAsyncPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                // RaiseAllPropertyChanged set e.PropertyName as null
                case null:
                case nameof(AsyncRepositories.Value):
                    SelectedRepository = RepositoriesAsync.Value?.FirstOrDefault();
                    break;
                default:
                    break;
            }
        }
    }
}
