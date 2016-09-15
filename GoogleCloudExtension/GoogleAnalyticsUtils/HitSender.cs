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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace GoogleAnalyticsUtils
{
    internal class HitSender : IHitSender
    {
        private const string ProductionServerUrl = "https://ssl.google-analytics.com/collect";
        private const string DebugServerUrl = "https://ssl.google-analytics.com/debug/collect";

        private readonly Lazy<HttpClient> _httpClient;
        private readonly string _serverUrl;
        private readonly string _userAgent;

        public HitSender(bool debug, string userAgent)
        {
            _serverUrl = debug ? DebugServerUrl : ProductionServerUrl;
            _userAgent = userAgent;
            _httpClient = new Lazy<HttpClient>(CreateHttpClient);
        }

        /// <summary>
        /// Sends the hit data to the server.
        /// </summary>
        /// <param name="hitData">The hit data to be sent.</param>
        public async void SendHitData(Dictionary<string, string> hitData)
        {
            var client = _httpClient.Value;
            using (var form = new FormUrlEncodedContent(hitData))
            using (var response = await client.PostAsync(_serverUrl, form).ConfigureAwait(false))
            {
                DebugPrintAnalyticsOutput(response.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Debugging utility that will print out to the output window the result of the hit request.
        /// </summary>
        /// <param name="resultTask">The task resulting from the request.</param>
        [Conditional("DEBUG")]
        private async void DebugPrintAnalyticsOutput(Task<string> resultTask)
        {
            var result = await resultTask.ConfigureAwait(false);
            Debug.WriteLine($"Output of analytics: {result}");
        }

        private HttpClient CreateHttpClient()
        {
            var result = new HttpClient();
            if (_userAgent != null)
            {
                result.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
            }
            return result;
        }
    }
}
