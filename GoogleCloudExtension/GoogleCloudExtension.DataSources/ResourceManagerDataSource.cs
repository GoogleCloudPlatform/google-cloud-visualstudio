using Google.Apis.Auth.OAuth2;
using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class wraps the <seealso cref="Google.Apis.CloudResourceManager.v1.CloudResourceManagerService"/> class for
    /// a given set of credentials.
    /// </summary>
    public class ResourceManagerDataSource : DataSourceBase<CloudResourceManagerService>
    {
        /// <summary>
        /// The constructor for the class.
        /// </summary>
        /// <param name="credential"></param>
        public ResourceManagerDataSource(GoogleCredential credential) : base(() => CreateService(credential))
        { }

        private static CloudResourceManagerService CreateService(GoogleCredential credential)
        {
            return new CloudResourceManagerService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            });
        }

        /// <summary>
        /// Returns the complete list of projects for the current credentials.
        /// </summary>
        public Task<IList<Project>> GetProjectsListAsync()
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    if (String.IsNullOrEmpty(token))
                    {
                        return Service.Projects.List().ExecuteAsync();
                    }
                    else
                    {
                        var request = Service.Projects.List();
                        request.PageToken = token;
                        return request.ExecuteAsync();
                    }
                },
                x => x.Projects,
                x => x.NextPageToken);
        }
    }
}
