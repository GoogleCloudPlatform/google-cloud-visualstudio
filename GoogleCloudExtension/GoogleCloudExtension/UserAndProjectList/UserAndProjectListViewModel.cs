// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.UserAndProjectList
{
    /// <summary>
    /// This class is the view model for the user and project list window.
    /// </summary>
    public class UserAndProjectListViewModel : Model
    {
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
        private IList<CloudProject> _Projects;
        public IList<CloudProject> Projects
        {
            get { return _Projects; }
            private set { SetValueAndRaise(ref _Projects, value); }
        }

        /// <summary>
        /// The selected cloud project, setting this property changes the current project for the
        /// extension as a whole.
        /// </summary>
        private CloudProject _CurrentProject;
        public CloudProject CurrentProject
        {
            get { return _CurrentProject; }
            set
            {
                UpdateCurrentProject(value);
                SetValueAndRaise(ref _CurrentProject, value);
            }
        }

        /// <summary>
        /// The list of registered accounts with GCloud.
        /// </summary>
        private IEnumerable<string> _Accounts;
        public IEnumerable<string> Accounts
        {
            get { return _Accounts; }
            set { SetValueAndRaise(ref _Accounts, value); }
        }

        /// <summary>
        /// The selected account, setting this property changes the current account for the extension.
        /// </summary>
        private string _CurrentAccount;
        public string CurrentAccount
        {
            get { return _CurrentAccount; }
            set
            {
                SetValueAndRaise(ref _CurrentAccount, value);
                UpdateCurrentAccount(value);
            }
        }

        /// <summary>
        /// Whether the view model is loading projects.
        /// </summary>
        private bool _LoadingProjects;
        private bool LoadingProjects
        {
            get { return _LoadingProjects; }
            set
            {
                _LoadingProjects = value;
                RaisePropertyChanged(nameof(Loading));
            }
        }

        /// <summary>
        /// Whether the view model is loading accounts.
        /// </summary>
        private bool _LoadingAccounts;
        internal bool LoadingAccounts
        {
            get { return _LoadingAccounts; }
            set
            {
                _LoadingAccounts = value;
                RaisePropertyChanged(nameof(Loading));
            }
        }

        /// <summary>
        /// Combination of both properties, used in UI bindings.
        /// </summary>
        public bool Loading => LoadingProjects || LoadingAccounts; 

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
                var currentAccountAndProject = await GCloudWrapper.Instance.GetCurrentAccountAndProjectAsync();
                if (newProject.Id == currentAccountAndProject.ProjectId)
                {
                    return;
                }
                var newCurrentAccountAndProject = new Credentials(
                    account: currentAccountAndProject.Account,
                    projectId: newProject.Id);
                GCloudWrapper.Instance.UpdateUserAndProject(newCurrentAccountAndProject);
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
                var currentAccountAndProject = await GCloudWrapper.Instance.GetCurrentAccountAndProjectAsync();
                if (currentAccountAndProject.Account != value)
                {
                    var newAccountAndProject = new Credentials(
                        account: value,
                        projectId: null);
                    GCloudWrapper.Instance.UpdateUserAndProject(newAccountAndProject);
                }

                // Since the account might be different we need to load the projects.
                if (currentAccountAndProject.Account != value || this.Projects == null)
                {
                    try
                    {
                        this.LoadingProjects = true;
                        var projects = await GCloudWrapper.Instance.GetProjectsAsync();
                        this.Projects = projects;
                        var candidateProject = projects?.Where(x => x.Id == currentAccountAndProject.ProjectId).FirstOrDefault();
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
