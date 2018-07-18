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
using GoogleCloudExtension.CloudExplorerSources.CloudConsoleLinks;
using GoogleCloudExtension.CloudExplorerSources.Gae;
using GoogleCloudExtension.CloudExplorerSources.Gce;
using GoogleCloudExtension.MenuBarControls;
using GoogleCloudExtension.Utils;
using System;
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

        internal static readonly Lazy<ImageSource> s_refreshIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(RefreshImagePath));

        private readonly ISelectionUtils _selectionUtils;
        private readonly IEnumerable<ICloudExplorerSource<ISourceRootViewModelBase>> _sources;
        private bool _isBusy;
        private string _emptyStateMessage;
        private string _emptyStateButtonCaption;
        private ICommand _emptyStateCommand;
        private IList<ISourceRootViewModelBase> _roots;

        /// <summary>
        /// Returns whether the view model is busy performing an operation.
        /// </summary>
        private bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetValueAndRaise(ref _isBusy, value);
                RaisePropertyChanged(nameof(IsReady));
                RaisePropertyChanged(nameof(IsEmptyState));
            }
        }

        /// <summary>
        /// Stores whether the cloud explorer is in zero state.
        /// </summary>
        public bool IsEmptyState => IsReady &&
            (CredentialsStore.Default.CurrentAccount == null || UserProject.CurrentProjectAsync?.Value?.Name == null);

        /// <summary>
        /// The negation of IsEmptyState.
        /// </summary>
        private bool IsNotEmptyState => !IsEmptyState;

        /// <summary>
        /// Returns whether the view model is ready for interactions. Simplifies binding.
        /// </summary>
        public bool IsReady => !IsBusy;

        /// <summary>
        /// The list list of roots for the hieratchical view, each root contains all of the data
        /// from a given source.
        /// </summary>
        public IList<ISourceRootViewModelBase> Roots
        {
            get { return _roots; }
            private set { SetValueAndRaise(ref _roots, value); }
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

        public IGcpUserProjectViewModel UserProject { get; }

        /// <summary>
        /// Command to execute when the user clicks on the emtpy state button.
        /// </summary>
        public ICommand EmptyStateCommand
        {
            get { return _emptyStateCommand; }
            set { SetValueAndRaise(ref _emptyStateCommand, value); }
        }

        /// <summary>
        /// The command to execute when a user double clicks on an item.
        /// </summary>
        public ProtectedCommand<IAcceptInput> DoubleClickCommand { get; }

        /// <summary>
        /// The command to execute to select a new GCP project.
        /// </summary>
        public ProtectedCommand SelectProjectCommand => UserProject.SelectProjectCommand;

        /// <summary>
        /// The command executed by the refresh button.
        /// </summary>
        internal ProtectedAsyncCommand RefreshCommand { get; }

        #region ICloudSourceContext implementation.

        Project ICloudSourceContext.CurrentProject => UserProject.CurrentProjectAsync.Value;

        void ICloudSourceContext.ShowPropertiesWindow(object item)
        {
            _selectionUtils.ActivatePropertiesWindow();
            _selectionUtils.SelectItem(item);
        }

        #endregion

        public CloudExplorerViewModel(ISelectionUtils selectionUtils)
        {
            _selectionUtils = selectionUtils;
            UserProject = GoogleCloudExtensionPackage.Instance.GetMefService<IGcpUserProjectViewModel>();

            RefreshCommand = new ProtectedAsyncCommand(OnRefreshCommandAsync);
            DoubleClickCommand = new ProtectedCommand<IAcceptInput>(OnDoubleClickCommand);

            // Contains the list of sources to display to the user, in the order they will
            // be displayed.

            _sources = new List<ICloudExplorerSource<ISourceRootViewModelBase>>
            {
                // The Google App Engine source.
                new GaeSource(this),

                // The Google Compute Engine source.
                new GceSource(this),

                // The source to navigate to the cloud console.
                new CloudConsoleLinksSource(this)
            };

            Buttons = new[]
            {
                new ButtonDefinition
                {
                    Icon = s_refreshIcon.Value,
                    ToolTip = Resources.CloudExplorerRefreshButtonToolTip,
                    Command = RefreshCommand
                }
            };

            CredentialsStore.Default.CurrentAccountChanged += OnCredentialsChanged;
            CredentialsStore.Default.CurrentProjectIdChanged += OnCredentialsChanged;
            CredentialsStore.Default.Reset += OnCredentialsChanged;

            ErrorHandlerUtils.HandleExceptionsAsync(ResetCredentialsAsync);
        }

        #region Command handlers

        private void OnDoubleClickCommand(IAcceptInput obj)
        {
            obj.OnDoubleClick();
        }


        private void OnNavigateToCloudConsoleCommand()
        {
            Process.Start("https://console.cloud.google.com/");
        }

        private async Task OnRefreshCommandAsync() => await ResetCredentialsAsync();

        #endregion

        #region Event handlers

        private void OnCredentialsChanged(object sender, EventArgs e) =>
            ErrorHandlerUtils.HandleExceptionsAsync(ResetCredentialsAsync);

        #endregion


        private async Task ResetCredentialsAsync()
        {
            try
            {
                IsBusy = true;

                UserProject.UpdateUserProfile();

                // Load the current project if one is found, otherwise ask the user to choose a project.
                UserProject.LoadCurrentProject();
                await UserProject.CurrentProjectAsync.SafeTask;

                // Update the data sources as they will depend on the project being selected.
                NotifySourcesOfUpdatedAccountOrProject();
                await RefreshSourcesAsync();

                // Notify of changes of the empty state.
                InvalidateEmptyState();

                // Update the enabled state of the buttons, to match the empty state.
                foreach (ButtonDefinition button in Buttons)
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
            RaiseAllPropertyChanged();
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
                EmptyStateCommand = UserProject.ManageAccountsCommand;
            }
            else if (UserProject.CurrentProjectAsync.Value == null)
            {
                EmptyStateMessage = Resources.CloudExploreNoProjectMessage;
                EmptyStateButtonCaption = Resources.CloudExplorerNoProjectButtonCaption;
                EmptyStateCommand = new ProtectedCommand(OnNavigateToCloudConsoleCommand);
            }
        }

        private async Task RefreshSourcesAsync()
        {
            Roots = null;
            foreach (ICloudExplorerSource<ISourceRootViewModelBase> source in _sources)
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
            foreach (ICloudExplorerSource<ISourceRootViewModelBase> source in _sources)
            {
                source.InvalidateProjectOrAccount();
            }
        }
    }
}
