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
    public class DeploymentDialogViewModel : Model
    {
        private readonly DeploymentDialogWindow _window;

        // Default values to show while loading data.
        private readonly IList<string> _loadingAccounts = new List<string> { "Loading..." };
        private readonly IList<CloudProject> _loadingProjects = new List<CloudProject> { new CloudProject { Name = "Loading..." } };


        /// <summary>
        /// The project that will be deployed.
        /// </summary>
        private string _Project;
        public string Project
        {
            get { return _Project; }
            set { SetValueAndRaise(ref _Project, value); }
        }

        /// <summary>
        /// The list of cloud projects available to deploy the code.
        /// </summary>
        private IList<CloudProject> _CloudProjects;
        public IList<CloudProject> CloudProjects
        {
            get { return _CloudProjects; }
            set { SetValueAndRaise(ref _CloudProjects, value); }
        }

        /// <summary>
        /// The selected cloud project where the code is going to be deployed.
        /// </summary>
        private CloudProject _SelectedCloudProject;
        public CloudProject SelectedCloudProject
        {
            get { return _SelectedCloudProject; }
            set { SetValueAndRaise(ref _SelectedCloudProject, value); }
        }

        /// <summary>
        /// The list of accounts avialabe to use as credentials for the deployment.
        /// </summary>
        private IEnumerable<string> _Accounts;
        public IEnumerable<string> Accounts
        {
            get { return _Accounts; }
            set { SetValueAndRaise(ref _Accounts, value); }
        }

        /// <summary>
        /// The selected account for deployment.
        /// </summary>
        private string _SelectedAccount;
        public string SelectedAccount
        {
            get { return _SelectedAccount; }
            set
            {
                SetValueAndRaise(ref _SelectedAccount, value);
                InvalidateSelectedAccount();
            }
        }

        /// <summary>
        /// The list of supported runtimes by the project being deployed.
        /// </summary>
        private IList<DnxRuntime> _SupportedRuntimes;
        public IList<DnxRuntime> SupportedRuntimes
        {
            get { return _SupportedRuntimes; }
            set { SetValueAndRaise(ref _SupportedRuntimes, value); }
        }

        /// <summary>
        /// The selected runtime to use for the deployment.
        /// </summary>
        private DnxRuntime _SelectedRuntime;
        public DnxRuntime SelectedRuntime
        {
            get { return _SelectedRuntime; }
            set { SetValueAndRaise(ref _SelectedRuntime, value); }
        }

        /// <summary>
        /// Wether all of the data is loaded and the dialog is ready to be used.
        /// </summary>
        private bool _Loaded;
        public bool Loaded
        {
            get { return _Loaded; }
            set { SetValueAndRaise(ref _Loaded, value); }
        }

        /// <summary>
        /// Whether the deployed version is to be made the default version.
        /// </summary>
        private bool _MakeDefault;
        public bool MakeDefault
        {
            get { return _MakeDefault; }
            set { SetValueAndRaise(ref _MakeDefault, value); }
        }

        /// <summary>
        /// The version name to use.
        /// </summary>
        private string _VersionName;
        public string VersionName
        {
            get { return _VersionName; }
            set { SetValueAndRaise(ref _VersionName, value); }
        }

        /// <summary>
        /// The command to invoke to start the deployment.
        /// </summary>
        public ICommand DeployCommand { get; private set; }

        /// <summary>
        /// The command to invoke to cancel the deployment dialog and close it.
        /// </summary>
        public ICommand CancelCommand { get; private set; }

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
                var accountAndProject = await GCloudWrapper.Instance.GetCurrentAccountAndProjectAsync();

                Accounts = accounts;
                _SelectedAccount = accountAndProject.Account; // Update the selected account without invalidating it.
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

        #region Commands

        private void OnDeployHandler(object param)
        {
            DeploymentUtils.DeployProjectAsync(
                   startupProject: _window.Options.Project,
                   projects: _window.Options.ProjectsToRestore,
                   selectedRuntime: SelectedRuntime,
                   versionName: VersionName,
                   makeDefault: MakeDefault,
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
                _SelectedCloudProject = null;

                var cloudProjects = await GCloudWrapper.Instance.GetProjectsAsync(
                    new Credentials(account: this.SelectedAccount));

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
