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
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GitUtils;
using GoogleCloudExtension.TeamExplorerExtension;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model to CsrReposContent.xaml
    /// </summary>
    public class CsrReposViewModel : ViewModelBase
    {
        /// <summary>
        /// Sometimes, the view and view model is recreated by Team Explorer.
        /// This is to preserve the list when a new user control is created.
        /// Without doing so, user constantly sees the list of repos are loading without reasons.
        /// </summary>
        private readonly static ObservableCollection<RepoItemViewModel> s_repoList
            = new ObservableCollection<RepoItemViewModel>();
        private static RepoItemViewModel s_activeRepo;

        private readonly ITeamExplorerUtils _teamExplorer;
        private bool _isReady = true;
        private RepoItemViewModel _selectedRepo;

        /// <summary>
        /// Gets the current active Repo
        /// </summary>
        public static RepoItemViewModel ActiveRepo
        {
            get { return s_activeRepo; }
            set
            {
                if (value != s_activeRepo && s_activeRepo != null)
                {
                    s_activeRepo.IsActiveRepo = false;
                }

                if (value != null)
                {
                    value.IsActiveRepo = true;
                }

                s_activeRepo = value;
            }
        }

        /// <summary>
        /// Indicates if the current view is not busy.
        /// </summary>
        public bool IsReady
        {
            get { return _isReady; }
            private set { SetValueAndRaise(ref _isReady, value); }
        }

        /// <summary>
        /// Show the list of repositories
        /// </summary>
        public ObservableCollection<RepoItemViewModel> Repositories
        {
            get { return s_repoList; }
        }

        /// <summary>
        /// Currently selected repository
        /// </summary>
        public RepoItemViewModel SelectedRepository
        {
            get { return _selectedRepo; }
            set { SetValueAndRaise(ref _selectedRepo, value); }
        }

        /// <summary>
        /// Responds to Clone command
        /// </summary>
        public ICommand CloneCreateRepoCommand { get; }

        /// <summary>
        /// Responds to Disconnect command
        /// </summary>
        public ICommand DisconnectCommand { get; }

        /// <summary>
        /// Responds to list view double click event
        /// </summary>
        public ICommand ListDoubleClickCommand { get; }

        public CsrReposViewModel(ITeamExplorerUtils teamExplorer)
        {
            _teamExplorer = teamExplorer.ThrowIfNull(nameof(teamExplorer));
            ListDoubleClickCommand = new ProtectedCommand(() =>
            {
                SetRepoActive(SelectedRepository);
                // Note, the order is critical.
                // When switching to HomeSection, current "this" object is destroyed.
                _teamExplorer.ShowHomeSection();
            });
            CloneCreateRepoCommand = new ProtectedAsyncCommand(CloneCreateRepoAsync);
        }

        /// <summary>
        /// Reload the repository list.
        /// </summary>
        public void Refresh()
        {
            ErrorHandlerUtils.HandleExceptionsAsync(ListRepositoryAsync);
        }

        /// <summary>
        /// When user double clicks at a repository, set it as active repo.
        /// </summary>
        public void SetRepoActive(RepoItemViewModel repo)
        {
            if (repo?.IsActiveRepo == false)
            {
                CreateEmptySolutionAtPath(repo.LocalPath);
                ActiveRepo = repo;
            }
        }

        /// <summary>
        /// Set the repository at the path as active, show the item in Bold font.
        /// </summary>
        /// <param name="localPath">The repository local path</param>
        public void ShowActiveRepo(string localPath)
        {
            var repoItem = Repositories?.FirstOrDefault(
                x => String.Compare(x.LocalPath, localPath, StringComparison.OrdinalIgnoreCase) == 0);
            ActiveRepo = repoItem;
        }

        /// <summary>
        /// By creating an empty solution at the path, Visual Studio git service sets the repo as current.
        /// </summary>
        private void CreateEmptySolutionAtPath(string localPath)
        {
            string guid = Guid.NewGuid().ToString();
            try
            {
                ShellUtils.Default.CreateEmptySolution(localPath, guid);
            }
            finally
            {
                try
                {
                    // Clean up the dummy `.vs` directory.
                    string tmpPath = Path.Combine(localPath, ".vs", guid);
                    if (Directory.Exists(tmpPath))
                    {
                        Directory.Delete(tmpPath, recursive: true);
                    }
                }
                catch (Exception ex) when (
                    ex is IOException ||
                    ex is UnauthorizedAccessException)
                { }
            }
        }


        /// <summary>
        /// Get a list of local repositories.  It is saved to local variable localRepos.
        /// For each local repository, get remote urls list.
        /// From each URL, get the project-id. 
        /// Now, check if the list of 'cloud repositories' under the project-id contains the URL.
        /// If it does, the local repository with the URL will be shown to user.
        /// </summary>
        private async Task ListRepositoryAsync()
        {
            if (!IsReady)
            {
                return;
            }

            var projects = await GetProjectsAsync();

            IsReady = false;
            Repositories.Clear();
            try
            {
                var watch = Stopwatch.StartNew();
                await AddLocalReposAsync(await GetLocalGitRepositoriesAsync(), projects);
                EventsReporterWrapper.ReportEvent(
                    CsrListedEvent.Create(CommandStatus.Success, watch.Elapsed));
                ShowActiveRepo(_teamExplorer.GetActiveRepository());
            }
            catch (Exception)
            {
                EventsReporterWrapper.ReportEvent(CsrListedEvent.Create(CommandStatus.Failure));
                throw;
            }
            finally
            {
                IsReady = true;
            }
        }

        /// <summary>
        /// projectRepos is used to cache the list of 'cloud repos' of the project-id.
        /// </summary>
        private async Task AddLocalReposAsync(IList<GitRepository> localRepos, IList<Project> projects)
        {
            List<string> dataSourceErrorProjects = new List<string>();
            Dictionary<string, IList<Repo>> projectRepos
                = new Dictionary<string, IList<Repo>>(StringComparer.OrdinalIgnoreCase);
            foreach (var localGitRepo in localRepos)
            {
                IList<string> remoteUrls = await localGitRepo.GetRemotesUrls();
                foreach (var url in remoteUrls)
                {
                    string projectId = CsrUtils.ParseProjectId(url);
                    var project = projects.FirstOrDefault(x => x.ProjectId == projectId);
                    if (String.IsNullOrWhiteSpace(projectId) || project == null)
                    {
                        continue;
                    }

                    try
                    {
                        var cloudRepo = await TryGetCloudRepoAsync(url, projectId, projectRepos);
                        if (cloudRepo == null)
                        {
                            Debug.WriteLine($"{projectId} repos does not contain {url}");
                            continue;
                        }
                        Repositories.Add(new RepoItemViewModel(cloudRepo, localGitRepo.Root));
                        break;
                    }
                    catch (DataSourceException)
                    {
                        dataSourceErrorProjects.Add(project.Name);
                    }
                }
            }

            if (dataSourceErrorProjects.Any())
            {
                UserPromptUtils.ErrorPrompt(
                    message: String.Format(
                        Resources.CsrFetchReposErrorMessage, String.Join(", ", dataSourceErrorProjects)),
                    title: Resources.CsrConnectSectionTitle);
            }
        }

        private async Task<Repo> TryGetCloudRepoAsync(
            string url,
            string projectId,
            Dictionary<string, IList<Repo>> projectReposMap)
        {
            IList<Repo> cloudRepos;
            Debug.WriteLine($"Check project id {projectId}");
            if (!projectReposMap.TryGetValue(projectId, out cloudRepos))
            {
                try
                {
                    cloudRepos = await CsrUtils.GetCloudReposAsync(projectId);
                    projectReposMap.Add(projectId, cloudRepos);
                }
                catch (DataSourceException)
                {
                    projectReposMap.Add(projectId, null);
                    throw;
                }
            }

            if (cloudRepos == null || !cloudRepos.Any())
            {
                Debug.WriteLine($"{projectId} has no repos found");
                return null;
            }

            return cloudRepos.FirstOrDefault(
                x => String.Compare(x.Url, url, StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        /// The list of local git repositories that Visual Studio remembers.
        /// </summary>
        /// <returns>
        /// A list of local repositories.
        /// Empty list is returned, never return null.
        /// </returns>
        private async Task<List<GitRepository>> GetLocalGitRepositoriesAsync()
        {
            List<GitRepository> localRepos = new List<GitRepository>();
            var repos = VsGitData.GetLocalRepositories(GoogleCloudExtensionPackage.Instance.VsVersion);
            if (repos != null)
            {
                var localRepoTasks = repos.Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(GitRepository.GetGitCommandWrapperForPathAsync);
                localRepos.AddRange((await Task.WhenAll(localRepoTasks)).Where(r => r != null));
            }
            return localRepos;
        }

        private async Task CloneCreateRepoAsync()
        {
            var projects = await GetProjectsAsync();
            if (!projects.Any())
            {
                return;
            }

            var result = CsrCloneWindow.PromptUser(projects);
            if (result == null)
            {
                return;
            }

            var repoItem = result.RepoItem;
            Repositories.Add(repoItem);

            // Created a new repo and cloned locally
            if (result.JustCreatedRepo)
            {
                var msg = String.Format(Resources.CsrCreateRepoNotificationFormat, repoItem.Name, repoItem.LocalPath);
                _teamExplorer.ShowMessage(msg,
                    command: new ProtectedCommand(() =>
                    {
                        SetRepoActive(repoItem);
                        ShellUtils.Default.LaunchCreateSolutionDialog(repoItem.LocalPath);
                        _teamExplorer.ShowHomeSection();
                    }));
            }
            else
            {
                var msg = String.Format(Resources.CsrCloneRepoNotificationFormat, repoItem.Name, repoItem.LocalPath);
                _teamExplorer.ShowMessage(msg,
                    command: new ProtectedCommand(() =>
                    {
                        SetRepoActive(repoItem);
                        _teamExplorer.ShowHomeSection();
                    }));
            }
        }

        /// <summary>
        /// Return a list of projects. Returns empty list if no item is found.
        /// </summary>
        private async Task<IList<Project>> GetProjectsAsync()
        {
            ResourceManagerDataSource resourceManager = DataSourceFactory.Default.CreateResourceManagerDataSource();
            if (resourceManager == null)
            {
                return new List<Project>();
            }

            IsReady = false;
            try
            {
                var projects = await resourceManager.GetProjectsListAsync();
                if (!projects.Any())
                {
                    UserPromptUtils.OkPrompt(
                        message: Resources.CsrNoProjectMessage,
                        title: Resources.CsrConnectSectionTitle);
                }
                return projects;
            }
            finally
            {
                IsReady = true;
            }
        }
    }
}
