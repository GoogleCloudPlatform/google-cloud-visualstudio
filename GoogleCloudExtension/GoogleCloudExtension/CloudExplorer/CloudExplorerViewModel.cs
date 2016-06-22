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
    public class CloudExplorerViewModel : ViewModelBase
    {
        private const string RefreshImagePath = "CloudExplorer/Resources/refresh.png";

        // Message and caption for the empty state when there's no account.
        private const string NoAccountMessage = "Add a new account, or select and existing one.";
        private const string NoAccountButtonCaption = "Select or Create Account";

        // Message and caption for the emtpy state when there's no projects for the account.
        private const string NoProjectMessage = "No projects found, create a project on the Cloud Console.";
        private const string NoProjectButtonCaption = "Navigate to Cloud Console";

        private static readonly Lazy<ImageSource> s_refreshIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(RefreshImagePath));
        private static readonly PlaceholderMessage s_projectPlaceholder = new PlaceholderMessage { Message = "No projects found" };
        private static readonly IList<PlaceholderMessage> s_projectsWithPlaceholder = new List<PlaceholderMessage> { s_projectPlaceholder };

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
        public IEnumerable<TreeHierarchy> Roots =>
            Enumerable.Concat<TreeHierarchy>(_sources.Select(x => x.Root), new[] { new CloudConsoleNode() });

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
                    CredentialsStore.Default.CurrentProjectId = project?.ProjectId;
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
            return currentCredential != null ? new GPlusDataSource(currentCredential, GoogleCloudExtensionPackage.ApplicationName) : null;
        }

        private static ResourceManagerDataSource CreateResourceManagerDataSource()
        {
            var currentCredential = CredentialsStore.Default.CurrentGoogleCredential;
            return currentCredential != null ? new ResourceManagerDataSource(currentCredential, GoogleCloudExtensionPackage.ApplicationName) : null;
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

        #region Command handlers

        private void OnManageAccountsCommand()
        {
            ExtensionAnalytics.ReportCommand(CommandName.OpenManageAccountsDialog, CommandInvocationSource.Button);

            ManageAccountsWindow.PromptUser();
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

        private void OnNavigateToCloudConsole()
        {
            Process.Start("https://console.cloud.google.com/");
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
                EmptyStateMessage = NoAccountMessage;
                EmptyStateButtonCaption = NoAccountButtonCaption;
                EmptyStateCommand = ManageAccountsCommand;
            }
            else if (_projects == null || _projects.Count == 0)
            {
                EmptyStateMessage = NoProjectMessage;
                EmptyStateButtonCaption = NoProjectButtonCaption;
                EmptyStateCommand = new WeakCommand(OnNavigateToCloudConsole);
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
