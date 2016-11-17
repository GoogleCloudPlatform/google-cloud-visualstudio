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

using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    /// <summary>
    /// The root view for Google App Engine for the Cloud Explorer.
    /// </summary>
    internal class GaeSourceRootViewModel : SourceRootViewModelBase
    {
        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeLoadingServicesCaption,
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeNoServicesFoundCaption,
            IsWarning = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerGaeFailedToLoadServicesCaption,
            IsError = true
        };

        public Lazy<GaeDataSource> DataSource;

        public Application GaeApplication;

        public override string RootCaption => Resources.CloudExplorerGaeRootNodeCaption;

        public override TreeLeaf ErrorPlaceholder => s_errorPlaceholder;

        public override TreeLeaf LoadingPlaceholder => s_loadingPlaceholder;

        public override TreeLeaf NoItemsPlaceholder => s_noItemsPlacehoder;

        public override void Initialize(ICloudSourceContext context)
        {
            base.Initialize(context);

            InvalidateProjectOrAccount();

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.CloudExplorerStatusMenuHeader, Command = new ProtectedCommand(OnStatusCommand) },
                new MenuItem { Header = Resources.UiOpenOnCloudConsoleMenuHeader, Command = new ProtectedCommand(OnOpenOnCloudConsoleCommand) },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void OnStatusCommand()
        {
            Process.Start("https://status.cloud.google.com/");
        }

        private void OnOpenOnCloudConsoleCommand()
        {
            var url = $"https://console.cloud.google.com/appengine/services?project={Context.CurrentProject.ProjectId}";
            Process.Start(url);
        }

        public override void InvalidateProjectOrAccount()
        {
            Debug.WriteLine("New credentials, invalidating the Google Cloud App Engine source.");
            DataSource = new Lazy<GaeDataSource>(CreateDataSource);
        }

        private GaeDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new GaeDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.ApplicationName);
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
                Debug.WriteLine("Loading list of services.");
                Task<Application> gaeApplicationTask = DataSource.Value.GetApplicationAsync();
                Task<List<ServiceViewModel>> servicesTask = LoadServiceList();
                await Task.WhenAll(gaeApplicationTask, servicesTask);

                GaeApplication = gaeApplicationTask.Result;
                var services = servicesTask.Result;
                Children.Clear();
                if (services == null)
                {
                    Children.Add(s_errorPlaceholder);
                }
                else
                {
                    foreach (var item in services)
                    {
                        Children.Add(item);
                    }
                    if (Children.Count == 0)
                    {
                        Children.Add(s_noItemsPlacehoder);
                    }
                }
                EventsReporterWrapper.ReportEvent(GaeServicesLoadedEvent.Create(CommandStatus.Success));
            }
            catch (DataSourceException ex)
            {
                GcpOutputWindow.OutputLine(Resources.CloudExplorerGaeFailedServicesMessage);
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();

                EventsReporterWrapper.ReportEvent(GaeServicesLoadedEvent.Create(CommandStatus.Failure));
                throw new CloudExplorerSourceException(ex.Message, ex);
            }
        }

        private async Task<List<ServiceViewModel>> LoadServiceList()
        {
            var services = await DataSource.Value.GetServiceListAsync();
            return services?.Select(x => new ServiceViewModel(this, x)).ToList();
        }
    }
}
