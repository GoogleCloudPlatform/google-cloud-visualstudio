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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.GitUtils;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.TeamExplorerExtension;
using GoogleCloudExtension.Utils;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model to <seealso cref="CsrSectionControl"/>.
    /// </summary>
    [Export(typeof(ISectionViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CsrSectionControlViewModel : ViewModelBase, ISectionViewModel
    {
        /// Sometimes, the view and view model is recreated by Team Explorer.
        /// This is to preserve the states when a new user control is created.
        private static string s_currentAccount;
        private static bool s_gitInited;

        private ITeamExplorerUtils _teamExplorerService;
        private readonly CsrReposContent _reposContent = new CsrReposContent();
        private readonly CsrUnconnectedContent _unconnectedContent = new CsrUnconnectedContent();
        private readonly CsrGitSetupWarningContent _gitSetupContent = new CsrGitSetupWarningContent();
        private CsrReposViewModel _reposViewModel;
        private CsrUnconnectedViewModel _unconnectedViewModel;
        private CsrGitSetupWarningViewModel _gitSetupViewModel;
        private ContentControl _content;
        private EventHandler _accountChangedHandler;

        /// <summary>
        /// The content for the section control.
        /// </summary>
        public ContentControl Content
        {
            get { return _content; }
            private set { SetValueAndRaise(ref _content, value); }
        }

        [ImportingConstructor]
        public CsrSectionControlViewModel()
        {
            Debug.WriteLine("new CsrSectionControlViewModel");
        }

        /// <summary>
        /// Display the unconnected view
        /// </summary>
        public void ShowUnconnectedView()
        {
            Content = _unconnectedContent;
            s_currentAccount = null;
        }

        /// <summary>
        /// Switch to connected view
        /// </summary>
        public void SignIn()
        {
            if (CredentialsStore.Default.CurrentAccount == null)
            {
                ManageAccountsWindow.PromptUser();
            }
            Refresh();
        }

        /// <summary>
        /// Continue on by showing unconnected or connected view,
        /// after it checks git is installed.
        /// </summary>
        public void OnGitInstallationCheckSuccess()
        {
            ErrorHandlerUtils.HandleAsyncExceptions(() => InitializeGitAsync(_teamExplorerService));

            _accountChangedHandler = (sender, e) => OnAccountChanged();
            CredentialsStore.Default.CurrentAccountChanged += _accountChangedHandler;
            CredentialsStore.Default.Reset += _accountChangedHandler;

            if (CredentialsStore.Default.CurrentAccount == null)
            {
                ShowUnconnectedView();
            }
            else
            {
                Content = _reposContent;
                if (s_currentAccount != CredentialsStore.Default.CurrentAccount?.AccountName)
                {
                    SetGitCredential(_teamExplorerService);
                    // Continue regardless of whether SetGitCredential succeeds or not.

                    _reposViewModel.Refresh();
                    s_currentAccount = CredentialsStore.Default.CurrentAccount?.AccountName;
                }
            }
        }

        #region implement interface ISectionViewModel

        /// <summary>
        /// Implicit implementation to ISectionViewModel.Refresh. 
        /// Using implicit declaration so that it can be accessed by 'this' object too.
        /// </summary>
        public void Refresh()
        {
            Debug.WriteLine("CsrSectionControlViewModel.Refresh");

            if (!CsrGitSetupWarningViewModel.GitInstallationVerified)
            {
                ErrorHandlerUtils.HandleAsyncExceptions(CheckGitInstallationAsync);
            }
            else if (CredentialsStore.Default.CurrentAccount == null)
            {
                ShowUnconnectedView();
            }
            else
            {
                ErrorHandlerUtils.HandleAsyncExceptions(() => InitializeGitAsync(_teamExplorerService));

                Content = _reposContent;
                s_currentAccount = CredentialsStore.Default.CurrentAccount?.AccountName;
                _reposViewModel.Refresh();
            }
        }

        void ISectionViewModel.Initialize(ITeamExplorerUtils teamExplorerService)
        {
            EventsReporterWrapper.ReportEvent(CsrConnectSectionOpenEvent.Create());
            Debug.WriteLine("CsrSectionControlViewModel Initialize");

            _teamExplorerService = teamExplorerService.ThrowIfNull(nameof(teamExplorerService));
            _reposViewModel = new CsrReposViewModel(_teamExplorerService);
            _reposContent.DataContext = _reposViewModel;
            _unconnectedViewModel = new CsrUnconnectedViewModel(this);
            _unconnectedContent.DataContext = _unconnectedViewModel;
            _gitSetupViewModel = new CsrGitSetupWarningViewModel(this);
            _gitSetupContent.DataContext = _gitSetupViewModel;

            if (!CsrGitSetupWarningViewModel.GitInstallationVerified)
            {
                ErrorHandlerUtils.HandleAsyncExceptions(CheckGitInstallationAsync);
            }
            else
            {
                OnGitInstallationCheckSuccess();
            }
        }

        void ISectionViewModel.UpdateActiveRepo(string newRepoLocalPath)
        {
            Debug.WriteLine($"CsrSectionControlViewModel.UpdateActiveRepo {newRepoLocalPath}");
            _reposViewModel.ShowActiveRepo(newRepoLocalPath);
        }

        void ISectionViewModel.Cleanup()
        {
            if (_accountChangedHandler != null)
            {
                CredentialsStore.Default.Reset -= _accountChangedHandler;
                CredentialsStore.Default.CurrentAccountChanged -= _accountChangedHandler;
            }
        }

        #endregion

        private async Task CheckGitInstallationAsync()
        {
            if (await _gitSetupViewModel.CheckInstallationAsync())
            {
                OnGitInstallationCheckSuccess();
            }
            else
            {
                Content = _gitSetupContent;
            }
        }

        private void OnAccountChanged()
        {
            if (s_currentAccount == CredentialsStore.Default.CurrentAccount?.AccountName)
            {
                return;
            }
            SetGitCredential(_teamExplorerService);
            Refresh();
        }

        private static async Task<bool> InitializeGitAsync(ITeamExplorerUtils teamExplorer)
        {
            if (s_gitInited)
            {
                return true;
            }

            return s_gitInited = (await SetUseHttpPathAsync(teamExplorer)) && SetGitCredential(teamExplorer);
        }

        private static async Task<bool> SetUseHttpPathAsync(ITeamExplorerUtils teamExplorer)
        {
            bool ret;
            try
            {
                await CsrGitUtils.SetUseHttpPathAsync();
                ret = true;
            }
            catch (GitCommandException)
            {
                ret = false;
            }
            if (!ret)
            {
                teamExplorer.ShowError(Resources.GitInitilizationFailedMessage);
            }
            return ret;
        }

        private static bool SetGitCredential(ITeamExplorerUtils teamExplorer)
        {
            if (CredentialsStore.Default.CurrentAccount != null)
            {
                if (!CsrGitUtils.StoreCredential(
                    CsrGitUtils.CsrUrlAuthority,
                    CredentialsStore.Default.CurrentAccount.RefreshToken,
                    CsrGitUtils.StoreCredentialPathOption.UrlHost))
                {
                    teamExplorer.ShowError(Resources.GitInitilizationFailedMessage);
                    return s_gitInited = false;
                }
            }
            return true;
        }
    }
}
