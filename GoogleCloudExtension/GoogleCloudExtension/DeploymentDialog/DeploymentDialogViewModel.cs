// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Dnx;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.DeploymentDialog
{
    /// <summary>
    /// This class is the view model for the deployment dialog.
    /// </summary>
    public class DeploymentDialogViewModel : ViewModelBase
    {
        private readonly DeploymentDialogWindow _window;

        // Default values to show while loading data.
        private readonly IList<string> _loadingAccounts = new List<string> { "Loading..." };
        private readonly IList<CloudProject> _loadingProjects = new List<CloudProject> { new CloudProject { Name = "Loading..." } };

        // Storage for the properties.
        private IList<CloudProject> _cloudProjects;
        private CloudProject _selectedCloudProject;
        private IEnumerable<string> _accounts;
        private string _selectedAccount;
        private DnxRuntime _selectedRuntime;
        private bool _loaded;
        private bool _makeDefault;
        private bool _preserveOutput;
        private string _versionName;

        /// <summary>
        /// The project that will be deployed.
        /// </summary>
        public string Project { get; }

        /// <summary>
        /// The list of supported runtimes by the project being deployed.
        /// </summary>
        public IList<DnxRuntime> SupportedRuntimes { get; }

        /// <summary>
        /// The list of cloud projects available to deploy the code.
        /// </summary>
        public IList<CloudProject> CloudProjects
        {
            get { return _cloudProjects; }
            set { SetValueAndRaise(ref _cloudProjects, value); }
        }

        /// <summary>
        /// The selected cloud project where the code is going to be deployed.
        /// </summary>
        public CloudProject SelectedCloudProject
        {
            get { return _selectedCloudProject; }
            set { SetValueAndRaise(ref _selectedCloudProject, value); }
        }

        /// <summary>
        /// The list of accounts avialabe to use as credentials for the deployment.
        /// </summary>
        public IEnumerable<string> Accounts
        {
            get { return _accounts; }
            set { SetValueAndRaise(ref _accounts, value); }
        }

        /// <summary>
        /// The selected account for deployment.
        /// </summary>
        public string SelectedAccount
        {
            get { return _selectedAccount; }
            set
            {
                SetValueAndRaise(ref _selectedAccount, value);
                InvalidateSelectedAccount();
            }
        }

        /// <summary>
        /// The selected runtime to use for the deployment.
        /// </summary>
        public DnxRuntime SelectedRuntime
        {
            get { return _selectedRuntime; }
            set { SetValueAndRaise(ref _selectedRuntime, value); }
        }

        /// <summary>
        /// Whether all of the data is loaded and the dialog is ready to be used.
        /// </summary>
        public bool Loaded
        {
            get { return _loaded; }
            set { SetValueAndRaise(ref _loaded, value); }
        }

        /// <summary>
        /// Whether the deployed version is to be made the default version.
        /// </summary>
        public bool MakeDefault
        {
            get { return _makeDefault; }
            set { SetValueAndRaise(ref _makeDefault, value); }
        }

        /// <summary>
        /// Whether the output should be preserved.
        /// </summary>
        public bool PreserveOutput
        {
            get { return _preserveOutput; }
            set { SetValueAndRaise(ref _preserveOutput, value); }
        }

        /// <summary>
        /// The version name to use.
        /// </summary>
        public string VersionName
        {
            get { return _versionName; }
            set { SetValueAndRaise(ref _versionName, value); }
        }

        /// <summary>
        /// The command to invoke to start the deployment.
        /// </summary>
        public ICommand DeployCommand { get; }

        /// <summary>
        /// The command to invoke to cancel the deployment dialog and close it.
        /// </summary>
        public ICommand CancelCommand { get; }

        public DeploymentDialogViewModel(DeploymentDialogWindow window)
        {
            this.DeployCommand = new WeakCommand(this.OnDeployHandler);
            this.CancelCommand = new WeakCommand(this.OnCancelHandler);
            this.Project = window.Options.Project.Name;
            this.SupportedRuntimes = window.Options.Project.SupportedRuntimes.ToList();
            this.SelectedRuntime = window.Options.Project.Runtime;
            _window = window;
        }

        public async void StartLoadingProjectsAsync()
        {
            Debug.WriteLine("Loading projects...");

            try
            {
                Loaded = false;
                Accounts = _loadingAccounts;
                CloudProjects = _loadingProjects;
                SelectedAccount = _loadingAccounts[0];
                SelectedCloudProject = _loadingProjects[0];

                var accounts = await GCloudWrapper.Instance.GetAccountsAsync();
                var cloudProjects = await GCloudWrapper.Instance.GetProjectsAsync();
                var accountAndProject = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();

                Accounts = accounts;
                _selectedAccount = accountAndProject.Account; // Update the selected account without invalidating it.
                RaisePropertyChanged(nameof(SelectedAccount));

                CloudProjects = cloudProjects;
                SelectedCloudProject = cloudProjects.Where(x => x.Id == accountAndProject.ProjectId).FirstOrDefault();

                Loaded = true;
                Debug.WriteLine("Projects loaded...");
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine("Failed to load list of projects to deploy.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();

                throw ex;
            }
        }

        #region Command handlers

        private void OnDeployHandler(object param)
        {
            DeploymentUtils.DeployProjectAsync(
                startupProject: _window.Options.Project,
                projects: _window.Options.ProjectsToRestore,
                selectedRuntime: SelectedRuntime,
                versionName: VersionName,
                makeDefault: MakeDefault,
                preserveOutput: PreserveOutput,
                accountAndProject: new Credentials(account: this.SelectedAccount, projectId: this.SelectedCloudProject.Id));
            _window.Close();
        }

        private void OnCancelHandler(object param)
        {
            _window.Close();
        }

        #endregion

        private async void InvalidateSelectedAccount()
        {
            if (!this.Loaded)
            {
                // We're still loading data, invalidation here does nothing.
                return;
            }

            try
            {
                this.Loaded = false;
                this.CloudProjects = null;
                _selectedCloudProject = null;

                var credentials = new Credentials(account: this.SelectedAccount);
                var cloudProjects = await GCloudWrapper.Instance.GetProjectsAsync(credentials);

                this.CloudProjects = cloudProjects;
                this.SelectedCloudProject = cloudProjects.FirstOrDefault();

                this.Loaded = true;
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine("Failed to fetch list of project.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
        }
    }
}
