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

namespace GoogleCloudExtension.CloudExplorerSources.CloudConsoleLinks
{
    public class ConsoleLinksRoot : TreeHierarchy, ISourceRootViewModelBase
    {
        private const string CloudConsoleRootUrl = "https://console.cloud.google.com/";
        private const string ProjectUrlArgumentFormat = "?project={0}";

        private const string HomeUrl = CloudConsoleRootUrl + ProjectUrlArgumentFormat;
        private const string GettingStartedPath = "getting-started";
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
        private const string CloudFunctionsPath = "functions";
        private const string CloudTasksPath = "cloudtasks";
        private const string BigtablePath = "bigtable";
        private const string DatastorePath = "datastore";
        private const string CloudStoragePath = "storage";
        private const string CloudSqlPath = "sql";
        private const string SpannerPath = "spanner";
        private const string MemoryStorePath = "memorystore";
        private const string FileStorePath = "filestore";
        private const string VpcNetworkPath = "networking";
        private const string NetworkServicesPath = "net-services";
        private const string HybridConnectivityPath = "hybrid";
        private const string NetworkServiceTierPath = "net-tier";
        private const string NetworkSecurityPath = "net-security";
        private const string StackDriverMonitoringPath = "monitoring";
        private const string StackDriverDebugPath = "debug";
        private const string StackDriverTracePath = "traces";
        private const string StackDriverLoggingPath = "logs";
        private const string StackDriverErrorReportingPath = "errors";
        private const string StackDriverProfilerPath = "profiler";
        private const string SourceRepositoriesPath = "code";
        private const string EndpointsPath = "endpoints";
        private const string BigQueryPath = "bigquery";
        private const string PubSubPath = "cloudpubsub";
        private const string DataprocPath = "dataproc";
        private const string DataflowPath = "dataflow";
        private const string MachineLearningEnginePath = "mlengine";
        private const string IoTCorePath = "iot";
        private const string ComposerPath = "composer";
        private const string GenomicsPath = "genomics";
        private const string DataprepPath = "dataprep";

        internal static readonly LinkInfo s_consoleHomeFormatInfo = new LinkInfo(
            HomeUrl, Resources.CloudExplorerConsoleLinkCaption);

        internal static readonly IReadOnlyList<LinkInfo> s_primaryConsoleLinkFormats = new[]
        {
            new LinkInfo(
                CloudConsoleRootUrl + GettingStartedPath + ProjectUrlArgumentFormat,
                Resources.CloudLinkGettingStartedCaption),
            new LinkInfo(
                CloudConsoleRootUrl + AppEnginePath + ProjectUrlArgumentFormat, Resources.CloudLinkAppEngineCaption),
            new LinkInfo(
                CloudConsoleRootUrl + ComputeEnginePath + ProjectUrlArgumentFormat,
                Resources.CloudLinkComputeEngineCaption),
            new LinkInfo(
                CloudConsoleRootUrl + KubernetesEnginePath + ProjectUrlArgumentFormat,
                Resources.CloudLinkKubernetesEngineCaption)
        };

