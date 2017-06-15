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
using Google.Apis.CloudSourceRepositories.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.GitUtils;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model for user control PickFileWindowContent.xaml.
    /// </summary>
    public class CsrCloneWindowViewModel : ViewModelBase
    {
        private CsrCloneWindow _owner;

        private string _localPath;
        private IList<Repo> _repos;
        private Repo _selectedRepo;
        private IEnumerable<Project> _projects;
        private Project _selectedProject;
        private bool _isReady = true;

        /// <summary>
        /// The projects list
        /// </summary>
        public IEnumerable<Project> Projects
        {
            get { return _projects; }
            private set { SetValueAndRaise(ref _projects, value); }
        }

        /// <summary>
        /// Selected project
        /// </summary>
        public Project SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                var oldValue = _selectedProject;
                SetValueAndRaise(ref _selectedProject, value);
                if (_selectedProject != null && _isReady && oldValue != _selectedProject)
                {
                    ErrorHandlerUtils.HandleAsyncExceptions(
                        () => ExecuteAsync(ListRepoAsync));
                }
            }
        }

        /// <summary>
        /// The list of repositories belong to the project
        /// </summary>
        public IList<Repo> Repositories
        {
            get { return _repos; }
            private set { SetValueAndRaise(ref _repos, value); }
        }

        /// <summary>
        /// Currently selected repository
        /// </summary>
        public Repo SelectedRepository
        {
            get { return _selectedRepo; }
            set { SetValueAndRaise(ref _selectedRepo, value); }
        }

        /// <summary>
        /// The local path to clone the repository to 
        /// </summary>
        public string LocalPath
        {
            get { return _localPath; }
            set { SetValueAndRaise(ref _localPath, value); }
        }

        /// <summary>
        /// Indicates if there is async task running that UI should be disabled.
        /// </summary>
        public bool IsReady
        {
            get { return _isReady; }
            set { SetValueAndRaise(ref _isReady, value); }
        }

        /// <summary>
        /// Responds to choose a folder command
        /// </summary>
        public ICommand PickFolderCommand { get; }

        /// <summary>
        /// Responds to OK button click event
        /// </summary>
        public ICommand OkCommand { get; }

        /// <summary>
        /// Final cloned repository
        /// </summary>
        public RepoItemViewModel Result { get; private set; }

        public CsrCloneWindowViewModel(CsrCloneWindow owner)
        {
            _owner = owner.ThrowIfNull(nameof(owner));
            PickFolderCommand = new ProtectedCommand(PickFoloder);
            OkCommand = new ProtectedCommand(taskHandler: () => ExecuteAsync(Clone));
            ErrorHandlerUtils.HandleAsyncExceptions(() => ExecuteAsync(Init));
        }

        private async Task Clone()
        {
            if (SelectedRepository == null || String.IsNullOrWhiteSpace(LocalPath) 
                || !Directory.Exists(LocalPath))
            {
                // TODO: show input validation error
                return;
            }

            string destPath = Path.Combine(LocalPath, CsrUtils.GetRepoName(SelectedRepository));
            if (Directory.Exists(destPath))
            {
                // TODO: show input validation error
                return;
            }

            if (!CsrGitUtils.StoreCredential(
                SelectedRepository.Url,
                CredentialsStore.Default.CurrentAccount.RefreshToken,
                useHttpPath: true))
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.CsrCloneFailedMessage,
                    title: Resources.uiDefaultPromptTitle);
                return;
            }

            GitRepository gitCommand = await CsrGitUtils.Clone(SelectedRepository.Url, destPath);
            if (gitCommand == null)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.CsrCloneFailedToSetCredentialMessage,
                    title: Resources.uiDefaultPromptTitle);
                return;
            }
            Result = new RepoItemViewModel(SelectedRepository, gitCommand);
            _owner.Close();
        }

        private void PickFoloder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = Resources.CsrCloneLocalPathLabel;
                dialog.SelectedPath = LocalPath;
                dialog.ShowNewFolderButton = true;
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    LocalPath = dialog.SelectedPath;
                }
            }
        }

        private async Task ExecuteAsync(Func<Task> task)
        {
            IsReady = false;
            try
            {
                await task();
            }
            finally
            {
                IsReady = true;
            }
        }

        private async Task ListRepoAsync()
        {
            Debug.WriteLine("ListRepoAsync");

            Repositories = null;
            if (SelectedProject == null)
            {
                return;
            }

            Repositories = await CsrUtils.GetCloudReposAsync(SelectedProject);
            SelectedRepository = Repositories?.FirstOrDefault();
        }

        private async Task Init()
        {
            Debug.WriteLine("Init");

            Projects = await CsrUtils.GetProjectsAsync();
            if (Projects?.Any() ?? false)
            {
                SelectedProject = Projects.FirstOrDefault();
                await ListRepoAsync();
            }
            else
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.CsrCloneNoProject,
                    title: Resources.uiDefaultPromptTitle);
                _owner.Close();
            }
        }
    }
}
