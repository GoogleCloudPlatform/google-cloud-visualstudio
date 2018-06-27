// Copyright 2018 Google Inc. All Rights Reserved.
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
using Google.Apis.Plus.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.PickProjectDialog;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.MenuBarControls
{
    [Export(typeof(IGcpUserProjectViewModel))]
    public class GcpUserProjectViewModel : ViewModelBase, IGcpUserProjectViewModel
    {
        private AsyncProperty<string> _profilePictureAsync;
        private AsyncProperty<string> _profileNameAsync;
        private string _projectDisplayString;
        private AsyncProperty<Project> _currentProject;
        private readonly Lazy<IDataSourceFactory> _dataSourceFactory;

        public AsyncProperty<Project> CurrentProjectAsync
        {
            get => _currentProject;
            private set => SetValueAndRaise(ref _currentProject, value);
        }

        /// <summary>
        /// The user ready string for the project.
        /// </summary>
        public string ProjectDisplayString
        {
            get => _projectDisplayString;
            private set => SetValueAndRaise(ref _projectDisplayString, value);
        }

        /// <summary>
        /// Returns the profile image URL.
        /// </summary>
        public AsyncProperty<string> ProfilePictureAsync
        {
            get => _profilePictureAsync;
            private set => SetValueAndRaise(ref _profilePictureAsync, value);
        }

        /// <summary>
        /// Returns the profile name.
        /// </summary>
        public AsyncProperty<string> ProfileNameAsync
        {
            get => _profileNameAsync;
            private set => SetValueAndRaise(ref _profileNameAsync, value);
        }

        /// <summary>
        /// The command to show the manage accounts dialog.
        /// </summary>
        public ProtectedCommand ManageAccountsCommand { get; }

        /// <summary>
        /// The command to execute to select a new GCP project.
        /// </summary>
        public ProtectedCommand SelectProjectCommand { get; }

        private IGPlusDataSource GPlusDataSource => DataSourceFactory.GPlusDataSource;
        private IDataSourceFactory DataSourceFactory => _dataSourceFactory.Value;
        private ICredentialsStore CredentialsStore { get; }

        [ImportingConstructor]
        public GcpUserProjectViewModel(Lazy<IDataSourceFactory> dataSourceFactory, ICredentialsStore credentialsStore)
        {
            _dataSourceFactory = dataSourceFactory;
            CredentialsStore = credentialsStore;

            CredentialsStore.CurrentAccountChanged += (sender, args) => UpdateUserProfile();
            CredentialsStore.CurrentProjectIdChanged += (sender, args) => LoadCurrentProject();

            ManageAccountsCommand = new ProtectedCommand(ManageAccountsWindow.PromptUser);
            SelectProjectCommand = new ProtectedCommand(OnSelectProjectCommand);
        }

        public void UpdateUserProfile()
        {
            if (GPlusDataSource != null)
            {
                Task<Person> profileTask = GPlusDataSource.GetProfileAsync();
                ProfilePictureAsync = AsyncPropertyUtils.CreateAsyncProperty(profileTask, x => x?.Image.Url);
                ProfileNameAsync = AsyncPropertyUtils.CreateAsyncProperty(
                    profileTask,
                    x => x?.Emails.FirstOrDefault()?.Value,
                    Resources.CloudExplorerLoadingMessage);
            }
            else
            {
                ProfilePictureAsync = null;
                ProfileNameAsync = new AsyncProperty<string>(Resources.CloudExplorerSelectAccountMessage);
            }
        }

        public void LoadCurrentProject()
        {
            if (!CurrentProjectAsync.IsPending)
            {
                CurrentProjectAsync = new AsyncProperty<Project>(GetCurrentProjectAsync());
            }
        }

        private async Task<Project> GetCurrentProjectAsync()
        {
            string currentProjectId = CredentialsStore.CurrentProjectId;
            if (currentProjectId != null)
            {
                Project project = await DataSourceFactory.ResourceManagerDataSource.GetProjectAsync(currentProjectId);

                ProjectDisplayString = project?.Name;
                return project;
            }
            else
            {
                return null;
            }
        }

        private void OnSelectProjectCommand()
        {
            Project selectedProject = PickProjectIdWindow.PromptUser(
                Resources.CloudExplorerPickProjectHelpMessage,
                false);
            if (selectedProject != null)
            {
                CredentialsStore.UpdateCurrentProject(selectedProject);
            }
        }
    }
}