        internal static readonly IReadOnlyList<Tuple<string, IReadOnlyList<LinkInfo>>>
            s_groupedConsoleLinkFormats = new[]
            {
                Tuple.Create<string, IReadOnlyList<LinkInfo>>(
                    Resources.ConsoleLinkProjectGroupCaption, new[]
                    {
                        new LinkInfo(
                            CloudConsoleRootUrl + CloudLauncherPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkCloudLauncherCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + IAmPath + ProjectUrlArgumentFormat, Resources.CloudLinkIAmCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + BillingPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkBillingCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + ApisAndServicesPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkApisAndServicesCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + SupportPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkSupportCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + SecurityCenterPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkSecurityCenterCaption)
                    }),
                Tuple.Create<string, IReadOnlyList<LinkInfo>>(
                    Resources.ConsoleLinkComputeGroupCaption, new[]
                    {
                        new LinkInfo(
                            CloudConsoleRootUrl + AppEnginePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkAppEngineCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + ComputeEnginePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkComputeEngineCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + KubernetesEnginePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkKubernetesEngineCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + CloudFunctionsPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkCloudFunctionsCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + CloudTasksPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkCloudTasksCaption)
                    }),
                Tuple.Create<string, IReadOnlyList<LinkInfo>>(
                    Resources.ConsoleLinkStorageGroupCaption, new[]
                    {
                        new LinkInfo(
                            CloudConsoleRootUrl + BigtablePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkBigtableCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + DatastorePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkDatastoreCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + CloudStoragePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkCloudStorageCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + CloudSqlPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkCloudSqlCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + SpannerPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkSpannerCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + MemoryStorePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkMemoryStoreCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + FileStorePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkFileStoreCaption)
                    }),
                Tuple.Create<string, IReadOnlyList<LinkInfo>>(
                    Resources.ConsoleLinkNetworkingGroupCaption, new[]
                    {
                        new LinkInfo(
                            CloudConsoleRootUrl + VpcNetworkPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkVpcNetworkCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + NetworkServicesPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkNetworkServicesCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + HybridConnectivityPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkHybridConnectivityCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + NetworkServiceTierPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkNetworkServiceTierCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + NetworkSecurityPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkNetworkSecurityCaption)
                    }),
                Tuple.Create<string, IReadOnlyList<LinkInfo>>(
                    Resources.ConsoleLinkStackDriverGroupCaption, new[]
                    {
                        new LinkInfo(
                            CloudConsoleRootUrl + StackDriverMonitoringPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkStackDriverMonitoringCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + StackDriverDebugPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkStackDriverDebugCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + StackDriverTracePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkStackDriverTraceCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + StackDriverLoggingPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkStackDriverLoggingCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + StackDriverErrorReportingPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkStackDriverErrorReportingCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + StackDriverProfilerPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkStackDriverProfilerCaption)
                    }),
                Tuple.Create<string, IReadOnlyList<LinkInfo>>(
                    Resources.ConsoleLinkToolsGroupCaption, new[]
                    {
                        new LinkInfo(
                            CloudConsoleRootUrl + ContainerRegistryPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkContainerRegistryCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + SourceRepositoriesPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkSourceRepositoriesCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + DeploymentManagerPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkDeploymentManagerCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + EndpointsPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkEndpointsCaption)
                    }),
                Tuple.Create<string, IReadOnlyList<LinkInfo>>(
                    Resources.ConsoleLinkBigDataGroupCaption, new[]
                    {
                        new LinkInfo(
                            CloudConsoleRootUrl + BigQueryPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkBigQueryCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + PubSubPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkPubSubCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + DataprocPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkDataprocCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + DataflowPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkDataflowCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + MachineLearningEnginePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkMachineLearningEngineCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + IoTCorePath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkIoTCoreCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + ComposerPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkComposerCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + GenomicsPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkGenomicsCaption),
                        new LinkInfo(
                            CloudConsoleRootUrl + DataprepPath + ProjectUrlArgumentFormat,
                            Resources.CloudLinkDataprepCaption)
                    })
            };

        private readonly ICloudSourceContext _context;
        private readonly Func<string, Process> _startProcess;

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

            foreach (LinkInfo formatLinkInfo in s_primaryConsoleLinkFormats)
            {
                Children.Add(new ConsoleLink(formatLinkInfo, _context));
            }

            foreach (Tuple<string, IReadOnlyList<LinkInfo>> consoleLinkFormatsGroup in
                s_groupedConsoleLinkFormats)
            {
                IReadOnlyList<LinkInfo> groupLinks = consoleLinkFormatsGroup.Item2;
                string groupCaption = consoleLinkFormatsGroup.Item1;
                Children.Add(new ConsoleLinkGroup(groupCaption, _context, groupLinks));
            }
        }

        public void Refresh() { }

        public void InvalidateProjectOrAccount() { }

        private void OnNavigateCommand() =>
            _startProcess(string.Format(s_consoleHomeFormatInfo.NavigateUrl, _context.CurrentProject?.ProjectId));
    }
}