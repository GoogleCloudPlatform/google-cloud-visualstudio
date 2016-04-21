// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.


using Google;
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
