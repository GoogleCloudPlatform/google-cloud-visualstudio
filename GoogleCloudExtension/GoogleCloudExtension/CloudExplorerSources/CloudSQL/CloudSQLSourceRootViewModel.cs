// Copyright 2016 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudExplorerSources.CloudSQL
{
    /// <summary>
    /// The root view for Cloud SQL for the Cloud Explorer.
    /// </summary>
    internal class CloudSQLSourceRootViewModel : SourceRootViewModelBase
    {
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerSqlLoadingInstancesCaption,
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerSqlNoInstancesFoundCaption,
            IsWarning = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerSqlFailedToLoadInstancesCaption,
            IsError = true
        };

        private static readonly IList<string> s_requiredApis = new List<string>
        {
            // Need the Cloud SQL API.
            KnownApis.CloudSQLApiName
        };

        public Lazy<CloudSqlDataSource> DataSource;

        public override string RootCaption => Resources.CloudExplorerSqlRootNodeCaption;

        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;

        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;

        public override TreeLeaf ApiNotEnabledPlaceholder
            => new TreeLeaf
            {
                Caption = Resources.CloudExplorerSqlApiNotEnabledCaption,
                IsError = true,
                ContextMenu = new ContextMenu
                {
                    ItemsSource = new List<FrameworkElement>
                    {
                        new MenuItem
                        {
                            Header = Resources.CloudExplorerSqlEnableApiMenuHeader,
                            Command = new ProtectedCommand(OnEnableCloudSQLApi)
                        }
                    }
                }
            };

        public override IList<string> RequiredApis => s_requiredApis;

        public override void Initialize(ICloudSourceContext context)
        {
            base.Initialize(context);

            InvalidateProjectOrAccount();

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.CloudExplorerStatusMenuHeader, Command = new ProtectedCommand(OnStatusCommand) },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void OnStatusCommand()
        {
            Process.Start("https://status.cloud.google.com/");
        }

        public override void InvalidateProjectOrAccount()
        {
            Debug.WriteLine("New credentials, invalidating the Google Cloud SQL source.");
            DataSource = new Lazy<CloudSqlDataSource>(CreateDataSource);
        }

        private CloudSqlDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new CloudSqlDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.VersionedApplicationName);
            }
            else
            {
                return null;
            }
        }

        protected override async Task LoadDataOverride()
        {
            try
            {
                Debug.WriteLine("Loading list of instances.");
                var instances = await LoadInstanceList();
                Children.Clear();
                if (instances == null)
                {
                    Children.Add(s_errorPlaceholder);
                }
                else
                {
                    foreach (var item in instances)
                    {
                        Children.Add(item);
                    }
                    if (Children.Count == 0)
                    {
                        Children.Add(s_noItemsPlacehoder);
                    }
                }

                EventsReporterWrapper.ReportEvent(CloudSQLInstancesLoadedEvent.Create(CommandStatus.Success));
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.OutputLine(Resources.CloudExplorerSqlFailedMessage);
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                EventsReporterWrapper.ReportEvent(CloudSQLInstancesLoadedEvent.Create(CommandStatus.Failure));
                throw new CloudExplorerSourceException(ex.Message, ex);
            }
        }

        private async Task<List<InstanceViewModel>> LoadInstanceList()
        {
            var instances = await DataSource.Value.GetInstanceListAsync();
            return instances?.Select(x => new InstanceViewModel(this, x)).ToList();
        }

        private async void OnEnableCloudSQLApi()
        {
            await ApiManager.Default.EnableServicesAsync(s_requiredApis);
            Refresh();
        }
    }
}
