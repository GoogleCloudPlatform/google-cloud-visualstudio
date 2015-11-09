// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Dnx;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.DeploymentDialog
{
    public class DeploymentDialogViewModel : Model
    {
        private string _Project;
        public string Project
        {
            get { return _Project; }
            set { SetValueAndRaise(ref _Project, value); }
        }

        private IList<GcpProject> _CloudProjects;
        public IList<GcpProject> CloudProjects
        {
            get { return _CloudProjects; }
            set { SetValueAndRaise(ref _CloudProjects, value); }
        }

        private GcpProject _SelectedCloudProject;
        public GcpProject SelectedCloudProject
        {
            get { return _SelectedCloudProject; }
            set { SetValueAndRaise(ref _SelectedCloudProject, value); }
        }

        private IList<string> _Accounts;
        public IList<string> Accounts
        {
            get { return _Accounts; }
            set { SetValueAndRaise(ref _Accounts, value); }
        }

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

        private IList<DnxRuntime> _SupportedRuntimes;
        public IList<DnxRuntime> SupportedRuntimes
        {
            get { return _SupportedRuntimes; }
            set { SetValueAndRaise(ref _SupportedRuntimes, value); }
        }

        private DnxRuntime _SelectedRuntime;
        public DnxRuntime SelectedRuntime
        {
            get { return _SelectedRuntime; }
            set { SetValueAndRaise(ref _SelectedRuntime, value); }
        }

        private bool _Loaded;
        public bool Loaded
        {
            get { return _Loaded; }
            set { SetValueAndRaise(ref _Loaded, value); }
        }

        private bool _MakeDefault;
        public bool MakeDefault
        {
            get { return _MakeDefault; }
            set { SetValueAndRaise(ref _MakeDefault, value); }
        }

        private string _VersionName;
        public string VersionName
        {
            get { return _VersionName; }
            set { SetValueAndRaise(ref _VersionName, value); }
        }

        private ICommand _DeployCommand;
        public ICommand DeployCommand
        {
            get { return _DeployCommand; }
            set { SetValueAndRaise(ref _DeployCommand, value); }
        }

        private ICommand _CancelCommand;
        public ICommand CancelCommand
        {
            get { return _CancelCommand; }
            set { SetValueAndRaise(ref _CancelCommand, value); }
        }

        public DeploymentDialogViewModel(DeploymentDialogWindow window)
        {
            this.DeployCommand = new WeakCommand(this.OnDeployHandler);
            this.CancelCommand = new WeakCommand(this.OnCancelHandler);
            this.Project = window.Options.Project.Name;
            this.SupportedRuntimes = window.Options.Project.SupportedRuntimes;
            this.SelectedRuntime = window.Options.Project.Runtime;
            _window = window;
        }

        private readonly DeploymentDialogWindow _window;

        private readonly IList<string> _loadingAccounts = new List<string> { "Loading..." };
        private readonly IList<GcpProject> _loadingProjects = new List<GcpProject> { new GcpProject { Name = "Loading..." } };

        public async void StartLoadingProjects()
        {
            Debug.WriteLine("Loading projects...");

            try
            {
                Loaded = false;
                Accounts = _loadingAccounts;
                CloudProjects = _loadingProjects;
                SelectedAccount = _loadingAccounts[0];
                SelectedCloudProject = _loadingProjects[0];

                var accounts = await GCloudWrapper.Instance.GetAccountListAsync();
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
                   accountAndProject: new AccountAndProjectId(account: this.SelectedAccount, projectId: this.SelectedCloudProject.Id));
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
                    new AccountAndProjectId(account: this.SelectedAccount));

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
