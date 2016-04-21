using Google;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public abstract class DataSourceBase<TService> where TService : BaseClientService
    {
        protected string ProjectId { get; }
        protected TService Service { get; }

        protected DataSourceBase(string projectId, Func<TService> factory)
        {
            ProjectId = projectId;
            Service = factory();
        }

        protected DataSourceBase(Func<TService> factory): this(null, factory)
        { }

        protected static async Task<IList<TItem>> LoadPagedListAsync<TItem, TItemsPage>(
            Func<string, Task<TItemsPage>> fetchPageFunc,
            Func<TItemsPage, IEnumerable<TItem>> itemsFunc,
            Func<TItemsPage, string> nextPageTokenFunc)
        {
            try
            {
                var result = new List<TItem>();
                string nextPageToken = null;
                do
                {
                    var page = await fetchPageFunc(nextPageToken);
                    result.AddRange(itemsFunc(page) ?? Enumerable.Empty<TItem>());
                    nextPageToken = nextPageTokenFunc(page);
                } while (!String.IsNullOrEmpty(nextPageToken));

                return result;
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get page of items: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
