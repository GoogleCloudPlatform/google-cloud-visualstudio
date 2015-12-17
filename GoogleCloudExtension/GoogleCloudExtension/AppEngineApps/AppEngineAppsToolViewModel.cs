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
    internal class VersionComparer : IComparer<ModuleAndVersionViewModel>
    {
        public int Compare(ModuleAndVersionViewModel x, ModuleAndVersionViewModel y)
        {
            // There's only one default version, so both having the default bit
            // set means is the same version.
            if (x.ModuleAndVersion.IsDefault && y.ModuleAndVersion.IsDefault)
            {
                return 0;
            }

            // Ensure the default version is first.
            if (x.ModuleAndVersion.IsDefault)
            {
                return -1;
            }
            else if (y.ModuleAndVersion.IsDefault)
            {
                return 1;
            }

            // No default version, compare by name.
            return x.ModuleAndVersion.Version.CompareTo(y.ModuleAndVersion.Version);
        }
    }

    /// <summary>
    /// This clas represents the group of versions for each module in the project, allows
    /// the UI to show a hierarchical view of modules and the versions that belong to the
    /// module.
    /// </summary>
    internal class Module
    {
        public string Name { get; }

        public IEnumerable<ModuleAndVersionViewModel> Versions { get; }

        public Module(string name, IEnumerable<ModuleAndVersionViewModel> versions)
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
            }
        }

        /// <summary>
        /// The command to invoke to refresh the list of modules and versions.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Helper property to determine if there are apps in the list.
        /// </summary>
        public bool HaveApps => (Apps?.Count ?? 0) != 0;

        public AppEngineAppsToolViewModel()
        {
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
                    .Select(x => new Module(x.Key, x.Select(y => new ModuleAndVersionViewModel(this, y))))
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
