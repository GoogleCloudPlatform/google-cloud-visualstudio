﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.AppEngineApps
{
    /// <summary>
    /// This class contains the view specific logic for the AppEngineAppsToolWindow view.
    /// </summary>
    internal class AppEngineAppsToolViewModel : ViewModelBase
    {
        /// <summary>
        /// The list of module and version combinations for the current project.
        /// </summary>
        private IList<ModuleAndVersion> _Apps;
        public IList<ModuleAndVersion> Apps
        {
            get { return _Apps; }
            private set
            {
                SetValueAndRaise(ref _Apps, value);
                RaisePropertyChanged(nameof(HaveApps));
                this.CurrentApp = value?.FirstOrDefault();
            }
        }

        /// <summary>
        /// The selected version.
        /// </summary>
        private ModuleAndVersion _CurrentApp;
        public ModuleAndVersion CurrentApp
        {
            get { return _CurrentApp; }
            set
            {
                if (value != null)
                {
                    this.SetDefaultVersionEnabled = !value.IsDefault;
                }
                this.OpenAppEnabled = (value != null);
                SetValueAndRaise(ref _CurrentApp, value);
            }
        }

        /// <summary>
        /// The command to invoke to open a browser on the selected app.
        /// </summary>
        public ICommand OpenAppCommand { get; private set; }

        /// <summary>
        /// The command to invoke to delete the selected version.
        /// </summary>
        public ICommand DeleteVersionCommand { get; private set; }

        /// <summary>
        /// The command to invoke to set the selected version as the default version.
        /// </summary>
        public ICommand SetDefaultVersionCommand { get; private set; }
        
        /// <summary>
        /// The command to invoke to refresh the list of modules and versions.
        /// </summary>
        public ICommand RefreshCommand { get; private set; }

        /// <summary>
        ///  Whether the open app command is enabled.
        /// </summary>
        private bool _OpenAppEnabled;
        public bool OpenAppEnabled
        {
            get { return _OpenAppEnabled; }
            set { SetValueAndRaise(ref _OpenAppEnabled, value); }
        }

        /// <summary>
        /// Wether the set default version command is enabled.
        /// </summary>
        private bool _SetDefaultVersionEnabled;
        public bool SetDefaultVersionEnabled
        {
            get { return _SetDefaultVersionEnabled; }
            set { SetValueAndRaise(ref _SetDefaultVersionEnabled, value); }
        }

        /// <summary>
        /// The title to use for the open app button.
        /// </summary>
        private string _OpenAppButtonTitle = "Open App";
        public string OpenAppButtonTitle
        {
            get { return _OpenAppButtonTitle; }
            private set { SetValueAndRaise(ref _OpenAppButtonTitle, value); }
        }

        public bool HaveApps => Apps?.Count != 0;

        public AppEngineAppsToolViewModel()
        {
            OpenAppCommand = new WeakCommand(this.OnOpenApp);
            DeleteVersionCommand = new WeakCommand(this.OnDeleteVersion);
            SetDefaultVersionCommand = new WeakCommand(this.OnSetDefaultVersion);
            RefreshCommand = new WeakCommand(this.OnRefresh);

            // Add a weak event handler to receive notifications of the deployment of app engine instances.
            // We also need to invalidate the list if the account or project changed.
            var handler = new WeakHandler(this.InvalidateAppEngineAppList);
            ExtensionEvents.AppEngineDeployed += handler.OnEvent;
            GCloudWrapper.Instance.AccountOrProjectChanged += handler.OnEvent;
        }

        public async void LoadAppEngineAppListAsync()
        {
            if (!GCloudWrapper.Instance.ValidateGCloudInstallation())
            {
                Debug.WriteLine("Cannot find GCloud, disabling the AppEngine tool window.");
                return;
            }

            try
            {
                this.LoadingMessage = "Loading AppEngine app list...";
                this.Loading = true;
                this.Apps = new List<ModuleAndVersion>();
                this.Apps = await AppEngineClient.GetAppEngineAppListAsync();
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine("Failed to load the list of AppEngine apps.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            finally
            {
                this.Loading = false;
            }
        }

        private async void OnOpenApp(object parameter)
        {
            var app = (ModuleAndVersion)parameter;
            if (app == null)
            {
                return;
            }

            try
            {
                this.OpenAppEnabled = false;
                this.OpenAppButtonTitle = "Opening app...";

                var accountAndProject = await GCloudWrapper.Instance.GetCurrentAccountAndProjectAsync();
                var url = $"https://{app.Version}-dot-{app.Module}-dot-{accountAndProject.ProjectId}.appspot.com/";
                Debug.WriteLine($"Opening URL: {url}");
                Process.Start(url);
            }
            finally
            {
                this.OpenAppEnabled = true;
                this.OpenAppButtonTitle = "Open App";
            }
        }

        private async void OnDeleteVersion(object parameter)
        {
            var app = (ModuleAndVersion)parameter;
            if (app == null)
            {
                return;
            }

            try
            {
                this.LoadingMessage = "Deleting version...";
                this.Loading = true;
                await AppEngineClient.DeleteAppVersion(app.Module, app.Version);
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine($"Failed to delete version {app.Version} in module {app.Module}");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            finally
            {
                this.Loading = false;
            }
            LoadAppEngineAppListAsync();
        }

        private async void OnSetDefaultVersion(object parameter)
        {
            var app = (ModuleAndVersion)parameter;
            if (app == null)
            {
                return;
            }

            try
            {
                this.Loading = true;
                this.LoadingMessage = "Setting default version...";
                await AppEngineClient.SetDefaultAppVersionAsync(app.Module, app.Version);
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine("Failed to set default version.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            finally
            {
                this.Loading = false;
            }
            LoadAppEngineAppListAsync();
        }

        private void OnRefresh(object param)
        {
            LoadAppEngineAppListAsync();
        }

        private void InvalidateAppEngineAppList(object src, EventArgs args)
        {
            Debug.WriteLine("AppEngine app list invalidated.");
            LoadAppEngineAppListAsync();
        }
    }
}
