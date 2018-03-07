using Google.Apis.Logging.v2.Data;
using Google.Apis.Logging.v2.Data.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Interface for a data source accessing
    /// <see href="https://cloud.google.com/logging/docs/api/reference/rest">Stackdriver Logging API</see>.
    /// Implemented by <see cref="LoggingDataSource"/>.
    /// </summary>
    public interface ILoggingDataSource
    {
        /// <summary>
        /// Returns the list of <see cref="MonitoredResourceDescriptor"/>.
        /// </summary>
        Task<IList<MonitoredResourceDescriptor>> GetResourceDescriptorsAsync();

        /// <summary>
        /// Returns the first page of log entries of the project id.
        /// </summary>
        /// <param name="filter">
        /// Optional. Refer to <see href="https://cloud.google.com/logging/docs/view/advanced_filters"/>.
        /// </param>
        /// <param name="orderBy">
        /// Optional, "timestamp desc" or "timestamp asc"
        /// </param>
        /// <param name="pageSize">
        /// Optional,
        /// If page size is not specified, a server side default value is used.
        /// </param>
        /// <param name="nextPageToken">
        /// Optional,
        /// The page token from last list request response.
        /// If the value is null, fetch the first page results.
        /// If the value is not null, it is hard requirement that the filter, orderBy and pageSize parameters
        /// must stay same as the prior call.
        /// </param>
        /// <param name="cancelToken">Optional. A cancellation token.</param>
        /// <returns>
        ///     <see ref="LogEntryRequestResult" /> object that contains log entries and next page token.
        /// </returns>
        Task<LogEntryRequestResult> ListLogEntriesAsync(
            string filter = null,
            string orderBy = null,
            int? pageSize = null,
            string nextPageToken = null,
            CancellationToken cancelToken = default(CancellationToken));

        /// <summary>
        /// Returns a list of log names of the current Google Cloud project.
        /// </summary>
        /// <param name="resourceType">The resource type, i.e gce_instance.</param>
        /// <param name="resourcePrefixList">
        /// Optional, can be null.
        /// A list of resource prefixes.
        /// As an example, for resource type app engine, the prefixes can be the module ids.
        /// </param>
        Task<IList<string>> ListProjectLogNamesAsync(
            string resourceType,
            IEnumerable<string> resourcePrefixList = null);

        /// <summary>
        /// List all resource keys for the project.
        /// </summary>
        Task<IList<ResourceKeys>> ListResourceKeysAsync();

        /// <summary>
        /// List all resource type values for the given resource type and resource key.
        /// </summary>
        /// <param name="resourceType">The resource type.</param>
        /// <param name="resourceKey">Optional, the resource key as prefix.</param>
        /// <returns>
        /// A task with result of a list of resource keys.
        /// </returns>
        Task<IList<string>> ListResourceTypeValuesAsync(string resourceType, string resourceKey = null);
    }
}