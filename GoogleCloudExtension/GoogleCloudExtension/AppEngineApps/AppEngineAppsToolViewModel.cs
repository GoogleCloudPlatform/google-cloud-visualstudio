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
    /// This comparer will ensure that the resulting order has the default version as the
    /// top of the list, followed by the rest of the versions sorted alphabetically by their
    /// names.
    /// </summary>
    internal class VersionComparer : IComparer<ModuleAndVersion>
    {
        public int Compare(ModuleAndVersion x, ModuleAndVersion y)
        {
            // There's only one default version, so both having the default bit
            // set means is the same version.
            if (x.IsDefault && y.IsDefault)
            {
                return 0;
            }

            // Ensure the default version is first.
            if (x.IsDefault)
            {
                return -1;
            }
            else if (y.IsDefault)
            {
                return 1;
            }

            // No default version, compare by name.
            return x.Version.CompareTo(y.Version);
        }
    }

    /// <summary>
    /// This clas represents the group of versions for each module in the project, allows
    /// the UI to show a hierarchical view of modules and the versions that belong to the
    /// module.
    /// </summary>
    public class Module
    {
        public string Name { get; }

        public IEnumerable<ModuleAndVersion> Versions { get; }

        public Module(string name, IEnumerable<ModuleAndVersion> versions)
        {
            Name = name;
            Versions = versions.OrderBy(x => x, new VersionComparer());
        }
    }

    /// <summary>
    /// This class contains the view specific logic for the AppEngineAppsToolWindow view.
    /// </summary>
    internal class AppEngineAppsToolViewModel : ViewModelBase
    {
        private IList<Module> _apps;
        private ModuleAndVersion _currentApp;
        private bool _openAppEnabled;
        private bool _setDefaultVersionEnabled;
        private string _openAppButtonTitle = "Open App";

        /// <summary>
        /// The list of module and version combinations for the current project.
        /// </summary>
        public IList<Module> Apps
        {
            get { return _apps; }
            private set
            {
                SetValueAndRaise(ref _apps, value);
                RaisePropertyChanged(nameof(HaveApps));
                this.CurrentApp = value?.FirstOrDefault()?.Versions?.FirstOrDefault();
            }
        }

        /// <summary>
        /// The selected version.
        /// </summary>
        public ModuleAndVersion CurrentApp
        {
            get { return _currentApp; }
            set
            {
                if (value != null)
                {
                    this.SetDefaultVersionEnabled = !value.IsDefault;
                }
                this.OpenAppEnabled = (value != null);
                SetValueAndRaise(ref _currentApp, value);
            }
        }

        /// <summary>
        /// The command to invoke to open a browser on the selected app.
        /// </summary>
        public ICommand OpenAppCommand { get; }

        /// <summary>
        /// The command to invoke to delete the selected version.
        /// </summary>
        public ICommand DeleteVersionCommand { get; }

        /// <summary>
        /// The command to invoke to set the selected version as the default version.
        /// </summary>
        public ICommand SetDefaultVersionCommand { get; }

        /// <summary>
        /// The command to invoke to refresh the list of modules and versions.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        ///  Whether the open app command is enabled.
        /// </summary>
        public bool OpenAppEnabled
        {
            get { return _openAppEnabled; }
            set { SetValueAndRaise(ref _openAppEnabled, value); }
        }

        /// <summary>
        /// Wether the set default version command is enabled.
        /// </summary>
        public bool SetDefaultVersionEnabled
        {
            get { return _setDefaultVersionEnabled; }
            set { SetValueAndRaise(ref _setDefaultVersionEnabled, value); }
        }

        /// <summary>
        /// The title to use for the open app button.
        /// </summary>
        public string OpenAppButtonTitle
        {
            get { return _openAppButtonTitle; }
            private set { SetValueAndRaise(ref _openAppButtonTitle, value); }
        }

        /// <summary>
        /// Helper property to determine if there are apps in the list.
        /// </summary>
        public bool HaveApps => (Apps?.Count ?? 0) != 0;

        public AppEngineAppsToolViewModel()
        {
            OpenAppCommand = new WeakCommand<ModuleAndVersion>(this.OnOpenApp);
            DeleteVersionCommand = new WeakCommand<ModuleAndVersion>(this.OnDeleteVersion);
            SetDefaultVersionCommand = new WeakCommand<ModuleAndVersion>(this.OnSetDefaultVersion);
            RefreshCommand = new WeakCommand(this.OnRefresh);

            // Add a weak event handler to receive notifications of the deployment of app engine instances.
            // We also need to invalidate the list if the account or project changed.
            var handler = new WeakAction<object, EventArgs>(this.InvalidateAppEngineAppList);
            ExtensionEvents.AppEngineDeployed += handler.Invoke;
            GCloudWrapper.Instance.AccountOrProjectChanged += handler.Invoke;
        }

        /// <summary>
        /// Loads the list of app engine apps, changing the state of the properties
        /// as the process advances.
        /// </summary>
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
                this.Apps = null;
                var apps = await AppEngineClient.GetAppEngineAppListAsync();
                this.Apps = apps
                    .GroupBy(x => x.Module)
                    .OrderBy(x => x.Key)
                    .Select(x => new Module(x.Key, x))
                    .ToList();
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

        #region Command handlers

        private async void OnOpenApp(ModuleAndVersion app)
        {
            try
            {
                this.OpenAppEnabled = false;
                this.OpenAppButtonTitle = "Opening app...";

                var accountAndProject = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
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

        private async void OnDeleteVersion(ModuleAndVersion app)
        {
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

        private async void OnSetDefaultVersion(ModuleAndVersion app)
        {
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

        private void OnRefresh()
        {
            LoadAppEngineAppListAsync();
        }

        #endregion

        private void InvalidateAppEngineAppList(object src, EventArgs args)
        {
            Debug.WriteLine("AppEngine app list invalidated.");
            LoadAppEngineAppListAsync();
        }
    }
}
