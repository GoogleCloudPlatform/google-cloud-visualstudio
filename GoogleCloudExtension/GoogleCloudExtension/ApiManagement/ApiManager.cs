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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.ProgressDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ApiManagement
{
    /// <summary>
    /// This class helps manage the APIs required by the various features in the extension. This class is defined as a singleton
    /// that manages its own <seealso cref="ServiceManagementDataSource"/> instance. This class will update itself when the user
    /// changes the current project/user.
    /// </summary>
    public class ApiManager : IApiManager
    {
        private static readonly Lazy<ApiManager> s_defaultManager = new Lazy<ApiManager>(CreateApiManager);

        /// <summary>
        /// The singleton instance for this class.
        /// </summary>
        public static ApiManager Default => s_defaultManager.Value;

        private Lazy<ServiceManagementDataSource> _dataSource;

        private ApiManager()
        {
            CredentialsStore.Default.CurrentAccountChanged += OnCurrentCredentialsChanged;
            CredentialsStore.Default.CurrentProjectIdChanged += OnCurrentCredentialsChanged;
            CredentialsStore.Default.Reset += OnCurrentCredentialsChanged;
            _dataSource = new Lazy<ServiceManagementDataSource>(CreateDataSource);
        }

        private ApiManager(string projectId)
        {
            _dataSource = new Lazy<ServiceManagementDataSource>(() => CreateDataSource(projectId));
        }

        /// <summary>
        /// Create an ApiManager instance for the given project id.
        /// </summary>
        /// <param name="projectId">GCP project id.</param>
        /// <returns>An instance of class ApiManager.</returns>
        public static IApiManager GetApiManager(string projectId)
        {
            return new ApiManager(projectId);
        }

        /// <summary>
        /// This method will check that all of the given service names are enabled.
        /// </summary>
        /// <param name="serviceNames">The list of services to check.</param>
        /// <returns>A task that will be true if all services are enabled, false otherwise.</returns>
        public async Task<bool> AreServicesEnabledAsync(IList<string> serviceNames)
        {
            if (serviceNames == null || serviceNames.Count == 0)
            {
                return true;
            }

            ServiceManagementDataSource dataSource = _dataSource.Value;
            if (dataSource == null)
            {
                return false;
            }

            IEnumerable<ServiceStatus> serviceStatus = await dataSource.CheckServicesStatusAsync(serviceNames);
            return serviceStatus.All(x => x.Enabled);
        }


        /// <summary>
        /// This method will check that all given services are enabled and if not will prompt the user to enable the
        /// necessary services.
        /// </summary>
        /// <param name="serviceNames">The services to check.</param>
        /// <param name="prompt">The prompt to use in the prompt dialog to ask the user for permission to enable the services.</param>
        /// <returns>A task that will be true if all services where enabled, false if the user cancelled or if the operation failed.</returns>
        public async Task<bool> EnsureAllServicesEnabledAsync(
            IEnumerable<string> serviceNames,
            string prompt)
        {
            ServiceManagementDataSource dataSource = _dataSource.Value;
            if (dataSource == null)
            {
                return false;
            }

            try
            {
                // Check all services in parallel.
                IList<string> servicesToEnable = (await dataSource.CheckServicesStatusAsync(serviceNames))
                    .Where(x => !x.Enabled)
                    .Select(x => x.Name)
                    .ToList();
                if (servicesToEnable.Count == 0)
                {
                    Debug.WriteLine("All the services are already enabled.");
                    return true;
                }

                // Need to enable the services, prompt the user.
                Debug.WriteLine($"Need to enable the services: {string.Join(",", servicesToEnable)}.");
                if (!UserPromptUtils.ActionPrompt(
                        prompt: prompt,
                        title: Resources.ApiManagerEnableServicesTitle,
                        actionCaption: Resources.UiEnableButtonCaption))
                {
                    return false;
                }

                // Enable all services in parallel.
                await ProgressDialogWindow.PromptUser(
                    dataSource.EnableAllServicesAsync(servicesToEnable),
                    new ProgressDialogWindow.Options
                    {
                        Title = Resources.ApiManagerEnableServicesTitle,
                        Message = Resources.ApiManagerEnableServicesProgressMessage,
                        IsCancellable = false
                    });
                return true;
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.ApiManagerEnableServicesErrorMessage,
                    title: Resources.UiErrorCaption,
                    errorDetails: ex.Message);
                return false;
            }
        }

        /// <summary>
        /// This method will enable the list of services given.
        /// </summary>
        /// <param name="serviceNames">The list of services to enable.</param>
        /// <returns>A task that will be completed once the operation finishes.</returns>
        public async Task EnableServicesAsync(IEnumerable<string> serviceNames)
        {
            ServiceManagementDataSource dataSource = _dataSource.Value;
            if (dataSource == null)
            {
                return;
            }

            try
            {
                await ProgressDialogWindow.PromptUser(
                    dataSource.EnableAllServicesAsync(serviceNames),
                    new ProgressDialogWindow.Options
                    {
                        Title = Resources.ApiManagerEnableServicesTitle,
                        Message = Resources.ApiManagerEnableServicesProgressMessage,
                        IsCancellable = false
                    });
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: Resources.ApiManagerEnableServicesErrorMessage,
                    title: Resources.UiErrorCaption,
                    errorDetails: ex.Message);
            }
        }

        private void OnCurrentCredentialsChanged(object sender, EventArgs e)
        {
            _dataSource = new Lazy<ServiceManagementDataSource>(CreateDataSource);
        }

        private static ApiManager CreateApiManager() => new ApiManager();

        private static ServiceManagementDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new ServiceManagementDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.VersionedApplicationName);
            }
            else
            {
                return null;
            }
        }

        private static ServiceManagementDataSource CreateDataSource(string projectId)
        {
            projectId.ThrowIfNullOrEmpty(nameof(projectId));
            return new ServiceManagementDataSource(
                projectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.VersionedApplicationName);
        }
    }
}
