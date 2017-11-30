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
using GoogleCloudExtension.CloudExplorerSources.Gae;
using GoogleCloudExtension.CloudExplorerSources.Gce;
using GoogleCloudExtension.CloudExplorerSources.Gcs;
using GoogleCloudExtension.CloudExplorerSources.PubSub;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.PickProjectDialog;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
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
        private AsyncProperty<string> _profilePictureAsync;
        private AsyncProperty<string> _profileNameAsync;
        private Project _currentProject;
        private Lazy<ResourceManagerDataSource> _resourceManagerDataSource;
        private Lazy<GPlusDataSource> _plusDataSource;
        private string _emptyStateMessage;
        private string _emptyStateButtonCaption;
        private ICommand _emptyStateCommand;
        private bool _loadingProject;
        private string _projectDisplayString;
        private IList<TreeHierarchy> _roots;

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
        public bool IsEmptyState => IsReady && (CredentialsStore.Default.CurrentAccount == null || _currentProject == null);

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
        public IList<TreeHierarchy> Roots
        {
            get { return _roots; }
            private set { SetValueAndRaise(ref _roots, value); }
        }

        public string ProjectDisplayString
        {
            get { return _projectDisplayString; }
            private set { SetValueAndRaise(ref _projectDisplayString, value); }
        }

        public bool LoadingProject
        {
            get { return _loadingProject; }
            private set { SetValueAndRaise(ref _loadingProject, value); }
        }

        /// <summary>
        /// Returns the profile image URL.
        /// </summary>
        public AsyncProperty<string> ProfilePictureAsync
        {
            get { return _profilePictureAsync; }
            private set { SetValueAndRaise(ref _profilePictureAsync, value); }
        }

        /// <summary>
        /// Returns the profile name.
        /// </summary>
        public AsyncProperty<string> ProfileNameAsync
        {
            get { return _profileNameAsync; }
            private set { SetValueAndRaise(ref _profileNameAsync, value); }
        }

        /// <summary>
        /// The list of buttons to add to the toolbar, a concatenation of all sources buttons.
        /// </summary>
        public IEnumerable<ButtonDefinition> Buttons { get; }

        /// <summary>
        /// Message to show when there's no data to show in the cloud explorer.
        /// </summary>
        public string EmptyStateMessage
        {
            get { return _emptyStateMessage; }
            set { SetValueAndRaise(ref _emptyStateMessage, value); }
        }

        /// <summary>
        /// Caption for the empty state button.
        /// </summary>
        public string EmptyStateButtonCaption
        {
            get { return _emptyStateButtonCaption; }
            set { SetValueAndRaise(ref _emptyStateButtonCaption, value); }
        }

        /// <summary>
        /// Command to execute when the user clicks on the emtpy state button.
        /// </summary>
        public ICommand EmptyStateCommand
        {
            get { return _emptyStateCommand; }
            set { SetValueAndRaise(ref _emptyStateCommand, value); }
        }

        /// <summary>
        /// The command to show the manage accounts dialog.
        /// </summary>
        public ICommand ManageAccountsCommand { get; }

        /// <summary>
        /// The command to execute when a user double clicks on an item.
        /// </summary>
        public ICommand DoubleClickCommand { get; }

        /// <summary>
        /// The command to execute to select a new GCP project.
        /// </summary>
        public ICommand SelectProjectCommand { get; }

        #region ICloudSourceContext implementation.

        Project ICloudSourceContext.CurrentProject => _currentProject;

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
                // The Google App Engine source.
                new GaeSource(this),

                // The Google Compute Engine source.
                new GceSource(this),

                // The Google Cloud Storage source.
                new GcsSource(this),

                // The Google Cloud SQL source.
                new CloudSQLSource(this),

                // The Google Publish/Subscription source.
                new PubsubSource(this),

                // The source to navigate to the cloud console.
                new CloudConsoleSource(),
            };

            var refreshButtonEnumerable = new[]
            {
                new ButtonDefinition
                {
                    Icon = s_refreshIcon.Value,
                    ToolTip = Resources.CloudExplorerRefreshButtonToolTip,
                    Command = new ProtectedCommand(OnRefreshCommand),
                }
            };
            Buttons = refreshButtonEnumerable.Concat(_sources.SelectMany(x => x.Buttons));

            ManageAccountsCommand = new ProtectedCommand(OnManageAccountsCommand);
            DoubleClickCommand = new ProtectedCommand<IAcceptInput>(OnDoubleClickCommand);
            SelectProjectCommand = new ProtectedCommand(OnSelectProjectCommand);

            CredentialsStore.Default.CurrentAccountChanged += OnCredentialsChanged;
            CredentialsStore.Default.CurrentProjectIdChanged += OnCredentialsChanged;
            CredentialsStore.Default.Reset += OnCredentialsChanged;

            ErrorHandlerUtils.HandleAsyncExceptions(ResetCredentialsAsync);
        }

        private static GPlusDataSource CreatePlusDataSource()
        {
            var currentCredential = CredentialsStore.Default.CurrentGoogleCredential;
            return currentCredential != null ? new GPlusDataSource(currentCredential, GoogleCloudExtensionPackage.VersionedApplicationName) : null;
        }

        private void UpdateUserProfile()
        {
            if (_plusDataSource.Value != null)
            {
                var profileTask = _plusDataSource.Value.GetProfileAsync();
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

        #region Command handlers

        private void OnDoubleClickCommand(IAcceptInput obj)
        {
            obj.OnDoubleClick();
        }

        private void OnManageAccountsCommand()
        {
            ManageAccountsWindow.PromptUser();
        }

        private void OnSelectProjectCommand()
        {
            Project selectedProject = PickProjectIdWindow.PromptUser(allowAccountChange: false);
            if (selectedProject == null)
            {
                return;
            }

            CredentialsStore.Default.UpdateCurrentProject(selectedProject);
        }

        private void OnNavigateToCloudConsoleCommand()
        {
            Process.Start("https://console.cloud.google.com/");
        }

        private void OnRefreshCommand()
        {
            ErrorHandlerUtils.HandleAsyncExceptions(ResetCredentialsAsync);
        }

        #endregion

        #region Event handlers

        private void OnCredentialsChanged(object sender, EventArgs e)
        {
            ErrorHandlerUtils.HandleAsyncExceptions(async () =>
            {
                Debug.WriteLine("Resetting the credentials.");
                await ResetCredentialsAsync();
            });
        }

        #endregion

        private async Task<Project> GetProjectForIdAsync(string projectId)
            => projectId != null ? await _resourceManagerDataSource.Value.GetProjectAsync(projectId) : null;

        private async Task ResetCredentialsAsync()
        {
            try
            {
                IsBusy = true;

                // These data sources only depend on the current account, which will not change for now.
                InvalidateAccountDependentDataSources();
                UpdateUserProfile();

                // Load the current project if one is found, otherwise ask the user to choose a project.
                await LoadCurrentProject();

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
            // Catch all, otherwise it terminates Visual Studio
            catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
            {
                Debug.WriteLine($"Exception at CloudExplorerViewModel.ResetCredentials. {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadCurrentProject()
        {
            // Avoid reentrancy.
            if (LoadingProject)
            {
                return;
            }

            try
            {
                try
                {
                    // Start the loading project process.
                    LoadingProject = true;

                    // Try to load the project.
                    _currentProject = await GetProjectForIdAsync(CredentialsStore.Default.CurrentProjectId);
                }
                catch (Exception ex) when (!ErrorHandlerUtils.IsCriticalException(ex))
                {
                    Debug.WriteLine($"Failed to load project: {ex.Message}");

                    _currentProject = null;
                }

                // If we managed to load the project, set the display string for it.
                ProjectDisplayString = _currentProject?.Name;
            }
            finally
            {
                LoadingProject = false;
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
            else if (_currentProject == null)
            {
                EmptyStateMessage = Resources.CloudExploreNoProjectMessage;
                EmptyStateButtonCaption = Resources.CloudExplorerNoProjectButtonCaption;
                EmptyStateCommand = new ProtectedCommand(OnNavigateToCloudConsoleCommand);
            }
        }

        private void InvalidateAccountDependentDataSources()
        {
            _resourceManagerDataSource = new Lazy<ResourceManagerDataSource>(DataSourceFactories.CreateResourceManagerDataSource);
            _plusDataSource = new Lazy<GPlusDataSource>(CreatePlusDataSource);
        }

        private async void RefreshSources()
        {
            // Clear the roots collection to clean the UI.
            Roots = null;
            foreach (var source in _sources)
            {
                source.Refresh();
            }

            // Wait for a full cycle so the automation server has time to adapt to the chagnes in the UI.
            await Task.Delay(100);
            Roots = _sources.Select(x => x.Root).ToList();
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
