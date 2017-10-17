using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
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

        public async Task<bool> EnsureServiceEnabledAsync(
            string serviceName,
            string displayName)
        {
            var dataSource = _dataSource.Value;
            if (dataSource == null)
            {
                return false;
            }

            if (await dataSource.IsServiceEnabledAsync(serviceName))
            {
                // Nothing to do.
                Debug.WriteLine($"The service {serviceName} is already enabled.");
                return true;
            }

            // Need to enable the service.
            Debug.WriteLine($"Need to enable the service {serviceName}.");
            if (!UserPromptUtils.ActionPrompt(
                    prompt: $"Do you want to enable the API {displayName}?",
                    title: "Enable needed API",
                    actionCaption: "Enable"))
            {
                return false;
            }

            await dataSource.EnableServiceAsync(serviceName);
            return true;
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
