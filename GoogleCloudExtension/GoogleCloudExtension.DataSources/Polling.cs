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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class represents a configuration to use for polling.
    /// </summary>
    internal class PollingConfiguration
    {
        /// <summary>
        /// The default polling interval.  This value is the delay between poll requests.
        /// Default is 1 second.
        /// </summary>
        public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The default polling timeout.  This value is the total time spent in delays before a timeout 
        /// is considerd to have occured.  This does not take into account any other time spent.
        /// Default is 5 minutes.
        /// </summary>
        public static readonly TimeSpan DefaultPollTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The delay between poll requests.
        /// </summary>
        public readonly TimeSpan Interval;

        /// <summary>
        /// The total time spent in delays between requests before a timeout is considered to
        /// have occured.
        /// </summary>
        public readonly TimeSpan Timeout;

        /// <summary>
        /// Create a new polling configuration.
        /// </summary>
        /// <param name="interval">Optional, The interval, defaults to <see cref="DefaultPollInterval"/> when unset or null</param>
        /// <param name="timeout">Optional, The timeout, defaults to <see cref="DefaultPollTimeout"/> when unset or null</param>
        public PollingConfiguration(TimeSpan? interval = null, TimeSpan? timeout = null)
        {
            Interval = interval ?? DefaultPollInterval;
            Timeout = timeout ?? DefaultPollTimeout;
        }
    }

    /// <summary>
    /// This class handles polling for a resource.
    /// </summary>
    internal class Polling<T>
    {
        /// <summary>
        /// Poll for a resource.
        /// </summary>
        /// <param name="resource">The origional resource to start from.</param>
        /// <param name="fetch">A delegate to fetch an updated resource.</param>
        /// <param name="stopPolling">A delegate to determine if polling should stop.</param>
        /// <param name="config">Optional, A polling configuration or the default if unset or null.</param>
        /// <param name="token">Optional, A cancelation token used to stop polling manually.</param>
        /// <exception cref="TimeoutException">If the polling passes the timeout threshold.</exception>>
        /// <exception cref="OperationCanceledException">If the operation is canceled.</exception>>
        public static async Task<T> Poll(T resource, Func<T, Task<T>> fetch, Predicate<T> stopPolling,
            PollingConfiguration config = null, CancellationToken token = default(CancellationToken))
        {
            // If no configuration is given use the default configuration.
            config = config ?? new PollingConfiguration();

            TimeSpan elapsed = TimeSpan.Zero;

            // Start polling for the resource.
            while (true)
            {
                // Check if the current result is in a stopable state.
                if (stopPolling.Invoke(resource))
                {
                    break;
                }

                // If a cancellation token is present and a cancelation has been requested throw.
                token.ThrowIfCancellationRequested();

                // If a timeout has occured throw the proper exception.
                if (elapsed >= config.Timeout)
                {
                    throw new TimeoutException();
                }

                // Wait and then poll.
                await Task.Delay(config.Interval);
                elapsed += config.Interval;

                // Fetch the next resource.
                Task<T> task = fetch.Invoke(resource);
                if (await Task.WhenAny(task, Task.Delay(config.Timeout - elapsed)) == task)
                {
                    resource = await task;
                }
                else
                {
                    throw new TimeoutException();
                }
            }
            return resource;
        }
    }
}
