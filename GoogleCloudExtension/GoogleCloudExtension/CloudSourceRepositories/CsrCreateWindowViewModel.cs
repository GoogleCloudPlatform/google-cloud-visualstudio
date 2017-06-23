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
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model for user control CsrCreateWindowContent.xaml.
    /// </summary>
    public class CsrCreateWindowViewModel : CsrCloneCreateViewModelBase
    {
        private static readonly Lazy<HashSet<char>> s_repoNameFirstCharSet = 
            new Lazy<HashSet<char>>(CreateRepoNameFistCharSet);
        private static readonly Lazy<HashSet<char>> 
            s_repoNmaeCharSet = new Lazy<HashSet<char>>(CreateRepoNameCharSet);

        private string _repositoryName;
        private bool _gotoCsrWebPage = true;

        protected override string RepoName => RepositoryName;

        /// <summary>
        /// Gets the repo name max length
        /// </summary>
        public int RepoNameMaxLength => 128;

        /// <summary>
        /// Responds to Ok button command
        /// </summary>
        public override ProtectedCommand OkCommand { get; }

        /// <summary>
        /// Get whether go to GCP console after creation
        /// </summary>
        public bool GotoCsrWebPage
        {
            get { return _gotoCsrWebPage; }
            set { SetValueAndRaise(ref _gotoCsrWebPage, value); }
        }

        /// <summary>
        /// Gets repository name
        /// </summary>
        public string RepositoryName
        {
            get { return _repositoryName; }
            set
            {
                SetValueAndRaise(ref _repositoryName, value);
                ValidateInputs();
            }
        }

        public CsrCreateWindowViewModel(CsrCreateWindow owner, IList<Project> projects) : base(owner, projects)
        {
            OkCommand = new ProtectedAsyncCommand(() => ExecuteAsync(CreateAsync), canExecuteCommand: false);
        }

        protected override void ValidateInputs()
        {
            SetValidationResults(ValidateRepoName(), nameof(RepositoryName));
            // Note: Call the base ValidateInputs after ValidateRepoName. Order matters.
            base.ValidateInputs();
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
                    title: Resources.CsrCreateWindowTitle);
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

        private IEnumerable<ValidationResult> ValidateRepoName()
        {
            if (String.IsNullOrWhiteSpace(RepositoryName))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValdiationNotEmptyMessage), 
                    Resources.CsrCreateRepoNameTextBoxLabel);
                yield break;
            }

            if (!s_repoNameFirstCharSet.Value.Contains(RepositoryName[0]))
            { 
                yield return StringValidationResult.FromResource(nameof(Resources.CsrRepoNameStartWithMessageFormat));
                yield break;
            }

            if (RepositoryName.Any(x => !s_repoNmaeCharSet.Value.Contains(x)))
            {
                yield return StringValidationResult.FromResource(nameof(Resources.CsrRepoNameValidationMessage));
                yield break;
            }
        }

        private static void AddCharRange(HashSet<char> set, char low, char high)
        {
            for (char ch = low; ch <= high; ++ch)
            {
                set.Add(ch);
            }
        }

        private static HashSet<char> CreateRepoNameFistCharSet()
        {
            var set = new HashSet<char>();
            AddCharRange(set, 'a', 'z');
            AddCharRange(set, 'A', 'Z');
            AddCharRange(set, '0', '9');
            set.Add('_');
            return set;
        }

        private static HashSet<char> CreateRepoNameCharSet()
        {
            var set = new HashSet<char>(s_repoNameFirstCharSet.Value);
            set.Add('-');
            return set;
        }
    }
}
