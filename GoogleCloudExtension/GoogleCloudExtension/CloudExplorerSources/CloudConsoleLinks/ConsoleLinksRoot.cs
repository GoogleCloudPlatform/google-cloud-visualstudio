// Copyright 2017 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GoogleCloudExtension.CloudExplorerSources.CloudConsoleLinks
{
    public class ConsoleLinksRoot : TreeHierarchy, ISourceRootViewModelBase
    {
        private const string CloudConsoleRootUrl = "https://console.cloud.google.com/";
        private const string ProjectUrlArgumentFormat = "?project={0}";

        private const string HomeUrl = CloudConsoleRootUrl + ProjectUrlArgumentFormat;
        private const string AppEnginePath = "appengine";
        private const string ComputeEnginePath = "compute";
        private const string KubernetesEnginePath = "kubernetes";
        private const string ContainerRegistryPath = "gcr";
        private const string DeploymentManagerPath = "dm";
        private const string CloudLauncherPath = "launcher";
        // ReSharper disable once InconsistentNaming
        private const string IAmPath = "iam-admin";
        private const string BillingPath = "billing";
        private const string ApisAndServicesPath = "apis";
        private const string SupportPath = "support";
        private const string SecurityCenterPath = "security";
        private const string BigtablePath = "bigtable";
        private const string DatastorePath = "datastore";
        private const string CloudStoragePath = "storage";
        private const string CloudSqlPath = "sql";
        private const string SpannerPath = "spanner";
        private const string VpcNetworkPath = "networking";
        private const string NetworkServicesPath = "net-services";
        private const string HybridConnectivityPath = "hybrid";
        private const string NetworkServiceTierPath = "net-tier";
        private const string NetworkSecurityPath = "net-security";
        private const string StackdriverMonitoringPath = "monitoring";
        private const string StackdriverDebugPath = "debug";
        private const string StackdriverTracePath = "traces";
        private const string StackdriverLoggingPath = "logs";
        private const string StackdriverErrorReportingPath = "errors";
        private const string StackdriverProfilerPath = "profiler";
        private const string SourceRepositoriesPath = "code";
        private const string EndpointsPath = "endpoints";
        private const string BigQueryPath = "bigquery";
        private const string PubSubPath = "cloudpubsub";
        private const string MachineLearningEnginePath = "mlengine";

        private const string KubernetesCodelabLink =
            "https://codelabs.developers.google.com/codelabs/cloud-kubernetes-aspnetcore/";

        internal static readonly LinkInfo s_consoleHomeFormatInfo = new LinkInfo(
            HomeUrl, Resources.CloudExplorerConsoleLinkCaption);

        private static readonly IReadOnlyList<(string path, string caption)> s_primaryConsoleLinkPaths = new[]
        {
            (AppEnginePath, Resources.CloudLinkAppEngineCaption),
            (ComputeEnginePath, Resources.CloudLinkComputeEngineCaption),
            (KubernetesEnginePath, Resources.CloudLinkKubernetesEngineCaption)
        };

        private static readonly IReadOnlyList<(string groupCaption, IReadOnlyList<(string path, string caption)> paths)>
            s_groupedConsoleLinkPaths = new(string groupCaption, IReadOnlyList<(string path, string caption)> paths)[]
            {
                (Resources.ConsoleLinkProjectGroupCaption, new[]
                {
                    (CloudLauncherPath, Resources.CloudLinkCloudLauncherCaption),
                    (IAmPath, Resources.CloudLinkIAmCaption),
                    (BillingPath, Resources.CloudLinkBillingCaption),
                    (ApisAndServicesPath, Resources.CloudLinkApisAndServicesCaption),
                    (SupportPath, Resources.CloudLinkSupportCaption),
                    (SecurityCenterPath, Resources.CloudLinkSecurityCenterCaption)
                }),
                (Resources.ConsoleLinkComputeGroupCaption, new[]
                {
                    (AppEnginePath, Resources.CloudLinkAppEngineCaption),
                    (ComputeEnginePath, Resources.CloudLinkComputeEngineCaption),
                    (KubernetesEnginePath, Resources.CloudLinkKubernetesEngineCaption)
                }),
                (Resources.ConsoleLinkStorageGroupCaption, new[]
                {
                    (BigtablePath, Resources.CloudLinkBigtableCaption),
                    (DatastorePath, Resources.CloudLinkDatastoreCaption),
                    (CloudStoragePath, Resources.CloudLinkCloudStorageCaption),
                    (CloudSqlPath, Resources.CloudLinkCloudSqlCaption),
                    (SpannerPath, Resources.CloudLinkSpannerCaption)
                }),
                (Resources.ConsoleLinkNetworkingGroupCaption, new[]
                {
                    (VpcNetworkPath, Resources.CloudLinkVpcNetworkCaption),
                    (NetworkServicesPath, Resources.CloudLinkNetworkServicesCaption),
                    (HybridConnectivityPath, Resources.CloudLinkHybridConnectivityCaption),
                    (NetworkServiceTierPath, Resources.CloudLinkNetworkServiceTierCaption),
                    (NetworkSecurityPath, Resources.CloudLinkNetworkSecurityCaption)
                }),
                (Resources.ConsoleLinkStackdriverGroupCaption, new[]
                {
                    (StackdriverMonitoringPath, Resources.CloudLinkStackdriverMonitoringCaption),
                    (StackdriverDebugPath, Resources.CloudLinkStackdriverDebugCaption),
                    (StackdriverTracePath, Resources.CloudLinkStackdriverTraceCaption),
                    (StackdriverLoggingPath, Resources.CloudLinkStackdriverLoggingCaption),
                    (StackdriverErrorReportingPath, Resources.CloudLinkStackdriverErrorReportingCaption),
                    (StackdriverProfilerPath, Resources.CloudLinkStackdriverProfilerCaption)
                }),
                (Resources.ConsoleLinkToolsGroupCaption, new[]
                {
                    (ContainerRegistryPath, Resources.CloudLinkContainerRegistryCaption),
                    (SourceRepositoriesPath, Resources.CloudLinkSourceRepositoriesCaption),
                    (DeploymentManagerPath, Resources.CloudLinkDeploymentManagerCaption),
                    (EndpointsPath, Resources.CloudLinkEndpointsCaption)
                }),
                (Resources.ConsoleLinkBigDataGroupCaption, new[]
                {
                    (BigQueryPath, Resources.CloudLinkBigQueryCaption),
                    (PubSubPath, Resources.CloudLinkPubSubCaption),
                    (MachineLearningEnginePath, Resources.CloudLinkMachineLearningEngineCaption)
                })
            };

        private static readonly IReadOnlyDictionary<string, LinkInfo> s_helpLinks = new Dictionary<string, LinkInfo>
        {
            [KubernetesEnginePath] = new LinkInfo(KubernetesCodelabLink, Resources.ConsoleLinksKubernetesInfoTooltip)
        };

        private readonly Func<string, Process> _startProcess;

        private readonly ICloudSourceContext _context;

        internal static IEnumerable<LinkInfo> PrimaryConsoleLinkFormats =>
            s_primaryConsoleLinkPaths.Select(PathTupleToLinkInfo);

        internal static IEnumerable<(string groupCaption, IEnumerable<LinkInfo> links)> GroupedConsoleLinkFormats =>
            s_groupedConsoleLinkPaths.Select(
                tuple => (tuple.groupCaption, tuple.paths.Select(PathTupleToLinkInfo)));

        /// <summary>
        /// The command to execute when the link is pressed.
        /// </summary>
        public ProtectedCommand NavigateCommand { get; }

        /// <summary>
        /// Creates a new <see cref="ConsoleLinksRoot"/>. Initalizes the root with all of the links.
        /// </summary>
        /// <param name="context">The <see cref="ICloudSourceContext"/> to get the current project id from.</param>
        public ConsoleLinksRoot(ICloudSourceContext context) : this(context, Process.Start) { }

        /// <summary>
        /// Internal constructior used for testing.
        /// </summary>
        /// <param name="context">The <see cref="ICloudSourceContext"/> to get the current project id from.</param>
        /// <param name="startProcess">The injected mock implementation of <see cref="Process.Start(string)"/>.</param>
        internal ConsoleLinksRoot(ICloudSourceContext context, Func<string, Process> startProcess)
        {
            _context = context;
            _startProcess = startProcess;
            Caption = s_consoleHomeFormatInfo.Caption;
            NavigateCommand = new ProtectedCommand(OnNavigateCommand);

            foreach ((string path, string caption) tuple in s_primaryConsoleLinkPaths)
            {
                LinkInfo linkInfo = PathTupleToLinkInfo(tuple);
                ConsoleLink consoleLink = s_helpLinks.ContainsKey(tuple.path) ?
                    new ConsoleLink(_context, linkInfo, s_helpLinks[tuple.path]) :
                    new ConsoleLink(_context, linkInfo);

                Children.Add(consoleLink);
            }

            foreach ((string groupCaption, IEnumerable<LinkInfo> linkInfos) in GroupedConsoleLinkFormats)
            {
                Children.Add(new ConsoleLinkGroup(groupCaption, _context, linkInfos));
            }
        }

        private static LinkInfo PathTupleToLinkInfo((string path, string caption) tuple) =>
            new LinkInfo(CloudConsoleRootUrl + tuple.path + ProjectUrlArgumentFormat, tuple.caption);

        public void Refresh() { }

        public void InvalidateProjectOrAccount() { }

        private void OnNavigateCommand() =>
            _startProcess(string.Format(s_consoleHomeFormatInfo.NavigateUrl, _context.CurrentProject?.ProjectId));
    }
}