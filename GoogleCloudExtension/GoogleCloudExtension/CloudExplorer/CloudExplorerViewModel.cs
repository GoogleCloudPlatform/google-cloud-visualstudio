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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This class is the view model for the Cloud Explore tool window.
    /// </summary>
    internal class CloudExplorerViewModel : ViewModelBase
    {
        private const string RefreshImagePath = "CloudExplorer/Resources/refresh.png";

        private static readonly Lazy<ImageSource> s_refreshIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(RefreshImagePath));

        private readonly IEnumerable<ICloudExplorerSource> _sources;
        private bool _isBusy;
        private AsyncPropertyValue<string> _profilePictureAsync;
        private AsyncPropertyValue<string> _profileNameAsync;
        private Project _currentProject;
        private IEnumerable<Project> _projects;
        private Lazy<ResourceManagerDataSource> _resourceManagerDataSource;
        private Lazy<GPlusDataSource> _plusDataSource;

        /// <summary>
        /// Returns whether the view model is busy performing an operation.
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                SetValueAndRaise(ref _isBusy, value);
                RaisePropertyChanged(nameof(IsReady));
            }
        }

        /// <summary>
        /// Returns whether the view model is ready for interactions. Simplifies binding.
        /// </summary>
        public bool IsReady => !IsBusy;

        /// <summary>
        /// The list list of roots for the hieratchical view, each root contains all of the data
        /// from a given source.
        /// </summary>
        public IEnumerable<TreeHierarchy> Roots => _sources.Select(x => x.Root);

        /// <summary>
        /// Returns the profile image URL.
        /// </summary>
        public AsyncPropertyValue<string> ProfilePictureAsync
        {
            get { return _profilePictureAsync; }
            set { SetValueAndRaise(ref _profilePictureAsync, value); }
        }

        /// <summary>
        /// Returns the profile name.
        /// </summary>
        public AsyncPropertyValue<string> ProfileNameAsync
        {
            get { return _profileNameAsync; }
            set { SetValueAndRaise(ref _profileNameAsync, value); }
        }

        /// <summary>
        /// The command to show the manage accounts dialog.
        /// </summary>
        public WeakCommand ManageAccountsCommand { get; }

        /// <summary>
        /// The list of buttons to add to the toolbar, a concatenation of all sources buttons.
        /// </summary>
        public IEnumerable<ButtonDefinition> Buttons { get; }

        /// <summary>
        /// The currently selected project.
        /// </summary>
        public Project CurrentProject
        {
            get { return _currentProject; }
            set
            {
                SetValueAndRaise(ref _currentProject, value);
                CredentialsStore.Default.CurrentProjectId = value?.ProjectId;
            }
        }

        /// <summary>
        /// The list of projects for the current account.
        /// </summary>
        public IEnumerable<Project> Projects
        {
            get { return _projects; }
            set { SetValueAndRaise(ref _projects, value); }
        }

        public CloudExplorerViewModel(IEnumerable<ICloudExplorerSource> sources)
        {
            _sources = sources;
            var refreshButtonEnumerable = new ButtonDefinition[]
            {
                new ButtonDefinition
                {
                    Icon = s_refreshIcon.Value,
                    ToolTip = "Refresh",
                    Command = new WeakCommand(this.OnRefresh),
                }
            };
            Buttons = Enumerable.Concat(refreshButtonEnumerable, _sources.SelectMany(x => x.Buttons));

            ManageAccountsCommand = new WeakCommand(OnManageAccountsCommand);

            CredentialsStore.Default.CurrentAccountChanged += OnCurrentAccountChanged;
            CredentialsStore.Default.CurrentProjectIdChanged += OnCurrentProjectIdChanged;
            CredentialsStore.Default.Reset += OnReset;

            ResetCredentials();
        }

        private static GPlusDataSource CreatePlusDataSource()
        {
            var currentCredential = CredentialsStore.Default.CurrentGoogleCredential;
            return currentCredential != null ? new GPlusDataSource(currentCredential) : null;
        }

        private static ResourceManagerDataSource CreateResourceManagerDataSource()
        {
            var currentCredential = CredentialsStore.Default.CurrentGoogleCredential;
            return currentCredential != null ? new ResourceManagerDataSource(currentCredential) : null;
        }

        private void UpdateUserProfile()
        {
            if (_plusDataSource.Value != null)
            {
                var profileTask = _plusDataSource.Value.GetProfileAsync();
                ProfilePictureAsync = AsyncPropertyValueUtils.CreateAsyncProperty(profileTask, x => x.Image.Url);
                ProfileNameAsync = AsyncPropertyValueUtils.CreateAsyncProperty(
                    profileTask,
                    x => x.Emails.FirstOrDefault()?.Value,
                    "Loading...");
            }
            else
            {
                ProfilePictureAsync = null;
                ProfileNameAsync = new AsyncPropertyValue<string>("Select credentials...");
            }
        }

        private void OnManageAccountsCommand()
        {
            ExtensionAnalytics.ReportCommand(CommandName.OpenManageAccountsDialog, CommandInvocationSource.Button);

            var dialog = new ManageAccountsWindow();
            dialog.ShowModal();
        }

        private void OnCurrentAccountChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("Changing account.");
            ResetCredentials();
        }

        private void OnCurrentProjectIdChanged(object sender, EventArgs e)
        {
            if (IsBusy)
            {
                return;
            }

            Debug.WriteLine("Changing project.");
            NotifySourcesOfUpdatedAccountOrProject();
            RefreshSources();
        }

        private void OnReset(object sender, EventArgs e)
        {
            Debug.WriteLine("Resetting the credentials.");
            ResetCredentials();
        }

        private async void ResetCredentials()
        {
            try
            {
                IsBusy = true;

                // These data sources only depend on the current account, which will not change for now.
                InvalidateAccountDependentDataSources();
                UpdateUserProfile();

                // Load the projects and select the new current project. Preference is given to the current project
                // as known by CredentialsStore. If it is not a valid project then the first project in the list will
                // be used. If no project is found then null will be the value.
                var projects = await LoadProjectListAsync();
                var newCurrentProject = projects.FirstOrDefault(x => x.ProjectId == CredentialsStore.Default.CurrentProjectId);
                if (newCurrentProject == null)
                {
                    newCurrentProject = projects.FirstOrDefault();
                }

                // Set the properties in the right order. This is needed because this in turn will
                // set the properties in the list control in the right order to preserve the current
                // project.
                Projects = projects;
                CurrentProject = newCurrentProject;

                // Update the data sources as they will depend on the project being selected.
                NotifySourcesOfUpdatedAccountOrProject();
                RefreshSources();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void InvalidateAccountDependentDataSources()
        {
            _resourceManagerDataSource = new Lazy<ResourceManagerDataSource>(CreateResourceManagerDataSource);
            _plusDataSource = new Lazy<GPlusDataSource>(CreatePlusDataSource);
        }

        private async Task<IEnumerable<Project>> LoadProjectListAsync()
        {
            if (_resourceManagerDataSource.Value != null)
            {
                var result = await _resourceManagerDataSource.Value.GetProjectsListAsync();
                return result.OrderBy(x => x.Name);
            }
            else
            {
                // TODO: Return a dummy project that shows no project was found.
                return Enumerable.Empty<Project>();
            }
        }

        private void OnRefresh()
        {
            ExtensionAnalytics.ReportCommand(CommandName.RefreshDataSource, CommandInvocationSource.Button);

            RefreshSources();
        }

        private void RefreshSources()
        {
            foreach (var source in _sources)
            {
                source.Refresh();
            }
            RaisePropertyChanged(nameof(Roots));
        }

        /// <summary>
        /// Notifies all of the explorer sources that there are new credentials, be it a new
        /// project selected, or a new user selected.
        /// </summary>
        private void NotifySourcesOfUpdatedAccountOrProject()
        {
            foreach (var source in _sources)
            {
                source.InvalidateProjectOrAccount();
            }
        }
    }
}
