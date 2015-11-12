// Copyright 2015 Google Inc. All Rights Reserved.
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
        private IList<AppEngineApplication> _Apps;
        public IList<AppEngineApplication> Apps
        {
            get { return _Apps; }
            set
            {
                SetValueAndRaise(ref _Apps, value);
                RaisePropertyChanged(nameof(HaveApps));
                this.CurrentApp = value?.FirstOrDefault();
            }
        }

        private AppEngineApplication _CurrentApp;
        public AppEngineApplication CurrentApp
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

        private ICommand _OpenAppCommand;
        public ICommand OpenAppCommand
        {
            get { return _OpenAppCommand; }
            set { SetValueAndRaise(ref _OpenAppCommand, value); }
        }

        private ICommand _DeleteVersionCommand;
        public ICommand DeleteVersionCommand
        {
            get { return _DeleteVersionCommand; }
            set { SetValueAndRaise(ref _DeleteVersionCommand, value); }
        }

        private ICommand _SetDefaultVersionCommand;
        public ICommand SetDefaultVersionCommand
        {
            get { return _SetDefaultVersionCommand; }
            set { SetValueAndRaise(ref _SetDefaultVersionCommand, value); }
        }

        private ICommand _RefreshCommand;
        public ICommand RefreshCommand
        {
            get { return _RefreshCommand; }
            set { SetValueAndRaise(ref _RefreshCommand, value); }
        }

        private bool _OpenAppEnabled = true;
        public bool OpenAppEnabled
        {
            get { return _OpenAppEnabled; }
            set { SetValueAndRaise(ref _OpenAppEnabled, value); }
        }

        private bool _SetDefaultVersionEnabled = true;
        public bool SetDefaultVersionEnabled
        {
            get { return _SetDefaultVersionEnabled; }
            set { SetValueAndRaise(ref _SetDefaultVersionEnabled, value); }
        }

        private string _OpenAppButtonTitle = "Open App";
        public string OpenAppButtonTitle
        {
            get { return _OpenAppButtonTitle; }
            set { SetValueAndRaise(ref _OpenAppButtonTitle, value); }
        }

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

        public bool HaveApps => Apps != null && Apps.Count != 0;

        public async void LoadAppEngineAppList()
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
                this.Apps = new List<AppEngineApplication>();
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
            var app = (AppEngineApplication)parameter;
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
            var app = (AppEngineApplication)parameter;
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
            LoadAppEngineAppList();
        }

        private async void OnSetDefaultVersion(object parameter)
        {
            var app = (AppEngineApplication)parameter;
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
            LoadAppEngineAppList();
        }

        private void OnRefresh(object param)
        {
            LoadAppEngineAppList();
        }

        private void InvalidateAppEngineAppList(object src, EventArgs args)
        {
            Debug.WriteLine("AppEngine app list invalidated.");
            LoadAppEngineAppList();
        }
    }
}
