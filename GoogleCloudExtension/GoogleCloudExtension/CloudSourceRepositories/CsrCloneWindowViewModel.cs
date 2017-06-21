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
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model for user control CsrCloneWindowContent.xaml.
    /// </summary>
    public class CsrCloneWindowViewModel : ValidatingViewModelBase
    {
        private readonly CsrCloneWindow _owner;

        private string _localPath;
        private IList<Repo> _repos;
        private Repo _selectedRepo;
        private IEnumerable<Project> _projects;
        private Project _selectedProject;
        private bool _isReady = true;
        private bool _projectHasRepo;

        /// <summary>
        /// Indicate if the selected project has repository
        /// </summary>
        public bool ProjectHasRepo
        {
            get { return _projectHasRepo; }
            private set { SetValueAndRaise(ref _projectHasRepo, value); }
        }

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
                    ErrorHandlerUtils.HandleAsyncExceptions(() => ExecuteAsync(ListRepoAsync));
                }
            }
        }

        /// <summary>
        /// The list of repositories that belong to the project
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
            set
            {
                SetValueAndRaise(ref _selectedRepo, value);
                ValidateInputs();
            }
        }

        /// <summary>
        /// The local path to clone the repository to 
        /// </summary>
        public string LocalPath
        {
            get { return _localPath; }
            set
            {
                SetValueAndRaise(ref _localPath, value);
                ValidateInputs();
            }
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
        public ProtectedCommand OkCommand { get; }

        /// <summary>
        /// Final cloned repository
        /// </summary>
        public RepoItemViewModel Result { get; private set; }

        public CsrCloneWindowViewModel(CsrCloneWindow owner, IList<Project> projects)
        {
            _owner = owner.ThrowIfNull(nameof(owner));
            Projects = projects.ThrowIfNull(nameof(projects));
            if (!Projects.Any())
            {
                throw new ArgumentException($"{nameof(projects)} must not be empty");
            }
            PickFolderCommand = new ProtectedCommand(PickFoloder);
            OkCommand = new ProtectedAsyncCommand(() => ExecuteAsync(CloneAsync), canExecuteCommand: false);
            ErrorHandlerUtils.HandleAsyncExceptions(() => ExecuteAsync(InitializeAsync));
        }

        private async Task CloneAsync()
        {
            // If OkCommand is enabled, SelectedRepository and LocalPath is valid
            string destPath = Path.Combine(LocalPath, SelectedRepository.GetRepoName());

            if (!CsrGitUtils.StoreCredential(
                SelectedRepository.Url,
                CredentialsStore.Default.CurrentAccount.RefreshToken,
                useHttpPath: true))
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.CsrCloneFailedToSetCredentialMessage,
                    title: Resources.UiDefaultPromptTitle);
                return;
            }

            try
            {
                GitRepository localRepo = await CsrGitUtils.Clone(SelectedRepository.Url, destPath);
                Result = new RepoItemViewModel(SelectedRepository, localRepo.Root);
                _owner.Close();
            }
            catch (CsrGitCommandException)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.CsrCloneFailedMessage,
                    title: Resources.UiDefaultPromptTitle);
                return;
            }
        }

        private void PickFoloder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
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

            Repositories = await CsrUtils.GetCloudReposAsync(SelectedProject.ProjectId);
            SelectedRepository = Repositories?.FirstOrDefault();
            ProjectHasRepo = Repositories?.Any() ?? false;
        }

        private async Task InitializeAsync()
        {
            Debug.WriteLine("Init");
            SelectedProject = Projects.FirstOrDefault();
            await ListRepoAsync();
        }

        private void ValidateInputs()
        {
            SetValidationResults(ValidateLocalPath(), nameof(LocalPath));
            OkCommand.CanExecuteCommand = SelectedRepository != null && !HasErrors;
        }

        private IEnumerable<ValidationResult> ValidateLocalPath()
        {
            string fieldName = Resources.CsrCloneLocalPathFieldName;
            if (String.IsNullOrEmpty(LocalPath))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValdiationNotEmptyMessage), fieldName);
                yield break;
            }
            if (!Directory.Exists(LocalPath))
            {
                yield return StringValidationResult.FromResource(nameof(Resources.CsrClonePathNotExistMessage));
                yield break;
            }
            if (SelectedRepository != null)
            {
                string destPath = Path.Combine(LocalPath, SelectedRepository.GetRepoName());
                if (Directory.Exists(destPath) && !PathUtils.IsDirectoryEmpty(destPath))
                {
                    yield return StringValidationResult.FromResource(
                        nameof(Resources.CsrClonePathExistNotEmptyMessageFormat), destPath);
                    yield break;
                }
            }
        }
    }
}
