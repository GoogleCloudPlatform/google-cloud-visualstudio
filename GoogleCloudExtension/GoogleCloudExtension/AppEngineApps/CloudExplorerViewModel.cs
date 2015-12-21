// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.CloudExplorer
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
    /// This class contains the view specific logic for the AppEngineAppsToolWindow view.
    /// </summary>
    internal class CloudExplorerViewModel : ViewModelBase
    {
        private IList<TreeHierarchy> _roots;

        /// <summary>
        /// The list of module and version combinations for the current project.
        /// </summary>
        public IList<TreeHierarchy> Roots
        {
            get { return _roots; }
            private set { SetValueAndRaise(ref _roots, value); }
        }

        /// <summary>
        /// The command to invoke to refresh the list of modules and versions.
        /// </summary>
        public ICommand RefreshCommand { get; }

        public CloudExplorerViewModel()
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
                this.Roots = null;
                var apps = await AppEngineClient.GetAppEngineAppListAsync();
                Roots = apps
                    .GroupBy(x => x.Module)
                    .OrderBy(x => x.Key)
                    .Select(x => MakeModuleHierarchy(x))
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

        private TreeHierarchy MakeModuleHierarchy(IGrouping<string, ModuleAndVersion> src)
        {
            var versions = src
                .OrderBy(x => x, new VersionComparer())
                .Select(x => new ModuleAndVersionViewModel(this, x));
            return new TreeHierarchy(versions) { Content = src.Key };
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
