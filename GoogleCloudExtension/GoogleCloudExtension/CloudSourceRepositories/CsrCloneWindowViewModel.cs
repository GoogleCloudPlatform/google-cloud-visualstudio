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
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.GitUtils;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly HashSet<string> _newReposList = new HashSet<string>();
        private string _localPath;
        private Repo _latestCreatedRepo;
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
                if (_selectedProject != null && IsReady && oldValue != _selectedProject)
                {
                    ErrorHandlerUtils.HandleAsyncExceptions(() =>
                        RepositoriesAsync.StartListRepoTaskAsync(_selectedProject.ProjectId));
                }
            }
        }

        /// <summary>
        /// The list of repositories that belong to the project
        /// </summary>
        public AsyncRepositories RepositoriesAsync { get; } = new AsyncRepositories();

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
        /// Responds to Clone button click event
        /// </summary>
        public ProtectedCommand CloneRepoCommand { get; }

        /// <summary>
        /// Responds to create repo button click event
        /// </summary>
        public ProtectedCommand CreateRepoCommand { get; }

        /// <summary>
        /// Gets a result of type <seealso cref="CloneDialogResult"/>.
        /// null inidcates no result is created, user cancelled the operation.
        /// </summary>
        public CloneDialogResult Result { get; private set; }

        public CsrCloneWindowViewModel(CsrCloneWindow owner, IList<Project> projects)
        {
            _owner = owner.ThrowIfNull(nameof(owner));
            Projects = projects.ThrowIfNull(nameof(projects));
            if (!Projects.Any())
            {
                throw new ArgumentException($"{nameof(projects)} must not be empty");
            }
            PickFolderCommand = new ProtectedCommand(PickFoloder);
            CloneRepoCommand = new ProtectedAsyncCommand(() => ExecuteAsync(CloneAsync), canExecuteCommand: false);
            CreateRepoCommand = new ProtectedCommand(OpenCreateRepoDialog, canExecuteCommand: false);
            RepositoriesAsync.PropertyChanged += RepositoriesAsyncPropertyChanged;

            var projectId = CredentialsStore.Default.CurrentProjectId;
            // If projectId is null, choose first project. Otherwise, choose the project.
            SelectedProject = Projects.FirstOrDefault(x => projectId == null || x.ProjectId == projectId);
        }

        private void RepositoriesAsyncPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                // RaiseAllPropertyChanged set e.PropertyName as null
                case null:
                case nameof(AsyncRepositories.Value):
                    CreateRepoCommand.CanExecuteCommand =
                        RepositoriesAsync.DisplayState != AsyncRepositories.DisplayOptions.Pending;

                    // Set last created repo as default
                    if (_latestCreatedRepo != null)
                    {
                        SelectedRepository = RepositoriesAsync.Value?
                            .FirstOrDefault(x => x.Name == _latestCreatedRepo.Name);
                        if (SelectedRepository != null)
                        {
                            break;
                        }
                        // else if it is null, user may have changed project, continue to select first repo.
                    }

                    SelectedRepository = RepositoriesAsync.Value?.FirstOrDefault();
                    break;
                default:
                    break;
            }
        }

        private async Task CloneAsync()
        {
            // If OkCommand is enabled, SelectedRepository and LocalPath is valid
            string destPath = Path.Combine(LocalPath.Trim(), SelectedRepository.GetRepoName());

            if (!CsrGitUtils.StoreCredential(
                SelectedRepository.Url,
                CredentialsStore.Default.CurrentAccount.RefreshToken,
                CsrGitUtils.StoreCredentialPathOption.UrlPath))
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.CsrCloneFailedToSetCredentialMessage,
                    title: Resources.UiDefaultPromptTitle);
                return;
            }

            try
            {
                var watch = Stopwatch.StartNew();
                GitRepository localRepo = await CsrGitUtils.CloneAsync(SelectedRepository.Url, destPath);
                Result = new CloneDialogResult
                {
                    RepoItem = new RepoItemViewModel(SelectedRepository, localRepo.Root),
                    JustCreatedRepo = _newReposList.Contains(SelectedRepository.Name)
                };
                _owner.Close();
                EventsReporterWrapper.ReportEvent(
                    CsrClonedEvent.Create(CommandStatus.Success, watch.Elapsed));
            }
            catch (GitCommandException)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.CsrCloneFailedMessage,
                    title: Resources.UiDefaultPromptTitle);
                EventsReporterWrapper.ReportEvent(CsrClonedEvent.Create(CommandStatus.Failure));
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

        private void ValidateInputs()
        {
            SetValidationResults(ValidateLocalPath(), nameof(LocalPath));
            CloneRepoCommand.CanExecuteCommand = SelectedRepository != null && !HasErrors;
        }

        private IEnumerable<ValidationResult> ValidateLocalPath()
        {
            string fieldName = Resources.CsrCloneLocalPathFieldName;
            string localPath = LocalPath?.Trim();
            if (String.IsNullOrEmpty(localPath))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValdiationNotEmptyMessage), fieldName);
                yield break;
            }
            if (!Directory.Exists(localPath))
            {
                yield return StringValidationResult.FromResource(nameof(Resources.CsrClonePathNotExistMessage));
                yield break;
            }
            if (SelectedRepository != null)
            {
                string destPath = Path.Combine(localPath, SelectedRepository.GetRepoName());
                if (Directory.Exists(destPath) && !PathUtils.IsDirectoryEmpty(destPath))
                {
                    yield return StringValidationResult.FromResource(
                        nameof(Resources.CsrClonePathExistNotEmptyMessageFormat), destPath);
                    yield break;
                }
            }
        }

        private void OpenCreateRepoDialog()
        {
            _latestCreatedRepo = CsrAddRepoWindow.PromptUser(RepositoriesAsync.Value, SelectedProject);
            if (_latestCreatedRepo != null)
            {
                _newReposList.Add(_latestCreatedRepo.Name);
                // Update the repos list
                ErrorHandlerUtils.HandleAsyncExceptions(
                    () => RepositoriesAsync.StartListRepoTaskAsync(_selectedProject.ProjectId));
            }
        }
    }
}
