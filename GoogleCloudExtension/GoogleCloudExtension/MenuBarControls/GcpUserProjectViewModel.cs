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
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.MenuBarControls
{
    [Export(typeof(IGcpUserProjectViewModel))]
    public class GcpUserProjectViewModel : ViewModelBase, IGcpUserProjectViewModel
    {
        private readonly Lazy<IUserPromptService> _userPromptService;
        private AsyncProperty<string> _profilePictureUrlAsync;
        private AsyncProperty<string> _profileNameAsync;
        private AsyncProperty<Project> _currentProject;
        private bool _isPopupOpen;
        private AsyncProperty<string> _profileEmailAsyc;

        public AsyncProperty<Project> CurrentProjectAsync
        {
            get => _currentProject;
            private set => SetValueAndRaise(ref _currentProject, value);
        }

        /// <summary>
        /// Returns the profile image URL.
        /// </summary>
        public AsyncProperty<string> ProfilePictureUrlAsync
        {
            get => _profilePictureUrlAsync;
            private set => SetValueAndRaise(ref _profilePictureUrlAsync, value);
        }

        /// <summary>
        /// Returns the profile name.
        /// </summary>
        public AsyncProperty<string> ProfileNameAsync
        {
            get => _profileNameAsync;
            private set => SetValueAndRaise(ref _profileNameAsync, value);
        }

        public AsyncProperty<string> ProfileEmailAsyc
        {
            get => _profileEmailAsyc;
            private set => SetValueAndRaise(ref _profileEmailAsyc, value);
        }

        /// <summary>
        /// Setting this to true opens the GCP Menu Bar Popup.
        /// </summary>
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set => SetValueAndRaise(ref _isPopupOpen, value);
        }

        /// <summary>
        /// The command to show the manage accounts dialog.
        /// </summary>
        public IProtectedCommand ManageAccountsCommand { get; }

        /// <summary>
        /// The command to execute to select a new GCP project.
        /// </summary>
        public IProtectedCommand SelectProjectCommand { get; }

        /// <summary>
        /// The command to open the GCP Menu Bar Popup.
        /// </summary>
        public ICommand OpenPopup { get; }

        private IGPlusDataSource GPlusDataSource => DataSourceFactory.GPlusDataSource;
        private IDataSourceFactory DataSourceFactory { get; }
        private ICredentialsStore CredentialsStore { get; }
        private IUserPromptService UserPromptService => _userPromptService.Value;

        [ImportingConstructor]
        public GcpUserProjectViewModel(
            IDataSourceFactory dataSourceFactory,
            ICredentialsStore credentialsStore,
            Lazy<IUserPromptService> userPromptService)
        {
            DataSourceFactory = dataSourceFactory;
            CredentialsStore = credentialsStore;
            _userPromptService = userPromptService;

            OpenPopup = new ProtectedCommand(() => IsPopupOpen = true);
            ManageAccountsCommand =
                new ProtectedCommand(() => UserPromptService.PromptUser(new ManageAccountsWindowContent()));
            SelectProjectCommand = new ProtectedCommand(SelectProject);

            CurrentProjectAsync = new AsyncProperty<Project>(GetCurrentProjectAsync());
            UpdateUserProfile();

            CredentialsStore.CurrentProjectIdChanged += (sender, args) => LoadCurrentProject();
            DataSourceFactory.DataSourcesUpdated += (sender, args) => UpdateUserProfile();
        }

        public void UpdateUserProfile()
        {
            if (GPlusDataSource != null)
            {
                Task<Person> profileTask = GPlusDataSource.GetProfileAsync();
                ProfilePictureUrlAsync = AsyncProperty.Create(
                    profileTask,
                    x => x?.Image?.Url);
                ProfileNameAsync = AsyncProperty.Create(
                    profileTask,
                    x => x?.DisplayName,
                    Resources.UiLoadingMessage);
                ProfileEmailAsyc = AsyncProperty.Create(
                    profileTask,
                    p => p.Emails.FirstOrDefault()?.Value);
            }
            else
            {
                ProfilePictureUrlAsync = new AsyncProperty<string>(null);
                ProfileNameAsync = new AsyncProperty<string>(null);
                ProfileEmailAsyc = new AsyncProperty<string>(null);
            }
        }

        public void LoadCurrentProject()
        {
            Project currentProject;
            if (CurrentProjectAsync.Value?.ProjectId == CredentialsStore.CurrentProjectId)
            {
                currentProject = CurrentProjectAsync.Value;
            }
            else
            {
                currentProject = null;
            }

            CurrentProjectAsync = new AsyncProperty<Project>(GetCurrentProjectAsync(), currentProject);
        }

        private async Task<Project> GetCurrentProjectAsync()
        {
            string currentProjectId = CredentialsStore.CurrentProjectId;
            if (currentProjectId != null)
            {
                return await DataSourceFactory.ResourceManagerDataSource.GetProjectAsync(currentProjectId);
            }
            else
            {
                return null;
            }
        }

        private void SelectProject()
        {
            Project selectedProject = UserPromptService.UserPromptResult(
                new PickProjectIdWindowContent(Resources.CloudExplorerPickProjectHelpMessage, false));
            if (selectedProject != null)
            {
                CurrentProjectAsync = new AsyncProperty<Project>(selectedProject);
                CredentialsStore.UpdateCurrentProject(selectedProject);
            }
        }
    }
}