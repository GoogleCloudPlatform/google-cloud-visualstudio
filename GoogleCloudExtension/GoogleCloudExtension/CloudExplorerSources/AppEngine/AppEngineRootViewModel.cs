// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.CloudExplorerSources.Utils;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineRootViewModel : TreeHierarchy
    {
        private const string AppEngineIconResourcePath = "CloudExplorerSources/AppEngine/Resources/app_engine.png";
        private const string ModuleIconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_view_module.png";

        private static readonly Lazy<ImageSource> s_appEngineIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(AppEngineIconResourcePath));
        private static readonly Lazy<ImageSource> s_moduleIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(ModuleIconResourcePath));
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Content = "Loading modules...",
            IsLoading = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Content = "Failed loading AppEngine modules.",
            IsError = true
        };

        private bool _loading;
        private bool _loaded;

        public AppEngineRootViewModel()
        {
            Content = "AppEngine";
            Icon = s_appEngineIcon.Value;
            Children.Add(s_loadingPlaceholder);

            // Add a weak event handler to receive notifications of the deployment of app engine instances.
            // We also need to invalidate the list if the account or project changed.
            var handler = new WeakAction<object, EventArgs>(this.InvalidateAppEngineAppList);
            ExtensionEvents.AppEngineDeployed += handler.Invoke;
            GCloudWrapper.Instance.AccountOrProjectChanged += handler.Invoke;
        }

        protected override void OnIsExpandedChanged(bool newValue)
        {
            if (_loading)
            {
                return;
            }

            if (!_loaded && newValue)
            {
                LoadAppEngineAppListAsync();
            }
        }

        /// <summary>
        /// Loads the list of app engine apps, changing the state of the properties
        /// as the process advances.
        /// </summary>
        internal async void LoadAppEngineAppListAsync()
        {
            try
            {
                _loading = true;

                var gcloudValidationResult = await EnvironmentUtils.ValidateGCloudInstallation();
                if (!gcloudValidationResult.IsValidGCloudInstallation())
                {
                    Children.Clear();
                    Children.Add(CommonUtils.GetErrorItem(gcloudValidationResult));
                }
                else
                {
                    var credentials = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
                    var oauthToken = await GCloudWrapper.Instance.GetAccessTokenAsync();

                    var services = await GaeDataSource.GetServicesAsync(credentials.ProjectId, oauthToken);
                    var servicesVersions = new List<Tuple<GaeService, IList<GaeVersion>>>();
                    foreach (var s in services)
                    {
                        var versions = await GaeDataSource.GetServiceVersionsAsync(s.Name, oauthToken);
                        var serviceVersion = new Tuple<GaeService, IList<GaeVersion>>(s, versions);
                        servicesVersions.Add(serviceVersion);
                    }

                    var nodes = servicesVersions.OrderBy(x => x.Item1.Id).Select(MakeModuleHierarchy);

                    Children.Clear();
                    foreach (var node in nodes)
                    {
                        Children.Add(node);
                    }
                }

                _loaded = true;
            }
            catch (GCloudException ex)
            {
                GcpOutputWindow.OutputLine("Failed to load the list of AppEngine apps.");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                Children.Clear();
                Children.Add(s_errorPlaceholder);
            }
            finally
            {
                _loading = false;
            }
        }

        private TreeHierarchy MakeModuleHierarchy(Tuple<GaeService, IList<GaeVersion>> serviceVersions)
        {
            var versionsById = serviceVersions.Item2.ToDictionary(x => x.Id, x => x);
            var versions = from v in serviceVersions.Item1.Split.Allocations
                           orderby v.Value descending
                           select new VersionViewModel(serviceVersions.Item1.Id, versionsById[v.Key], v.Value);

            return new TreeHierarchy(versions) { Content = serviceVersions.Item1.Id, Icon = s_moduleIcon.Value };
        }

        private void InvalidateAppEngineAppList(object src, EventArgs args)
        {
            Refresh();
        }

        internal void Refresh()
        {
            if (!_loaded)
            {
                return;
            }

            ResetChildren();
            LoadAppEngineAppListAsync();
        }

        private void ResetChildren()
        {
            _loaded = false;
            Children.Clear();
            Children.Add(s_loadingPlaceholder);
        }
    }
}
