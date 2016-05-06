// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Base class for all of the data sources, contains the credentials for the service and
    /// the common routines that most source will need, such as pagination.
    /// </summary>
    /// <typeparam name="TService">The type of the service that ultimately performs the API calls.</typeparam>
    public abstract class DataSourceBase<TService> where TService : BaseClientService
    {
        /// <summary>
        /// The project ID to use for this data source.
        /// </summary>
        protected string ProjectId { get; }

        /// <summary>
        /// The service wrapped by this data source.
        /// </summary>
        protected TService Service { get; }

        /// <summary>
        /// Initializes this class with the <paramref name="projectId"/> and uses <paramref name="factory"/> to
        /// create an instance of the service to wrap.
        /// </summary>
        /// <param name="projectId">The project id for this data source.</param>
        /// <param name="service">The service for this data source.</param>
        protected DataSourceBase(string projectId, TService service)
        {
            ProjectId = projectId;
            Service = service;
        }

        /// <summary>
        /// Initializes an instance of the data source with only a service, for those APIs that do
        /// not require a project id.
        /// </summary>
        /// <param name="service">The service for this data source.</param>
        protected DataSourceBase(TService service) : this(null, service)
        { }

        /// <summary>
        /// Loads all of the items from a paged api.
        /// </summary>
        /// <typeparam name="TItem">The type of the items in the pages.</typeparam>
        /// <typeparam name="TItemsPage">The type of the pages that contain the items.</typeparam>
        /// <param name="fetchPageFunc">The function to call, with a page token if known, to get a page of items.</param>
        /// <param name="itemsFunc">The function that given a page will return an <c>IEnumerable</c> of items.</param>
        /// <param name="nextPageTokenFunc">The function that given a page will return the token of the next page.</param>
        /// <returns>A list with all of the items downloaded, the combination of all of the pages.</returns>
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
