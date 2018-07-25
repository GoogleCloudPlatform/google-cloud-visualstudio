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
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtension.MenuBarControls
{
    [Export(typeof(IGcpUserProjectViewModel))]
    public class GcpUserProjectViewModel : ViewModelBase, IGcpUserProjectViewModel
    {
        private AsyncProperty<BitmapImage> _profilePictureAsync;
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
        public AsyncProperty<BitmapImage> ProfilePictureAsync
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
        public ProtectedCommand ManageAccountsCommand { get; }

        /// <summary>
        /// The command to execute to select a new GCP project.
        /// </summary>
        public ProtectedCommand SelectProjectCommand { get; }

        /// <summary>
        /// The command to open the GCP Menu Bar Popup.
        /// </summary>
        public ICommand OpenPopup { get; }

        private IGPlusDataSource GPlusDataSource => DataSourceFactory.GPlusDataSource;
        private IDataSourceFactory DataSourceFactory { get; }
        private ICredentialsStore CredentialsStore { get; }

        [ImportingConstructor]
        public GcpUserProjectViewModel(IDataSourceFactory dataSourceFactory, ICredentialsStore credentialsStore)
        {
            DataSourceFactory = dataSourceFactory;
            CredentialsStore = credentialsStore;

            CredentialsStore.CurrentProjectIdChanged += (sender, args) => LoadCurrentProject();
            DataSourceFactory.DataSourcesUpdated += (sender, args) => UpdateUserProfile();
            LoadCurrentProject();

            OpenPopup = new ProtectedCommand(() => IsPopupOpen = true);
            ManageAccountsCommand = new ProtectedCommand(ManageAccountsWindow.PromptUser);
            SelectProjectCommand = new ProtectedCommand(OnSelectProjectCommand);
        }

        public void UpdateUserProfile()
        {
            if (GPlusDataSource != null)
            {
                Task<Person> profileTask = GPlusDataSource.GetProfileAsync();
                ProfilePictureAsync = AsyncPropertyUtils.CreateAsyncProperty(
                    profileTask,
                    x => x != null ? new BitmapImage(new Uri(x.Image.Url)) : null);
                ProfileNameAsync = AsyncPropertyUtils.CreateAsyncProperty(
                    profileTask,
                    x => x?.DisplayName,
                    Resources.UiLoadingMessage);
                ProfileEmailAsyc = AsyncPropertyUtils.CreateAsyncProperty(
                    profileTask,
                    p => p.Emails.FirstOrDefault()?.Value);
            }
            else
            {
                ProfilePictureAsync = null;
                ProfileNameAsync = null;
                ProfileEmailAsyc = null;
            }
        }

        public void LoadCurrentProject()
        {
            Project currentProject;
            if (CurrentProjectAsync?.Value?.ProjectId == CredentialsStore.CurrentProjectId)
            {
                currentProject = CurrentProjectAsync?.Value;
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

        private void OnSelectProjectCommand()
        {
            Project selectedProject = PickProjectIdWindow.PromptUser(
                Resources.CloudExplorerPickProjectHelpMessage,
                false);
            if (selectedProject != null)
            {
                CurrentProjectAsync = new AsyncProperty<Project>(selectedProject);
                CredentialsStore.UpdateCurrentProject(selectedProject);
            }
        }
    }
}