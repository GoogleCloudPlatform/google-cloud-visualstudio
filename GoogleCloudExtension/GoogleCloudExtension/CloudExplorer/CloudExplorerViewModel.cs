// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
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
        private AsyncPropertyValue<IEnumerable<GcpProject>> _projectsAsync;
        private GcpProject _currentProject;

        /// <summary>
        /// The list of module and version combinations for the current project.
        /// </summary>
        public IEnumerable<TreeHierarchy> Roots
        {
            get
            {
                foreach (var source in _sources)
                {
                    yield return source.Root;
                }
            }
        }

        public AsyncPropertyValue<IEnumerable<GcpProject>> ProjectsAsync
        {
            get { return _projectsAsync; }
            set { SetValueAndRaise(ref _projectsAsync, value); }
        }

        public IList<ButtonDefinition> Buttons => _buttons;

        public GcpProject CurrentProject
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

            ProjectsAsync = new AsyncPropertyValue<IEnumerable<GcpProject>>(LoadProjectListAsync());

            foreach (var source in _sources)
            {
                var sourceButtons = source.Buttons;
                _buttons.AddRange(sourceButtons);
            }

            AccountsManager.CurrentCredentialsChanged += OnCurrentCredentialsChanged;
        }

        private async void OnCurrentCredentialsChanged(object sender, EventArgs e)
        {
            if (AccountsManager.CurrentCredentials == null)
            {
                ProjectsAsync = null;
                RefreshSources();
                return;
            }

            var projectsTask = LoadProjectListAsync();
            ProjectsAsync = new AsyncPropertyValue<IEnumerable<GcpProject>>(projectsTask);

            await projectsTask;
            RefreshSources();
        }

        private async Task<IEnumerable<GcpProject>> LoadProjectListAsync()
        {
            var oauthToken = await AccountsManager.GetAccessTokenAsync();
            return await ResourceManagerDataSource.GetProjectsListAsync(oauthToken);
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

        private void InvalidateCurrentProject()
        {
            Debug.WriteLine($"Setting selected project to {CurrentProject?.Id ?? "null"}");
            foreach (var source in _sources)
            {
                source.CurrentProject = CurrentProject;
            }
            RefreshSources();
        }
    }
}
