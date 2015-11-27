// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GoogleCloudExtension.UserAndProjectList
{
    /// <summary>
    /// This class is the view model for the user and project list window.
    /// </summary>
    public class UserAndProjectListViewModel : ViewModelBase
    {
        private IList<CloudProject> _projects;
        private CloudProject _currentProject;
        private IEnumerable<string> _accounts;
        private string _currentAccount;
        private bool _loadingProjects;
        private bool _loadingAccounts;

        /// <summary>
        /// Helper property that determines if GCloud is installed.
        /// </summary>
        public bool IsGCloudInstalled => GCloudWrapper.Instance.ValidateGCloudInstallation();

        /// <summary>
        /// Helper property that negates the previous one, needed to simplify UI bindings.
        /// </summary>
        public bool IsGCloudNotInstalled => !IsGCloudInstalled;

        /// <summary>
        /// The list of cloud projects available to the selected user.
        /// </summary>
        public IList<CloudProject> Projects
        {
            get { return _projects; }
            private set { SetValueAndRaise(ref _projects, value); }
        }

        /// <summary>
        /// The selected cloud project, setting this property changes the current project for the
        /// extension as a whole.
        /// </summary>
        public CloudProject CurrentProject
        {
            get { return _currentProject; }
            set
            {
                UpdateCurrentProject(value);
                SetValueAndRaise(ref _currentProject, value);
            }
        }

        /// <summary>
        /// The list of registered accounts with GCloud.
        /// </summary>
        public IEnumerable<string> Accounts
        {
            get { return _accounts; }
            set { SetValueAndRaise(ref _accounts, value); }
        }

        /// <summary>
        /// The selected account, setting this property changes the current account for the extension.
        /// </summary>
        public string CurrentAccount
        {
            get { return _currentAccount; }
            set
            {
                SetValueAndRaise(ref _currentAccount, value);
                UpdateCurrentAccount(value);
            }
        }

        /// <summary>
        /// Whether the view model is loading projects.
        /// </summary>
        public bool LoadingProjects
        {
            get { return _loadingProjects; }
            set
            {
                _loadingProjects = value;
                RaisePropertyChanged(nameof(Loading));
            }
        }

        /// <summary>
        /// Whether the view model is loading accounts.
        /// </summary>
        public bool LoadingAccounts
        {
            get { return _loadingAccounts; }
            set
            {
                _loadingAccounts = value;
                RaisePropertyChanged(nameof(Loading));
            }
        }

        /// <summary>
        /// Combination of both properties, used in UI bindings.
        /// </summary>
        new public bool Loading => LoadingProjects || LoadingAccounts;

        public UserAndProjectListViewModel()
        {
            var handler = new WeakDelegate<object, EventArgs>(this.OnCredentialsChanged);
            GCloudWrapper.Instance.AccountOrProjectChanged += handler.Invoke;
        }

        private void OnCredentialsChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("Invalidating the credentials.");
            LoadAccountsAsync();
        }

        /// <summary>
        /// Loads the list of accounts for the dialog.
        /// </summary>
        public async void LoadAccountsAsync()
        {
            if (!GCloudWrapper.Instance.ValidateGCloudInstallation())
            {
                Debug.WriteLine("GCloud is not installed, disabling the User and Project tool window.");
                return;
            }

            try
            {
                this.LoadingAccounts = true;
                var accounts = await GCloudWrapper.Instance.GetAccountsAsync();
                var currentAccountAndProject = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
                this.Accounts = accounts;
                this.CurrentAccount = currentAccountAndProject.Account;
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.Activate();
                AppEngineOutputWindow.Clear();
                AppEngineOutputWindow.OutputLine($"Failed to load the current account and project.");
                AppEngineOutputWindow.OutputLine(ex.Message);
            }
            finally
            {
                this.LoadingAccounts = false;
            }
        }

        /// <summary>
        /// Updates the current project if it has changed.
        /// </summary>
        /// <param name="newProject"></param>
        private async void UpdateCurrentProject(CloudProject newProject)
        {
            try
            {
                if (newProject == null)
                {
                    return;
                }
                var currentAccountAndProject = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
                if (newProject.Id == currentAccountAndProject.ProjectId)
                {
                    return;
                }
                var newCurrentAccountAndProject = new Credentials(
                    account: currentAccountAndProject.Account,
                    projectId: newProject.Id);
                GCloudWrapper.Instance.UpdateCredentials(newCurrentAccountAndProject);
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine($"Failed to update project to {newProject.Name}");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
        }

        /// <summary>
        /// Updates the current account if it has changed.
        /// </summary>
        /// <param name="value"></param>
        private async void UpdateCurrentAccount(string value)
        {
            if (value == null)
            {
                return;
            }

            try
            {
                // Only need to update the GCloudWrapper current account if the account
                // is different than the current one.
                var currentAccountAndProject = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
                if (currentAccountAndProject.Account != value)
                {
                    var newAccountAndProject = new Credentials(
                        account: value,
                        projectId: null);
                    GCloudWrapper.Instance.UpdateCredentials(newAccountAndProject);
                }

                // Since the account might be different we need to load the projects.
                if (currentAccountAndProject.Account != value || this.Projects == null)
                {
                    try
                    {
                        this.LoadingProjects = true;
                        var projects = await GCloudWrapper.Instance.GetProjectsAsync();
                        this.Projects = projects;
                        var candidateProject = projects?.FirstOrDefault(x => x.Id == currentAccountAndProject.ProjectId);
                        if (candidateProject == null)
                        {
                            candidateProject = projects?.FirstOrDefault();
                        }
                        this.CurrentProject = candidateProject;
                    }
                    finally
                    {
                        this.LoadingProjects = false;
                    }
                }
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine($"Failed to update current account to {value}");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
        }
    }
}
