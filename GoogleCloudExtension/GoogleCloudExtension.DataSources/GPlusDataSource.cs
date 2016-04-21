using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Plus.v1;
using Google.Apis.Plus.v1.Data;
using Google.Apis.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public class GPlusDataSource : DataSourceBase<PlusService>
    {
        public GPlusDataSource(GoogleCredential credential) : base(() => CreateService(credential))
        { }

        private static PlusService CreateService(GoogleCredential credentials)
        {
            return new PlusService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credentials
            });
        }

        public async Task<Person> GetProfileAsync()
        {
            try
            {
                return await Service.People.Get("me").ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get person: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
