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
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model for user control CsrCreateWindowContent.xaml.
    /// </summary>
    public class CsrCreateWindowViewModel : CsrCloneCreateViewModelBase
    {
        private string _repoName;
        private bool _gotoCsrWebPage = true;

        public bool GotoCsrWebPage
        {
            get { return _gotoCsrWebPage; }
            set { SetValueAndRaise(ref _gotoCsrWebPage, value); }
        }

        public string RepositoryName
        {
            get { return _repoName; }
            set
            {
                SetValueAndRaise(ref _repoName, value);
                ValidateInputs();
            }
        }

        protected override string RepoName => RepositoryName;

        public override ProtectedCommand OkCommand { get; }

        public int REPO_NAME_LEN => 128;

        public CsrCreateWindowViewModel(CsrCreateWindow owner, IList<Project> projects) : base(owner, projects)
        {
            OkCommand = new ProtectedAsyncCommand(() => ExecuteAsync(CreateAsync), canExecuteCommand: false);
        }

        private async Task CreateAsync()
        {
            var csrDatasource = CsrUtils.CreateCsrDataSource(SelectedProject.ProjectId);

            Repo cloudRepo = null;
            try
            {
                cloudRepo = await csrDatasource.CreateRepoAsync(RepositoryName);
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: ex.Message,
                    title: "Create repository");
                return;
            }

            // In success, CloneAsync set Result, close dialog.
            await CloneAsync(cloudRepo);
            if (base.Result != null && GotoCsrWebPage)
            {
                string fmt = "https://console.cloud.google.com/code/develop/browse/{0}?project={1}";
                string url = String.Format(fmt, RepositoryName, SelectedProject.ProjectId);
                Process.Start(url);
            }
        }

        protected override void ValidateInputs()
        {
            SetValidationResults(ValidateRepoName(RepositoryName), nameof(RepositoryName));
            // Note: Call the base ValidateInputs after ValidateRepoName.
            base.ValidateInputs();
        }

        private static readonly HashSet<char> REPO_NAME_FIRST_CHAR = new HashSet<char>();
        //    CharMatcher.inRange('A', 'Z')
        //        .or(CharMatcher.inRange('a', 'z'))
        //        .or(CharMatcher.inRange('0', '9'))
        //        .or(CharMatcher.is('_'));
        private static HashSet<char> REPO_NAME_MATCHER;

        private static void AddCharRange(char low, char high)
        {
            for (char ch = low; ch <= high; ++ch)
            {
                REPO_NAME_FIRST_CHAR.Add(ch);
            }
        }

        private IEnumerable<ValidationResult> ValidateRepoName(string name)
        {
            if (!REPO_NAME_FIRST_CHAR.Any())
            {
                AddCharRange('a', 'z');
                AddCharRange('A', 'Z');
                AddCharRange('0', '9');
                REPO_NAME_FIRST_CHAR.Add('_');
                REPO_NAME_MATCHER = new HashSet<char>(REPO_NAME_FIRST_CHAR);
                REPO_NAME_MATCHER.Add('-');
            }

            if (String.IsNullOrWhiteSpace(name))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValdiationNotEmptyMessage), 
                    Resources.CsrCreateRepoNameTextBoxLebel);
                yield break;
            }

            if (!REPO_NAME_FIRST_CHAR.Contains(name[0]))
            { 
                yield return StringValidationResult.FromResource(nameof(Resources.CsrRepoNameStartWithMessageFormat), name);
                yield break;
            }

            if (name.Skip(1).Any(x => !REPO_NAME_MATCHER.Contains(x)))
            {
                yield return StringValidationResult.FromResource(nameof(Resources.CsrCreateRepoNameValidationMessage), name);
                yield break;
            }
        }
    }
}
