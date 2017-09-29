// Copyright 2017 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.ProjectIdDialog
{
    /// <summary>
    /// View model for picking a project id.
    /// </summary>
    public class PickProjectIdViewModel : ViewModelBase
    {
        private IList<Project> _projects;
        private Project _selectedProject;
        private string _projectId;
        private AsyncProperty _loadTask;

        private readonly IPickProjectIdWindow _owner;
        private readonly Func<IResourceManagerDataSource> _resourceManagerDataSourceFactory;
        private readonly Action _promptAccountManagement;

        /// <summary>
        /// Result of the view model after the dialog window is closed. Remains
        /// null until an action buttion is clicked.
        /// </summary>
        public string Result { get; private set; }

        /// <summary>
        /// Command to open the manage users dialog.
        /// </summary>
        public ProtectedCommand ChangeUserCommand { get; }

        /// <summary>
        /// Command to confirm the selection of a project id.
        /// </summary>
        public ProtectedCommand OkCommand { get; }

        /// <summary>
        /// Command to skip project input.
        /// </summary>
        public ProtectedCommand SkipCommand { get; }

        /// <summary>
        /// The list of projects available to the current user.
        /// </summary>
        public IList<Project> Projects
        {
            get { return _projects; }
            set { SetValueAndRaise(ref _projects, value); }
        }

        /// <summary>
        /// The project selected from the list of current projects.
        /// </summary>
        public Project SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                SetValueAndRaise(ref _selectedProject, value);
                if (SelectedProject != null)
                {
                    ProjectId = SelectedProject.ProjectId;
                }
            }
        }

        /// <summary>
        /// The project id that will be selected on an OkCommand.
        /// </summary>
        public string ProjectId
        {
            get { return _projectId; }
            set
            {
                SetValueAndRaise(ref _projectId, value);
                OkCommand.CanExecuteCommand = !string.IsNullOrEmpty(ProjectId);
            }
        }

        /// <summary>
        /// The property that surfaces task completion information for the Load Projects task.
        /// </summary>
        public AsyncProperty LoadTask
        {
            get { return _loadTask; }
            set { SetValueAndRaise(ref _loadTask, value); }
        }

        public PickProjectIdViewModel(IPickProjectIdWindow owner)
            : this(owner, DataSourceFactories.CreateResourceManagerDataSource, ManageAccountsWindow.PromptUser)
        { }

        /// <summary>
        /// For Testing.
        /// </summary>
        /// <param name="owner">The window or mock window that owns this ViewModel.</param>
        /// <param name="dataSourceFactory">The factory of the source of projects.</param>
        /// <param name="promptAccountManagement">Action to prompt managing accounts.</param>
        internal PickProjectIdViewModel(
            IPickProjectIdWindow owner,
            Func<IResourceManagerDataSource> dataSourceFactory,
            Action promptAccountManagement)
        {
            _owner = owner;
            _resourceManagerDataSourceFactory = dataSourceFactory;
            _promptAccountManagement = promptAccountManagement;

            ChangeUserCommand = new ProtectedCommand(OnChangeUser);
            OkCommand = new ProtectedCommand(OnOk, false);
            SkipCommand = new ProtectedCommand(OnSkip);
            ProjectId = CredentialsStore.Default.CurrentProjectId;
            StartLoadProjects();
        }

        private void StartLoadProjects()
        {
            if (CredentialsStore.Default.CurrentAccount != null)
            {
                LoadTask = AsyncPropertyUtils.CreateAsyncProperty(LoadProjectsAsync());
            }
            else
            {
                LoadTask = null;
            }
        }

        private async Task LoadProjectsAsync()
        {
            Projects = await _resourceManagerDataSourceFactory().GetSortedActiveProjectsAsync();
            if (string.IsNullOrEmpty(ProjectId) || ProjectId == SelectedProject?.ProjectId)
            {
                // Updates ProjectId within the property.
                SelectedProject =
                    Projects.FirstOrDefault(p => p.ProjectId == CredentialsStore.Default.CurrentProjectId) ??
                    Projects.FirstOrDefault();
            }
            else
            {
                SelectedProject = Projects.FirstOrDefault(p => p.ProjectId == ProjectId);
            }
        }

        private void OnChangeUser()
        {
            _promptAccountManagement();
            StartLoadProjects();
        }

        private void OnOk()
        {
            Result = ProjectId;
            _owner.Close();
        }

        private void OnSkip()
        {
            Result = "";
            _owner.Close();
        }
    }
}