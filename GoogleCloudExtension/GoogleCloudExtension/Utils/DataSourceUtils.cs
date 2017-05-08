using Google.Apis.Auth.OAuth2;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;

namespace GoogleCloudExtension.Utils
{
    public static class DataSourceUtils
    {
        public static ResourceManagerDataSource CreateResourceManagerDataSource()
        {
            GoogleCredential currentCredential = CredentialsStore.Default.CurrentGoogleCredential;
            if (currentCredential != null)
            {
                return new ResourceManagerDataSource(
                    currentCredential, GoogleCloudExtensionPackage.VersionedApplicationName);
            }
            else
            {
                return null;
            }
        }
    }
}