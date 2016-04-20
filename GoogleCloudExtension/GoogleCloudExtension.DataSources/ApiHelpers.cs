// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.


using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    internal static class ApiHelpers
    {
        /// <summary>
        /// Loads all of the pages of a paginated data source, returning a list of the individual
        /// items.
        /// </summary>
        /// <typeparam name="TItem">The type of the items in the pages.</typeparam>
        /// <typeparam name="TItemsPage">The type of the pages.</typeparam>
        /// <param name="fetchPageFunc">The func to call to fetch pages.</param>
        /// <param name="itemsFunc">The func to call to extract the items from the page.</param>
        /// <param name="nextPageTokenFunc">The func to call to get the next page token.</param>
        /// <returns></returns>
        public static async Task<IList<TItem>> NewLoadPagedListAsync<TItem, TItemsPage>(
            Func<string, Task<TItemsPage>> fetchPageFunc,
            Func<TItemsPage, IEnumerable<TItem>> itemsFunc,
            Func<TItemsPage, string> nextPageTokenFunc)
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
                throw new DataSourceException(ex.Message, ex);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse response: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
