// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.UserAndProjectList
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("170d091f-5a05-46e9-9d7b-3fdab8b413d3")]
    public class UserAndProjectListWindow : ToolWindowPane
    {
        private readonly UserAndProjectListViewModel _model;
        private readonly UserAndProjectListWindowControl _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAndProjectListWindow"/> class.
        /// </summary>
        public UserAndProjectListWindow() : base(null)
        {
            this.Caption = "Projects";

            _model = new UserAndProjectListViewModel();
            _content = new UserAndProjectListWindowControl();
            _content.DataContext = _model;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = _content;

            LoadAccountsAsync();
        }

        private async void LoadAccountsAsync()
        {
            if (!GCloudWrapper.Instance.ValidateGCloudInstallation())
            {
                Debug.WriteLine("GCloud is not installed, disabling the User and Project tool window.");
                return;
            }

            try
            {
                _model.LoadingAccounts = true;
                var accounts = await GCloudWrapper.Instance.GetAccountListAsync();
                var currentAccountAndProject = await GCloudWrapper.Instance.GetCurrentAccountAndProjectAsync();
                _model.Accounts = accounts;
                _model.CurrentAccount = currentAccountAndProject.Account;
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine($"Failed to load the current account and project.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            finally
            {
                _model.LoadingAccounts = false;
            }
        }
    }
}
