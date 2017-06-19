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
        private static ObservableCollection<RepoItemViewModel> s_repoList;
        private static RepoItemViewModel s_activeRepo;

        private readonly ITeamExplorerUtils _teamExplorer;
        private bool _isReady = true;
        private RepoItemViewModel _selectedRepo;

        /// <summary>
        /// Gets the current active Repo
        /// </summary>
        static public RepoItemViewModel ActiveRepo
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
            CloneCommand = new ProtectedCommand(Clone);
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

                // Note, the order is critical.
                // When switching to HomeSection, current "this" object is destroyed.
                ActiveRepo = SelectedRepository;
                _teamExplorer.ShowHomeSection();
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
            // TODO: Add in next PR.
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
            if (Loading)
            {
                return;
            }

            var resourceManager = DataSourceFactories.CreateResourceManagerDataSource();
            if (resourceManager == null)
            {
                return;
            }

            IsReady = false;
            Loading = true;
            Repositories = new ObservableCollection<RepoItemViewModel>();
            
            try
            {
                var projects = await resourceManager.GetSortedActiveProjectsAsync();
                if (!projects.Any())
                {
                    return;
                }

                await AddLocalReposAsync(await GetLocalGitRepositories());

                SetActiveRepo(_teamExplorer.GetActiveRepository());
            }
            finally
            {
                Loading = false;
                IsReady = true;
            }
        }

        /// <summary>
        /// projectRepos is used to cache the list of 'cloud repos' of the project-id.
        /// </summary>
        private async Task AddLocalReposAsync(IList<GitRepository> localRepos)
        {
            Dictionary<string, IList<Repo>> projectRepos
                = new Dictionary<string, IList<Repo>>(StringComparer.OrdinalIgnoreCase);
            foreach (var localGitRepo in localRepos)
            {
                IList<string> remoteUrls = await localGitRepo.GetRemotesUrls();
                foreach (var url in remoteUrls)
                {
                    string projectId = CsrUtils.ParseProjectId(url);
                    if (String.IsNullOrWhiteSpace(projectId))
                    {
                        continue;
                    }

                    var cloudRepo = await TryGetCloudRepoAsync(url, projectId, projectRepos);
                    if (cloudRepo == null)
                    {
                        Debug.WriteLine($"{projectId} repos does not contain {url}");
                        continue;
                    }
                    Repositories.Add(new RepoItemViewModel(cloudRepo, localGitRepo.Root));
                    break;
                }
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

                cloudRepos = await CsrUtils.GetCloudReposAsync(projectId);
                projectReposMap.Add(projectId, cloudRepos);
            }

            if (!(cloudRepos != null && cloudRepos.Any()))
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

        private void Clone()
        {
            var repoItem = CsrCloneWindow.PromptUser();
            if (repoItem != null)
            {
                (Repositories ?? new ObservableCollection<RepoItemViewModel>()).Add(repoItem);
            }
        }
    }
}
