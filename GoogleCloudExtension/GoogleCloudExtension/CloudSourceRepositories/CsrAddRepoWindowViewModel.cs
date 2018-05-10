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
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model for user control CsrAddRepoWindowContent.xaml.
    /// </summary>
    public class CsrAddRepoWindowViewModel : ValidatingViewModelBase
    {
        private static readonly Regex s_repoNameRegex = new Regex("^[a-zA-Z0-9_-]*$");
        private const char RepoNameExcludingCharacter = '-';
        private readonly IList<Repo> _repos;
        private readonly Project _project;
        private readonly CsrAddRepoWindow _owner;
        private string _repoName;
        private bool _isReady = true;

        private string CsrConsoleLink =>
            $"https://console.cloud.google.com/code/develop/repo?project={_project.ProjectId}";

        /// <summary>
        /// Limit the input box length to 63 characters
        /// </summary>
        public int NameMaxLength => 63;

        /// <summary>
        /// The repository name 
        /// </summary>
        public string RepositoryName
        {
            get { return _repoName; }
            set
            {
                SetValueAndRaise(ref _repoName, value);
                ValidateInputs();
            }
        }

        /// <summary>
        /// Add repo for the project message
        /// </summary>
        public string AddRepoForProjectMessage =>
            String.Format(Resources.CsrAddRepoForProjectMessageFormat, _project.Name);

        /// <summary>
        /// Indicates if there is async task running that UI should be disabled.
        /// </summary>
        public bool IsReady
        {
            get { return _isReady; }
            set { SetValueAndRaise(ref _isReady, value); }
        }

        /// <summary>
        /// Responds to OK button click event
        /// </summary>
        public ProtectedCommand OkCommand { get; }

        /// <summary>
        /// Responds console link command
        /// </summary>
        public ProtectedCommand CsrLinkCommand { get; }

        /// <summary>
        /// The added new repo
        /// </summary>
        public Repo Result { get; private set; }

        public CsrAddRepoWindowViewModel(CsrAddRepoWindow owner, IList<Repo> repos, Project project)
        {
            _owner = owner.ThrowIfNull(nameof(owner));
            _repos = repos.ThrowIfNull(nameof(repos));
            _project = project.ThrowIfNull(nameof(project));
            OkCommand = new ProtectedAsyncCommand(CreateRepoAsync, canExecuteCommand: false);
            CsrLinkCommand = new ProtectedCommand(() => Process.Start(CsrConsoleLink));
        }

        private async Task CreateRepoAsync()
        {
            var csrDatasource = CsrUtils.CreateCsrDataSource(_project.ProjectId);
            IsReady = false;
            try
            {
                var watch = Stopwatch.StartNew();
                // No null check. By the time user gets here, csrDatasource won't be null.
                Result = await csrDatasource.CreateRepoAsync(RepositoryName.Trim());
                EventsReporterWrapper.ReportEvent(
                    CsrCreatedEvent.Create(CommandStatus.Success, duration: watch.Elapsed));
                _owner.Close();
            }
            catch (Exception)
            {
                EventsReporterWrapper.ReportEvent(CsrCreatedEvent.Create(CommandStatus.Failure));
                throw;
            }
            finally
            {
                IsReady = true;
            }
        }

        private void ValidateInputs()
        {
            SetValidationResults(ValidateRepoName(), nameof(RepositoryName));
            // Note, we have only one text box input, no error when it's empty or null, just disable OK button.
            OkCommand.CanExecuteCommand = !(String.IsNullOrWhiteSpace(RepositoryName) || HasErrors);
        }

        internal IEnumerable<ValidationResult> ValidateRepoName()
        {
            string name = RepositoryName?.Trim();

            // Note, we have only one text box input, no error when it's empty or null
            if (String.IsNullOrWhiteSpace(name))
            {
                yield break;
            }

            if (!s_repoNameRegex.IsMatch(name))
            {
                yield return StringValidationResult.FromResource(nameof(Resources.CsrRepoNameRuleMessage));
            }

            if (name[0] == RepoNameExcludingCharacter)
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.CsrRepoNameFirstCharacterExtraRuleMessage));
            }

            if (name.Length < 3 || name.Length > NameMaxLength)
            {
                yield return StringValidationResult.FromResource(nameof(Resources.CsrRepoNameLengthLimitMessage));
            }

            if (_repos.Any(x => String.Compare(x.GetRepoName(), name, ignoreCase: true) == 0))
            {
                yield return StringValidationResult.FromResource(nameof(Resources.CsrRepoNameExistsMessage));
            }
        }
    }
}
