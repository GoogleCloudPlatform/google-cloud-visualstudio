using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.ProgressDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ApiManagement
{
    public class ApiManager
    {
        private static readonly Lazy<ApiManager> s_defaultManager = new Lazy<ApiManager>(CreateApiManager);

        public static ApiManager Default => s_defaultManager.Value;

        private Lazy<ServiceManagementDataSource> _dataSource = new Lazy<ServiceManagementDataSource>(CreateDataSource);

        private ApiManager()
        {
            CredentialsStore.Default.CurrentAccountChanged += OnCurrentCredentialsChanged;
            CredentialsStore.Default.Reset += OnCurrentCredentialsChanged;
        }

        public async Task<bool> AreServicesEnabledAsync(IEnumerable<string> serviceNames)
        {
            var dataSource = _dataSource.Value;
            if (dataSource == null)
            {
                return false;
            }

            var serviceStatus = await dataSource.CheckServicesStatusAsync(serviceNames);
            return serviceStatus.All(x => x.Item2);
        }

        public Task<bool> EnsureServiceEnabledAsync(
            string serviceName,
            string prompt)
        {
            return EnsureAllServicesEnabledAsync(new List<string> { serviceName }, prompt);
        }

        public async Task<bool> EnsureAllServicesEnabledAsync(
            IEnumerable<string> serviceNames,
            string prompt)
        {
            var dataSource = _dataSource.Value;
            if (dataSource == null)
            {
                return false;
            }

            try
            {
                // Check all services in parallel.
                var servicesToEnable = (await dataSource.CheckServicesStatusAsync(serviceNames))
                    .Where(x => !x.Item2)
                    .Select(x => x.Item1);
                if (servicesToEnable.Count() == 0)
                {
                    Debug.WriteLine("All the services are already enabled.");
                    return true;
                }

                // Need to enable the services, prompt the user.
                Debug.WriteLine($"Need to enable the services: {string.Join(",", servicesToEnable)}.");
                if (!UserPromptUtils.ActionPrompt(
                        prompt: prompt,
                        title: "Enable Required Services",
                        actionCaption: "Enable"))
                {
                    return false;
                }

                // Enable all services in parallel.
                await ProgressDialogWindow.PromptUser(
                    dataSource.EnableAllServicesAsync(servicesToEnable),
                    new ProgressDialogWindow.Options
                    {
                        Title = "Enabling Services",
                        Message = "Enabling the necessary services.",
                        IsCancellable = false
                    });
                return true;
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: "Failed to enable the necessary services.",
                    title: Resources.UiErrorCaption,
                    errorDetails: ex.Message);
                return false;
            }
        }

        public async Task EnableServicesAsync(IEnumerable<string> serviceNames)
        {
            var dataSource = _dataSource.Value;
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
                        Title = "Enabling Services",
                        Message = "Enabling the necessary services.",
                        IsCancellable = false
                    });
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ErrorPrompt(
                    message: "Failed to enable the necessary services.",
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
    }
}
