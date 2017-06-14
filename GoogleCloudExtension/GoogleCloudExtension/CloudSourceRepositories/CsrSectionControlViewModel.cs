﻿// Copyright 2017 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.TeamExplorerExtension;
using GoogleCloudExtension.Utils;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
        private static bool s_isConnected = false;
        private static string s_currentAccount;

        private readonly CsrReposContent _reposContent = new CsrReposContent();
        private readonly CsrUnconnectedContent _unconnectedContent = new CsrUnconnectedContent();
        private CsrReposViewModel _reposViewModel;
        private CsrUnconnectedViewModel _unconnectedViewModel;
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
        public void Disconnect()
        {
            Content = _unconnectedContent;
            s_isConnected = false;
            s_currentAccount = null;
        }

        /// <summary>
        /// Switch to connected view
        /// </summary>
        public void Connect()
        {
            if (s_isConnected)
            {
                return;
            }
            if (CredentialsStore.Default.CurrentAccount == null)
            {
                ManageAccountsWindow.PromptUser();
            }
            if (CredentialsStore.Default.CurrentAccount != null)
            {
                Content = _reposContent;
                s_isConnected = true;
                Refresh();
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
            if (CredentialsStore.Default.CurrentAccount == null)
            {
                Disconnect();
            }
            else if (s_isConnected)
            {
                s_currentAccount = CredentialsStore.Default.CurrentAccount?.AccountName;
                _reposViewModel.Refresh();
            }
        }

        void ISectionViewModel.Initialize(ITeamExplorerUtils teamExplorerService)
        {
            Debug.WriteLine("CsrSectionControlViewModel Initialize");
            teamExplorerService.ThrowIfNull(nameof(teamExplorerService));
            _reposViewModel = new CsrReposViewModel(this, teamExplorerService);
            _unconnectedViewModel = new CsrUnconnectedViewModel(this);
            _reposContent.DataContext = _reposViewModel;
            _unconnectedContent.DataContext = _unconnectedViewModel;

            _accountChangedHandler = (sender, e) => OnAccountChanged();
            CredentialsStore.Default.CurrentAccountChanged += _accountChangedHandler;
            CredentialsStore.Default.Reset += _accountChangedHandler;

            if (s_isConnected && CredentialsStore.Default.CurrentAccount != null)
            {
                Content = _reposContent;
                if (s_currentAccount != CredentialsStore.Default.CurrentAccount?.AccountName)
                {
                    _reposViewModel.Refresh();
                }
                s_currentAccount = CredentialsStore.Default.CurrentAccount?.AccountName;
            }
            else
            {
                Disconnect();
            }
        }

        void ISectionViewModel.UpdateActiveRepo(string newRepoLocalPath)
        {
            Debug.WriteLine($"CsrSectionControlViewModel.UpdateActiveRepo {newRepoLocalPath}");
            _reposViewModel.SetActiveRepo(newRepoLocalPath);
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

        private void OnAccountChanged()
        {
            if (s_isConnected && s_currentAccount == CredentialsStore.Default.CurrentAccount?.AccountName)
            {
                return;
            }
            Refresh();
        }
    }
}
