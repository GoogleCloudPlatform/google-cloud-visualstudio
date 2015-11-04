// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.UserAndProjectList
{
    public class UserAndProjectListViewModel : Model
    {
        public bool IsGCloudInstalled
        {
            get { return GCloudWrapper.DefaultInstance.ValidateGCloudInstallation(); }
        }

        public bool IsGCloudNotInstalled
        {
            get { return !this.IsGCloudInstalled; }
        }

        private IList<GcpProject> _Projects;
        public IList<GcpProject> Projects
        {
            get { return _Projects; }
            private set { SetValueAndRaise(ref _Projects, value); }
        }

        private GcpProject _CurrentProject;
        public GcpProject CurrentProject
        {
            get { return _CurrentProject; }
            set
            {
                UpdateCurrentProject(value);
                SetValueAndRaise(ref _CurrentProject, value);
            }
        }

        private async void UpdateCurrentProject(GcpProject newProject)
        {
            try
            {
                if (newProject == null)
                {
                    return;
                }
                var currentAccountAndProject = await GCloudWrapper.DefaultInstance.GetCurrentAccountAndProjectAsync();
                if (newProject.Id == currentAccountAndProject.ProjectId)
                {
                    return;
                }
                var newCurrentAccountAndProject = new AccountAndProjectId(
                    account: currentAccountAndProject.Account,
                    projectId: newProject.Id);
                GCloudWrapper.DefaultInstance.UpdateUserAndProject(newCurrentAccountAndProject);
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine($"Failed to update project to {newProject.Name}");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
        }

        private IList<string> _Accounts;
        public IList<string> Accounts
        {
            get { return _Accounts; }
            set { SetValueAndRaise(ref _Accounts, value); }
        }

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
                var currentAccountAndProject = await GCloudWrapper.DefaultInstance.GetCurrentAccountAndProjectAsync();
                if (currentAccountAndProject.Account != value)
                {
                    var newAccountAndProject = new AccountAndProjectId(
                        account: value,
                        projectId: null);
                    GCloudWrapper.DefaultInstance.UpdateUserAndProject(newAccountAndProject);
                }

                // Since the account might be different we need to load the projects.
                if (currentAccountAndProject.Account != value || this.Projects == null)
                {
                    try
                    {
                        this.LoadingProjects = true;
                        var projects = await GCloudWrapper.DefaultInstance.GetProjectsAsync();
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

        public bool Loading
        {
            get { return LoadingProjects || LoadingAccounts; }
        }
    }
}
