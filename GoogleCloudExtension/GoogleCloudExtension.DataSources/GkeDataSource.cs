using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Container.v1;
using Google.Apis.Container.v1.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that fecthes information from Google Container Engine.
    /// </summary>
    public class GkeDataSource : DataSourceBase<ContainerService>
    {
        private const string AllZonesValue = "-";

        public GkeDataSource(string projectId, GoogleCredential credential, string appName):
            base(projectId, credential, init => new ContainerService(init), appName)
        { }

        /// <summary>
        /// Lists all of the clusters in the current project.
        /// </summary>
        /// <returns>The list of clusters.</returns>
        public async Task<IList<Cluster>> GetClusterListAsync()
        {
            try
            {
                var response = await Service.Projects.Zones.Clusters.List(ProjectId, AllZonesValue).ExecuteAsync();
                return response.Clusters;
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to list clusters: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
