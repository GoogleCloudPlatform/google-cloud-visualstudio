// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Credentials;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class AppEngineRootViewModel : SourceRootViewModelBase
    {
        private const string AppEngineIconResourcePath = "CloudExplorerSources/AppEngine/Resources/app_engine.png";
        
        private const string BuiltInPrefix = "ah-";

        private static readonly Lazy<ImageSource> s_appEngineIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(AppEngineIconResourcePath));
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
                if (Owner.CurrentProject == null)
                {
                    throw new CloudExplorerSourceException("Must have a non-null current project.");
                }

                var oauthToken = await CredentialsManager.GetAccessTokenAsync();
                var services = await GaeDataSource.GetServicesAsync(Owner.CurrentProject.Id, oauthToken);
                var servicesVersions = new List<Tuple<GaeService, IList<GaeVersion>>>();
                foreach (var s in services)
                {
                    var versions = await GaeDataSource.GetServiceVersionsAsync(s.Name, oauthToken);
                    var serviceVersion = new Tuple<GaeService, IList<GaeVersion>>(s, versions.Where(x => !x.Id.StartsWith(BuiltInPrefix)).ToList());
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
            var versions = new List<GaeVersionViewModel>();
            foreach (var version in serviceVersions.Item2)
            {
                double allocation = 0.0;
                serviceVersions.Item1.Split.Allocations.TryGetValue(version.Id, out allocation);
                versions.Add(new GaeVersionViewModel(this, serviceVersions.Item1.Id, version, allocation));
            }
            return new GaeServiceViewModel(this, serviceVersions.Item1, versions);
        }
    }
}
