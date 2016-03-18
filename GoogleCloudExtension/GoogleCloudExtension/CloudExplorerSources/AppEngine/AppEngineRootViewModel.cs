// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineRootViewModel : SourceRootViewModelBase
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
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Content = "No apps found."
        };

        public override ImageSource RootIcon => s_appEngineIcon.Value;

        public override string RootCaption => "AppEngine";

        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;

        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;

        /// <summary>
        /// Loads the list of app engine apps, changing the state of the properties
        /// as the process advances.
        /// </summary>
        protected override async Task LoadDataOverride()
        {
            try
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
            catch (DataSourceException ex)
            {
                GcpOutputWindow.OutputLine("Failed to load the list of AppEngine apps.");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                throw new CloudExplorerSourceException(ex.Message, ex);
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
    }
}
