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
        private static ObservableCollection<RepoItemViewModel> s_repoList;

        private readonly ITeamExplorerUtils _teamExplorer;
        private RepoItemViewModel _activeRepo;
        private bool _isReady = true;
        private RepoItemViewModel _selectedRepo;

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
            private set { SetValueAndRaise(ref s_repoList, value); }
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
        /// Gets the current active Repo
        /// </summary>
        public RepoItemViewModel ActiveRepo
        {
            get { return _activeRepo; }
            set
            {
                if (value != _activeRepo && _activeRepo != null)
                {
                    _activeRepo.IsActiveRepo = false;
                }

                if (value != null)
                {
                    value.IsActiveRepo = true;
                }

                _activeRepo = value;
            }
        }

        /// <summary>
        /// Responds to Clone command
        /// </summary>
        public ICommand CloneCommand { get; }

        /// <summary>
        /// Responds to Create command.
        /// </summary>
        public ICommand CreateCommand { get; }

        /// <summary>
        /// Responds to Disconnect command
        /// </summary>
        public ICommand DisconnectCommand { get; }

        /// <summary>
        /// Responds to list view double click event
        /// </summary>
        public ICommand ListDoubleClickCommand { get; }

        public CsrReposViewModel(CsrSectionControlViewModel parent, ITeamExplorerUtils teamExplorer)
        {
            parent.ThrowIfNull(nameof(parent));
            _teamExplorer = teamExplorer.ThrowIfNull(nameof(teamExplorer));
            DisconnectCommand = new ProtectedCommand(parent.Disconnect);
            ListDoubleClickCommand = new ProtectedCommand(SetSelectedRepoActive);
        }

        /// <summary>
        /// Reload the repository list.
        /// </summary>
        public void Refresh()
        {
            ErrorHandlerUtils.HandleAsyncExceptions(ListRepositoryAsync);
        }

        /// <summary>
        /// When user double clicks at a repository, set it as active repo.
        /// </summary>
        public void SetSelectedRepoActive()
        {
            if (SelectedRepository?.IsActiveRepo == false)
            {
                SetCurrentRepo(SelectedRepository.LocalPath);
                _teamExplorer.ShowHomeSection();
                ActiveRepo = SelectedRepository;
            }
        }

        /// <summary>
        /// Set a repository as active, show the item in Bold font.
        /// </summary>
        /// <param name="localPath">The repository local path</param>
        public void SetActiveRepo(string localPath)
        {
            var repoItem = Repositories?.FirstOrDefault(
                x => String.Compare(x.LocalPath, localPath, StringComparison.OrdinalIgnoreCase) == 0);
            ActiveRepo = repoItem;
        }

        private void SetCurrentRepo(string localPath)
        {
            string guid = Guid.NewGuid().ToString();
            try
            {
                ShellUtils.CreateEmptySolution(localPath, guid);
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
        /// Get a list of local repositories.  It is saved to local localRepos.
        /// For each local repository, get the URL.
        /// From the URL, get the project-id. 
        /// Now, checkes if the list of repos under the project-id contains the URL.
        /// If it does, add it to Repositories.
        /// 
        /// projectReposDictionay is used to cache the list of repos of the project-id.
        /// </summary>
        private async Task ListRepositoryAsync()
        {
            if (Loading)
            {
                return;
            }

            IsReady = false;
            Loading = true;
            Repositories = new ObservableCollection<RepoItemViewModel>();
            Dictionary<string, IList<Repo>> projectRepos 
                = new Dictionary<string, IList<Repo>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var projects = await CsrUtils.GetProjectsAsync();

                if (!(projects?.Any() ?? false))
                {
                    return;
                }

                var localRepos = await GetLocalGitRepositories();
                foreach (var localGitRepo in localRepos)
                {
                    string url = await localGitRepo.GetRemoteUrl();
                    if (String.IsNullOrWhiteSpace(url))
                    {
                        Debug.WriteLine($"{localGitRepo.Root} does not get remote url");
                        continue;
                    }
                    string projectId = CsrUtils.ParseProjectId(url);
                    IList<Repo> cloudRepos = null;
                    Project project = null;
                    if (String.IsNullOrWhiteSpace(projectId))
                    {
                        continue;
                    }
                    Debug.WriteLine($"Check project id {projectId}");
                    if (!projectRepos.TryGetValue(projectId, out cloudRepos))
                    {
                        project = projects.FirstOrDefault(
                            x => String.Compare(x.ProjectId, projectId, StringComparison.OrdinalIgnoreCase) == 0);
                        if (project == null)
                        {
                            Debug.WriteLine($"{projectId} is invalid or unknown project id");
                            continue;
                        }
                        cloudRepos = await CsrUtils.GetCloudReposAsync(project);
                        projectRepos.Add(projectId, cloudRepos);
                    }

                    if (!(cloudRepos?.Any() ?? false))
                    {
                        Debug.WriteLine($"{projectId} has no repos found");
                        continue;
                    }

                    var cloud = cloudRepos.FirstOrDefault(
                        x => String.Compare(x.Url, url, StringComparison.OrdinalIgnoreCase) == 0);
                    if (cloud == null)
                    {
                        Debug.WriteLine($"{projectId} repos does not contain {url}");
                        continue;
                    }
                    Repositories.Add(new RepoItemViewModel(cloud, localGitRepo));
                }

                SetActiveRepo(_teamExplorer.GetActiveRepository());
            }
            finally
            {
                Loading = false;
                IsReady = true;
            }
        }

        /// <summary>
        /// The list of local git repositories that Visual Studio remembers.
        /// </summary>
        private async Task<List<GitRepository>> GetLocalGitRepositories()
        {
            List<GitRepository> localRepos = new List<GitRepository>();
            var repos = VsGitData.GetLocalRepositories(GoogleCloudExtensionPackage.VsVersion);
            if (repos != null)
            {
                // Not using Linq because it's tricky to use await inside Linq.
                foreach (var path in repos)
                {
                    if (String.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }
                    var git = await GitRepository.GetGitCommandWrapperForPathAsync(path);
                    if (git != null)
                    {
                        localRepos.Add(git);
                    }
                }
            }
            return localRepos;
        }
    }
}
