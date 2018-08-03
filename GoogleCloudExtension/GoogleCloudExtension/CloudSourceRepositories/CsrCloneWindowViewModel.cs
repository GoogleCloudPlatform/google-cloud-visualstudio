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
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.Git;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model for user control CsrCloneWindowContent.xaml.
    /// </summary>
    public class CsrCloneWindowViewModel : ValidatingViewModelBase
    {
        internal static Func<string, IApiManager> s_getApiManagerFunc = ApiManager.GetApiManager;
        internal static readonly string s_defaultLocalPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Cloud Source Repositories");
        private static readonly List<string> s_requiredApis =
            new List<string> { KnownApis.CloudSourceRepositoryApiName };

        private readonly Action _closeOwnerFunc;
        private readonly HashSet<string> _newReposList = new HashSet<string>();
        private string _localPath = s_defaultLocalPath;
        private Repo _latestCreatedRepo;
        private Repo _selectedRepo;
        private IEnumerable<Project> _projects;
        private Project _selectedProject;
        private bool _isReady = true;
        private AsyncProperty<bool> _apisAreEnabled;
        private AsyncProperty<IList<Repo>> _repositoriesAsync = new AsyncProperty<IList<Repo>>(null);
        internal static Func<string, Task<IList<Repo>>> s_getCloudReposAsync = CsrUtils.GetCloudReposAsync;

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
            get => _selectedProject;
            set
            {
                Project oldValue = _selectedProject;
                SetValueAndRaise(ref _selectedProject, value);
                if (oldValue != _selectedProject)
                {
                    IApiManager apiManager = s_getApiManagerFunc(_selectedProject.ProjectId);
                    ApisAreEnabled = new AsyncProperty<bool>(apiManager.AreServicesEnabledAsync(s_requiredApis));
                }
            }
        }

        /// <summary>
        /// The list of repositories that belong to the project
        /// </summary>
        public AsyncProperty<IList<Repo>> RepositoriesAsync
        {
            get => _repositoriesAsync;
            set
            {
                RepositoriesAsync.PropertyChanged -= OnAsycRepositoriesPropertyChanged;
                SetValueAndRaise(ref _repositoriesAsync, value);
                RepositoriesAsync.PropertyChanged += OnAsycRepositoriesPropertyChanged;
                OnAsycRepositoriesPropertyChanged(null, null);
            }
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
        /// Indicates if the selected project needs to enable the CSR APIs.
        /// </summary>
        public AsyncProperty<bool> ApisAreEnabled
        {
            get => _apisAreEnabled;
            set
            {
                SetValueAndRaise(ref _apisAreEnabled, value);
                RepositoriesAsync = new AsyncProperty<IList<Repo>>(LoadReposAsync(), new Repo[0]);
            }
        }

        private async Task<IList<Repo>> LoadReposAsync()
        {
            await ApisAreEnabled.SafeTask;
            if (ApisAreEnabled.Value)
            {
                return await s_getCloudReposAsync(_selectedProject.ProjectId);
            }
            else
            {
                return new Repo[0];
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
        public ProtectedAsyncCommand CloneRepoCommand { get; }

        /// <summary>
        /// Responds to create repo button click event
        /// </summary>
        public ProtectedCommand CreateRepoCommand { get; }

        /// <summary>
        /// Responds to the enable api link button click event
        /// </summary>
        public ProtectedAsyncCommand EnableApiCommand { get; }

        /// <summary>
        /// Gets a result of type <seealso cref="CloneDialogResult"/>.
        /// null inidcates no result is created, user cancelled the operation.
        /// </summary>
        public CloneDialogResult Result { get; private set; }

        public CsrCloneWindowViewModel(Action closeOwnerFunc, IList<Project> projects)
        {
            _closeOwnerFunc = closeOwnerFunc.ThrowIfNull(nameof(closeOwnerFunc));
            Projects = projects.ThrowIfNull(nameof(projects));
            if (!Projects.Any())
            {
                throw new ArgumentException($"{nameof(projects)} must not be empty");
            }
            EnableApiCommand = new ProtectedAsyncCommand(() => ExecuteAsync(OnEnableApiCommandAsync));
            PickFolderCommand = new ProtectedCommand(PickFoloder);
            CloneRepoCommand = new ProtectedAsyncCommand(() => ExecuteAsync(CloneAsync), canExecuteCommand: false);
            CreateRepoCommand = new ProtectedCommand(OpenCreateRepoDialog, canExecuteCommand: false);

            var projectId = CredentialsStore.Default.CurrentProjectId;
            // If projectId is null, choose first project. Otherwise, choose the project.
            SelectedProject = Projects.FirstOrDefault(x => projectId == null || x.ProjectId == projectId);
        }

        private void OnAsycRepositoriesPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CreateRepoCommand.CanExecuteCommand = RepositoriesAsync.IsSuccess;

            // Set last created repo as default
            if (_latestCreatedRepo != null)
            {
                SelectedRepository = RepositoriesAsync.Value?
                    .FirstOrDefault(x => x.Name == _latestCreatedRepo.Name);
                if (SelectedRepository != null)
                {
                    return;
                }

                // else if it is null, user may have changed project, continue to select first repo.
            }

            SelectedRepository = RepositoriesAsync.Value?.FirstOrDefault();
        }

        private async Task CloneAsync()
        {
            if (IsDefaultLocation(LocalPath) && !Directory.Exists(s_defaultLocalPath))
            {
                Directory.CreateDirectory(s_defaultLocalPath);
            }

            // If OkCommand is enabled, SelectedRepository and LocalPath is valid
            string destPath = Path.Combine(LocalPath, SelectedRepository.GetRepoName());

            if (!CsrGitUtils.StoreCredential(
                SelectedRepository.Url,
                CredentialsStore.Default.CurrentAccount.RefreshToken,
                CsrGitUtils.StoreCredentialPathOption.UrlPath))
            {
                UserPromptService.Default.ErrorPrompt(
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
                _closeOwnerFunc();
                EventsReporterWrapper.ReportEvent(
                    CsrClonedEvent.Create(CommandStatus.Success, watch.Elapsed));
            }
            catch (GitCommandException)
            {
                UserPromptService.Default.ErrorPrompt(
                    message: Resources.CsrCloneFailedMessage,
                    title: Resources.UiDefaultPromptTitle);
                EventsReporterWrapper.ReportEvent(CsrClonedEvent.Create(CommandStatus.Failure));
                return;
            }
        }

        private async Task OnEnableApiCommandAsync()
        {
            IApiManager apiManager = s_getApiManagerFunc(_selectedProject.ProjectId);
            await apiManager.EnableServicesAsync(s_requiredApis);
            ApisAreEnabled = new AsyncProperty<bool>(true);
        }

        private void PickFoloder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.SelectedPath = LocalPath;
                dialog.ShowNewFolderButton = true;
                DialogResult result = dialog.ShowDialog();
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
            if (string.IsNullOrEmpty(LocalPath))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValdiationNotEmptyMessage), fieldName);
                yield break;
            }
            if (!IsDefaultLocation(LocalPath) && !Directory.Exists(LocalPath))
            {
                yield return StringValidationResult.FromResource(nameof(Resources.CsrClonePathNotExistMessage));
            }
            if (SelectedRepository != null)
            {
                string destPath = Path.Combine(LocalPath, SelectedRepository.GetRepoName());
                if (Directory.Exists(destPath) && !PathUtils.IsDirectoryEmpty(destPath))
                {
                    yield return StringValidationResult.FromResource(
                        nameof(Resources.CsrClonePathExistNotEmptyMessageFormat), destPath);
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
                RepositoriesAsync = new AsyncProperty<IList<Repo>>(LoadReposAsync(), new Repo[0]);
            }
        }

        private bool IsDefaultLocation(string localPath) =>
            string.Equals(Path.GetFullPath(localPath), s_defaultLocalPath, StringComparison.OrdinalIgnoreCase);
    }
}
