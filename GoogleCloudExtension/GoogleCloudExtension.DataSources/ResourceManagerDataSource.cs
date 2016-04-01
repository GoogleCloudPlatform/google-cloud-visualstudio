using GoogleCloudExtension.DataSources.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public static class ResourceManagerDataSource
    {
        public static Task<IList<GcpProject>> GetProjectsListAsync(string oauthToken)
        {
            var baseUrl = $"https://cloudresourcemanager.googleapis.com/v1/projects";
            var client = new WebClient().SetOauthToken(oauthToken);

            return ApiHelpers.LoadPagedListAsync<GcpProject, GcpProjectPage>(
                client,
                baseUrl,
                x => x.Items,
                x => String.IsNullOrEmpty(x.NextPageToken) ? null : $"{baseUrl}?pageToken={x.NextPageToken}");
        }
    }
}
