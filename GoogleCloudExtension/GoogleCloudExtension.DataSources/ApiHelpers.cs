using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    internal static class ApiHelpers
    {
        /// <summary>
        /// Implementation of the paging algorithm for all of the data sources. Uses <seealso cref="Func{T, TResult}"/> as 
        /// a way to customize the behavior.
        /// </summary>
        /// <typeparam name="TItem">The type if the element in the resulting collection.</typeparam>
        /// <typeparam name="TItemsPage">The type used to deserialize a page of items.</typeparam>
        /// <param name="client">The authenticated client to use to fetch data.</param>
        /// <param name="firstPageUrl">The url to use for the first page of data.</param>
        /// <param name="itemsFunc">The function that given the <typeparamref name="TItemsPage"/> instance returns the list of 
        /// <typeparamref name="TItem"/> for that page.
        /// </param>
        /// <param name="nextPageUrlFunc">The function that given the <typeparamref name="TItemsPage"/> instance returns the URL for 
        /// next page.</param>
        /// <returns></returns>
        public static async Task<IList<TItem>> LoadPagedListAsync<TItem, TItemsPage>(
            WebClient client,
            string firstPageUrl,
            Func<TItemsPage, IEnumerable<TItem>> itemsFunc,
            Func<TItemsPage, string> nextPageUrlFunc)
        {
            try
            {
                var result = new List<TItem>();
                var url = firstPageUrl;
                while (url != null)
                {
                    Debug.WriteLine($"Reading url: {url}");
                    var response = await client.DownloadStringTaskAsync(url);
                    var items = JsonConvert.DeserializeObject<TItemsPage>(response);
                    result.AddRange(itemsFunc(items) ?? Enumerable.Empty<TItem>());
                    url = nextPageUrlFunc(items);
                }
                return result;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Request failed: {ex.Message}");
                throw new DataSourceException(ex.Message);
            }
        }
    }
}
