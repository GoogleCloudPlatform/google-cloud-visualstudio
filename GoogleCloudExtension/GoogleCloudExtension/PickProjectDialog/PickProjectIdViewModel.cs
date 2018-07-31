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
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PickProjectDialog
{
    /// <summary>
    /// View model for picking a project id.
    /// </summary>
    public class PickProjectIdViewModel : ViewModelBase, IPickProjectIdViewModel
    {
        private IEnumerable<Project> _projects;
        private Project _selectedProject;
        private AsyncProperty _loadTask;
        private bool _allowAccountChange;
        private string _filter;
        private string _helpText;
        private bool _hasAccount;

        /// <summary>
        /// Result of the view model after the dialog window is closed. Remains
        /// null until an action buttion is clicked.
        /// </summary>
        public Project Result { get; private set; }

        /// <summary>
        /// Command to open the manage users dialog.
        /// </summary>
        public ProtectedCommand ChangeUserCommand { get; }

        /// <summary>
        /// Command to confirm the selection of a project id.
        /// </summary>
        public ProtectedCommand OkCommand { get; }

        /// <summary>
        /// Command to execute when refreshing the list of projects.
        /// </summary>
        public ProtectedCommand RefreshCommand { get; }

        /// <summary>
        /// The list of projects available to the current user.
        /// </summary>
        public IEnumerable<Project> Projects
        {
            get { return _projects; }
            private set { SetValueAndRaise(ref _projects, value); }
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
                OkCommand.CanExecuteCommand = !string.IsNullOrEmpty(SelectedProject?.ProjectId);
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

        public bool HasAccount
        {
            get { return _hasAccount; }
            private set { SetValueAndRaise(ref _hasAccount, value); }
        }

        public string Filter
        {
            get { return _filter; }
            set { SetValueAndRaise(ref _filter, value); }
        }

        public bool AllowAccountChange
        {
            get { return _allowAccountChange; }
            private set { SetValueAndRaise(ref _allowAccountChange, value); }
        }

        public string HelpText
        {
            get { return _helpText; }
            private set { SetValueAndRaise(ref _helpText, value); }
        }

        /// <summary>
        /// Implements <see cref="ICloseSource.Close"/>. When invoked, tells the parent window to close.
        /// </summary>
        public event Action Close;

        public PickProjectIdViewModel(string helpText, bool allowAccountChange)
        {
            AllowAccountChange = allowAccountChange;
            HelpText = helpText;

            ChangeUserCommand = new ProtectedCommand(OnChangeUserCommand);
            OkCommand = new ProtectedCommand(OnOkCommand, canExecuteCommand: false);
            RefreshCommand = new ProtectedCommand(OnRefreshCommand, canExecuteCommand: false);
            StartLoadProjects();
        }

        public bool FilterItem(object item)
        {
            var project = item as Project;
            if (project == null)
            {
                return false;
            }

            // If there is no filter, allow the item.
            if (string.IsNullOrEmpty(Filter))
            {
                return true;
            }

            // Check name and project id for the filter.
            return project.ProjectId.Contains(Filter) || project.Name.Contains(Filter);
        }

        private void StartLoadProjects()
        {
            if (CredentialsStore.Default.CurrentAccount != null)
            {
                LoadTask = new AsyncProperty(LoadProjectsAsync());
                HasAccount = true;
            }
            else
            {
                LoadTask = null;
                HasAccount = false;
            }
        }

        private async Task LoadProjectsAsync()
        {
            // The projects list will be empty while we load.
            Projects = Enumerable.Empty<Project>();
            RefreshCommand.CanExecuteCommand = false;

            // Updat the to loaded list of projects.
            Projects = (await DataSourceFactory.Default.ResourceManagerDataSource.ProjectsListTask) ?? Enumerable.Empty<Project>();
            RefreshCommand.CanExecuteCommand = true;

            // Choose project from the list.
            if (SelectedProject == null)
            {
                SelectedProject = Projects.FirstOrDefault(p => p.ProjectId == CredentialsStore.Default.CurrentProjectId);
            }
            else
            {
                SelectedProject = Projects.FirstOrDefault(p => p.ProjectId == SelectedProject.ProjectId);
            }
        }

        private void OnChangeUserCommand()
        {
            GoogleCloudExtensionPackage.Instance.UserPromptService.PromptUser(new ManageAccountsWindowContent());
            StartLoadProjects();
        }

        private void OnOkCommand()
        {
            Result = SelectedProject;
            Close?.Invoke();
        }

        private void OnRefreshCommand()
        {
            DataSourceFactory.Default.ResourceManagerDataSource.RefreshProjects();
            StartLoadProjects();
        }
    }
}