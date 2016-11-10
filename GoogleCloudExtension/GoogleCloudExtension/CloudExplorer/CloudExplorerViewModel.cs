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
using GoogleCloudExtension.CloudExplorerSources.CloudSQL;
using GoogleCloudExtension.CloudExplorerSources.Gce;
using GoogleCloudExtension.CloudExplorerSources.Gcs;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This class is the view model for the Cloud Explore tool window.
    /// </summary>
    public class CloudExplorerViewModel : ViewModelBase, ICloudSourceContext
    {
        private const string RefreshImagePath = "CloudExplorer/Resources/refresh.png";

        private static readonly Lazy<ImageSource> s_refreshIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(RefreshImagePath));
        private static readonly PlaceholderMessage s_projectPlaceholder = new PlaceholderMessage { Message = Resources.CloudExplorerNoProjectsFoundPlaceholderMessage };
        private static readonly IList<PlaceholderMessage> s_projectsWithPlaceholder = new List<PlaceholderMessage> { s_projectPlaceholder };

        private readonly SelectionUtils _selectionUtils;
        private readonly IEnumerable<ICloudExplorerSource> _sources;
        private bool _isBusy;
        private AsyncPropertyValue<string> _profilePictureAsync;
        private AsyncPropertyValue<string> _profileNameAsync;
        private object _currentProject;
        private IList<Project> _projects;
        private Lazy<ResourceManagerDataSource> _resourceManagerDataSource;
        private Lazy<GPlusDataSource> _plusDataSource;
        private string _emptyStateMessage;
        private string _emptyStateButtonCaption;
        private ICommand _emptyStateCommand;

        /// <summary>
        /// Returns whether the view model is busy performing an operation.
        /// </summary>
        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                SetValueAndRaise(ref _isBusy, value);
                RaisePropertyChanged(nameof(IsReady));
                RaisePropertyChanged(nameof(IsEmptyState));
            }
        }

        /// <summary>
        /// Stores whether the cloud explorer is in zero state.
        /// </summary>
        public bool IsEmptyState => IsReady && (CredentialsStore.Default.CurrentAccount == null || (_projects?.Count ?? 0) == 0);

        /// <summary>
        /// The negation of IsEmptyState.
        /// </summary>
        public bool IsNotEmptyState => !IsEmptyState;

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
            private set { SetValueAndRaise(ref _profilePictureAsync, value); }
        }

        /// <summary>
        /// Returns the profile name.
        /// </summary>
        public AsyncPropertyValue<string> ProfileNameAsync
        {
            get { return _profileNameAsync; }
            private set { SetValueAndRaise(ref _profileNameAsync, value); }
        }

        /// <summary>
        /// The command to show the manage accounts dialog.
        /// </summary>
        public ICommand ManageAccountsCommand { get; }

        /// <summary>
        /// The list of buttons to add to the toolbar, a concatenation of all sources buttons.
        /// </summary>
        public IEnumerable<ButtonDefinition> Buttons { get; }

        /// <summary>
        /// The currently selected project.
        /// </summary>
        public object CurrentProject
        {
            get { return _currentProject; }
            set
            {
                SetValueAndRaise(ref _currentProject, value);
                if (value == null || value is Project)
                {
                    var project = (Project)value;
                    CredentialsStore.Default.UpdateCurrentProject(project);
                }
            }
        }

        /// <summary>
        /// The list of projects for the current account.
        /// </summary>
        public IEnumerable EffectiveProjects
        {
            get
            {
                if (_projects == null || _projects.Count == 0)
                {
                    return s_projectsWithPlaceholder;
                }
                else
                {
                    return _projects;
                }
            }
        }

        public string EmptyStateMessage
        {
            get { return _emptyStateMessage; }
            set { SetValueAndRaise(ref _emptyStateMessage, value); }
        }

        public string EmptyStateButtonCaption
        {
            get { return _emptyStateButtonCaption; }
            set { SetValueAndRaise(ref _emptyStateButtonCaption, value); }
        }

        public ICommand EmptyStateCommand
        {
            get { return _emptyStateCommand; }
            set { SetValueAndRaise(ref _emptyStateCommand, value); }
        }

        #region ICloudSourceContext implementation.

        Project ICloudSourceContext.CurrentProject => _currentProject as Project;

        IEnumerable<Project> ICloudSourceContext.Projects => _projects;

        void ICloudSourceContext.ShowPropertiesWindow(object item)
        {
            _selectionUtils.ActivatePropertiesWindow();
            _selectionUtils.SelectItem(item);
        }

        #endregion

        public CloudExplorerViewModel(SelectionUtils selectionUtils)
        {
            _selectionUtils = selectionUtils;

            // Contains the list of sources to display to the user, in the order they will
            // be displayed.

            _sources = new List<ICloudExplorerSource>
            {
                // The Google Compute Engine source.
                new GceSource(this),

                // The Google Cloud Storage source.
                new GcsSource(this),

                // The Google Cloud SQL source.
                new CloudSQLSource(this),

                // The source to navigate to the cloud console.
                new CloudConsoleSource(),
            };

            var refreshButtonEnumerable = new ButtonDefinition[]
            {
                new ButtonDefinition
                {
                    Icon = s_refreshIcon.Value,
                    ToolTip = Resources.CloudExplorerRefreshButtonToolTip,
                    Command = new ProtectedCommand(OnRefreshCommand),
                }
            };
            Buttons = Enumerable.Concat(refreshButtonEnumerable, _sources.SelectMany(x => x.Buttons));

            ManageAccountsCommand = new ProtectedCommand(OnManageAccountsCommand);

            CredentialsStore.Default.CurrentAccountChanged += OnCurrentAccountChanged;
            CredentialsStore.Default.CurrentProjectIdChanged += OnCurrentProjectIdChanged;
            CredentialsStore.Default.Reset += OnReset;

            ResetCredentials();
        }

        private static GPlusDataSource CreatePlusDataSource()
        {
            var currentCredential = CredentialsStore.Default.CurrentGoogleCredential;
            return currentCredential != null ? new GPlusDataSource(currentCredential, GoogleCloudExtensionPackage.VersionedApplicationName) : null;
        }

        private static ResourceManagerDataSource CreateResourceManagerDataSource()
        {
            var currentCredential = CredentialsStore.Default.CurrentGoogleCredential;
            return currentCredential != null ? new ResourceManagerDataSource(currentCredential, GoogleCloudExtensionPackage.VersionedApplicationName) : null;
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
                    Resources.CloudExplorerLoadingMessage);
            }
            else
            {
                ProfilePictureAsync = null;
                ProfileNameAsync = new AsyncPropertyValue<string>(Resources.CloudExplorerSelectAccountMessage);
            }
        }

        #region Command handlers

        private void OnManageAccountsCommand()
        {
            ManageAccountsWindow.PromptUser();
        }

        private void OnCurrentProjectIdChanged(object sender, EventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                if (IsBusy)
                {
                    return;
                }

                Debug.WriteLine("Changing project.");
                NotifySourcesOfUpdatedAccountOrProject();
                RefreshSources();
            });
        }

        private void OnNavigateToCloudConsoleCommand()
        {
            Process.Start("https://console.cloud.google.com/");
        }

        private void OnRefreshCommand()
        {
            ResetCredentials();
        }

        #endregion

        #region Event handlers

        private void OnReset(object sender, EventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                Debug.WriteLine("Resetting the credentials.");
                ResetCredentials();
            });
        }

        private void OnCurrentAccountChanged(object sender, EventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                Debug.WriteLine("Changing account.");
                ResetCredentials();
            });
        }

        #endregion

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

                if (projects.Count == 0)
                {
                    UpdateProjects(null);
                    CurrentProject = s_projectPlaceholder;
                }
                else
                {
                    var newCurrentProject = projects.FirstOrDefault(x => x.ProjectId == CredentialsStore.Default.CurrentProjectId);
                    if (newCurrentProject == null)
                    {
                        newCurrentProject = projects.FirstOrDefault();
                    }

                    // Set the properties in the right order. This is needed because this in turn will
                    // set the properties in the list control in the right order to preserve the current
                    // project.
                    UpdateProjects(projects);
                    CurrentProject = newCurrentProject;
                }

                // Update the data sources as they will depend on the project being selected.
                NotifySourcesOfUpdatedAccountOrProject();
                RefreshSources();

                // Notify of changes of the empty state.
                InvalidateEmptyState();

                // Update the enabled state of the buttons, to match the empty state.
                foreach (var button in Buttons)
                {
                    button.IsEnabled = IsNotEmptyState;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void InvalidateEmptyState()
        {
            RaisePropertyChanged(nameof(IsEmptyState));
            RaisePropertyChanged(nameof(IsNotEmptyState));

            // Prepare the message and button for the empty state.
            if (CredentialsStore.Default.CurrentAccount == null)
            {
                EmptyStateMessage = Resources.CloudExplorerNoAccountMessage;
                EmptyStateButtonCaption = Resources.CloudExplorerNoAccountButtonCaption;
                EmptyStateCommand = ManageAccountsCommand;
            }
            else if (_projects == null || _projects.Count == 0)
            {
                EmptyStateMessage = Resources.CloudExploreNoProjectMessage;
                EmptyStateButtonCaption = Resources.CloudExplorerNoProjectButtonCaption;
                EmptyStateCommand = new ProtectedCommand(OnNavigateToCloudConsoleCommand);
            }
        }

        private void UpdateProjects(IList<Project> projects)
        {
            _projects = projects;
            RaisePropertyChanged(nameof(EffectiveProjects));
            InvalidateEmptyState();
        }

        private void InvalidateAccountDependentDataSources()
        {
            _resourceManagerDataSource = new Lazy<ResourceManagerDataSource>(CreateResourceManagerDataSource);
            _plusDataSource = new Lazy<GPlusDataSource>(CreatePlusDataSource);
        }

        private async Task<IList<Project>> LoadProjectListAsync()
        {
            if (_resourceManagerDataSource.Value != null)
            {
                var result = await _resourceManagerDataSource.Value.GetProjectsListAsync();
                return result.Where(x => x.LifecycleState == "ACTIVE").OrderBy(x => x.Name).ToList();
            }
            else
            {
                return new List<Project>();
            }
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
