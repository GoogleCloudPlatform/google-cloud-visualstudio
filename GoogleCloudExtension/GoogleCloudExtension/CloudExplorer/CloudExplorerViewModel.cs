// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
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
    /// This class contains the view specific logic for the AppEngineAppsToolWindow view.
    /// </summary>
    internal class CloudExplorerViewModel : ViewModelBase
    {
        private const string RefreshImagePath = "CloudExplorer/Resources/refresh.png";

        private static readonly Lazy<ImageSource> s_refreshIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(RefreshImagePath));

        private readonly IList<ICloudExplorerSource> _sources;
        private readonly List<ButtonDefinition> _buttons;
        private AsyncPropertyValue<IEnumerable<Project>> _projectsAsync;
        private AsyncPropertyValue<string> _profilePictureAsync;
        private AsyncPropertyValue<string> _profileNameAsync;
        private Project _currentProject;
        private bool _changingCredentials;
        private Lazy<ResourceManagerDataSource> _resourceManagerDataSource;
        private Lazy<GPlusDataSource> _plusDataSource;

        /// <summary>
        /// The list of module and version combinations for the current project.
        /// </summary>
        public IEnumerable<TreeHierarchy> Roots => _sources.Select(x => x.Root);

        public AsyncPropertyValue<IEnumerable<Project>> ProjectsAsync
        {
            get { return _projectsAsync; }
            set { SetValueAndRaise(ref _projectsAsync, value); }
        }

        public AsyncPropertyValue<string> ProfilePictureAsync
        {
            get { return _profilePictureAsync; }
            set { SetValueAndRaise(ref _profilePictureAsync, value); }
        }

        public AsyncPropertyValue<string> ProfileNameAsync
        {
            get { return _profileNameAsync; }
            set { SetValueAndRaise(ref _profileNameAsync, value); }
        }

        public WeakCommand ManageAccountsCommand { get; }

        public IList<ButtonDefinition> Buttons => _buttons;

        public Project CurrentProject
        {
            get { return _currentProject; }
            set
            {
                SetValueAndRaise(ref _currentProject, value);
                InvalidateCurrentProject();
            }
        }

        public CloudExplorerViewModel(IEnumerable<ICloudExplorerSource> sources)
        {
            _sources = new List<ICloudExplorerSource>(sources);
            _buttons = new List<ButtonDefinition>()
            {
                new ButtonDefinition
                {
                    Icon = s_refreshIcon.Value,
                    ToolTip = "Refresh",
                    Command = new WeakCommand(this.OnRefresh),
                }
            };

            ResetDataSources();

            ProjectsAsync = new AsyncPropertyValue<IEnumerable<Project>>(LoadProjectListAsync());

            foreach (var source in _sources)
            {
                var sourceButtons = source.Buttons;
                _buttons.AddRange(sourceButtons);
            }

            ManageAccountsCommand = new WeakCommand(OnManageAccountsCommand, canExecuteCommand: AccountsManager.CurrentAccount != null);
            if (AccountsManager.CurrentAccount != null)
            {
                UpdateUserProfile();
            }

            AccountsManager.CurrentCredentialsChanged += OnCurrentCredentialsChanged;
        }

        private static GPlusDataSource CreatePlusDataSource() => new GPlusDataSource(AccountsManager.CurrentGoogleCredential);

        private static ResourceManagerDataSource CreateResourceManagerDataSource() => new ResourceManagerDataSource(AccountsManager.CurrentGoogleCredential);

        private void UpdateUserProfile()
        {
            var profileTask = _plusDataSource.Value.GetProfileAsync();
            ProfilePictureAsync = AsyncPropertyValue<string>.CreateAsyncProperty(profileTask, x => x.Image.Url);
            ProfileNameAsync = AsyncPropertyValue<string>.CreateAsyncProperty(
                profileTask,
                x => x.Emails.FirstOrDefault()?.Value,
                "Loading...");
        }

        private void OnManageAccountsCommand()
        {
            var dialog = new ManageAccountsWindow();
            dialog.ShowModal();
        }

        private async void OnCurrentCredentialsChanged(object sender, EventArgs e)
        {
            try
            {
                _changingCredentials = true;

                ResetDataSources();

                ManageAccountsCommand.CanExecuteCommand = AccountsManager.CurrentAccount != null;

                if (AccountsManager.CurrentAccount == null)
                {
                    ProjectsAsync = null;
                    InvalidateSourcesCredentials();
                    RefreshSources();
                    return;
                }

                UpdateUserProfile();

                var projectsTask = LoadProjectListAsync();
                ProjectsAsync = new AsyncPropertyValue<IEnumerable<Project>>(projectsTask);

                var projects = await projectsTask;
                _changingCredentials = false;

                CurrentProject = projects.FirstOrDefault();
            }
            finally
            {
                _changingCredentials = false;
            }
        }

        private void ResetDataSources()
        {
            _resourceManagerDataSource = new Lazy<ResourceManagerDataSource>(CreateResourceManagerDataSource);
            _plusDataSource = new Lazy<GPlusDataSource>(CreatePlusDataSource);
        }

        private async Task<IEnumerable<Project>> LoadProjectListAsync()
        {
            return await _resourceManagerDataSource.Value.GetProjectsListAsync();
        }

        private void OnRefresh()
        {
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
        private void InvalidateSourcesCredentials()
        {
            foreach (var source in _sources)
            {
                source.InvalidateCredentials();
            }
        }

        /// <summary>
        /// Called whenever the current project changes, updates all of the sources with the new credentials.
        /// </summary>
        private void InvalidateCurrentProject()
        {
            if (_changingCredentials)
            {
                Debug.WriteLine("Invalidating the current project while changing credentials.");
                return;
            }

            Debug.WriteLine($"Setting selected project to {CurrentProject?.ProjectId ?? "null"}");
            foreach (var source in _sources)
            {
                source.CurrentProject = CurrentProject;
            }

            InvalidateSourcesCredentials();
            RefreshSources();
        }
    }
}
