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

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class represents a configuration to use for polling.
    /// </summary>
    public class PollingConfiguration
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
        public TimeSpan Interval;

        /// <summary>
        /// The total time spent in delays between requests before a timeout is considered to
        /// have occured.
        /// </summary>
        public TimeSpan Timeout;

        /// <summary>
        /// Create a new polling configuration.
        /// </summary>
        /// <param name="interval">Optional, The interval, defaults to <see cref="DefaultPollInterval"/> when unset or null</param>
        /// <param name="timeout">Optional, The timeout, defaults to <see cref="DefaultPollTimeout"/> when unset or null</param>
        public PollingConfiguration(TimeSpan? interval = null, TimeSpan? timeout = null)
        {
            Interval= interval ?? DefaultPollInterval;
            Timeout = timeout ?? DefaultPollTimeout;
        }
    }

    /// <summary>
    /// This class handles polling for a resource.
    /// </summary>
    public class Polling<T>
    {

        /// <summary>
        /// Delegate to fetch an updated resource.
        /// </summary>
        /// <param name="item">The current resource.</param>
        /// <returns>The updated resource.</returns>
        public delegate Task<T> Fetch(T item);

        /// <summary>
        /// Delegate to determine if polling should stop.
        /// </summary>
        /// <param name="item">The current resource.</param>
        /// <returns>True if the polling should stop.</returns>
        public delegate bool StopPolling(T item);

        /// <summary>
        /// Poll for a resource.
        /// </summary>
        /// <param name="task">The origional task to start from.</param>
        /// <param name="fetch">A delegate to fetch an updated resource.</param>
        /// <param name="stopPolling">A delegate to determine if polling should stop.</param>
        /// <param name="config">Optional, A polling configuration or the default if unset or null.</param>
        /// <param name="token">Optional, A cancelation token used to stop polling manually.</param>
        /// <exception cref="TimeoutException">If the polling passes the timeout threshold.</exception>>
        /// <returns></returns>
        public static async Task<T> Poll(Task<T> task, Fetch fetch, StopPolling stopPolling,
            PollingConfiguration config = null, CancellationToken? token = null)
        {
            // If no configuration is given use the default configuration.
            config = config ?? new PollingConfiguration();

            // Wait for the initial task to complete before polling.
            T result = await task;
            TimeSpan elapsed = TimeSpan.Zero;

            // Start polling for the resource.
            while (true)
            {
                // Check if the current result is in a stopable state.
                if (stopPolling.Invoke(result))
                {
                    break;
                }

                // If a cancellation token is present and a cancelation has been requested stop polling.
                if (token.HasValue && token.Value.IsCancellationRequested)
                {
                    break;
                }

                // If a timeout has occured throw the proper exception.
                if (elapsed >= config.Timeout)
                {
                    throw new TimeoutException();
                }

                // Wait and then poll.
                await Task.Delay(config.Interval);
                elapsed += config.Interval;
                result = await fetch.Invoke(result);
            }
            return result;
        }
    }
}
