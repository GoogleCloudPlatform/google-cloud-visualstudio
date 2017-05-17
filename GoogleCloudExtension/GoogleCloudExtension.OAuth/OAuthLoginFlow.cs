﻿// Copyright 2016 Google Inc. All Rights Reserved.
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.OAuth
{
    /// <summary>
    /// This class manages the user flow of entering OAUTH credentials.
    /// </summary>
    public class OAuthLoginFlow
    {
        /// <summary>
        /// The result of a login flow.
        /// </summary>
        private class FlowResult
        {
            /// <summary>
            /// The access code sent by the server in case of success.
            /// </summary>
            public string AccessCode { get; set; }

            /// <summary>
            /// The error message sent by the server in case of failure.
            /// </summary>
            public string Error { get; set; }
        }

        private readonly OAuthCredentials _credentials;
        private readonly IEnumerable<string> _scopes;
        private readonly string _successUrl;
        private readonly string _failureUrl;

        public OAuthLoginFlow(
            OAuthCredentials credentials,
            IEnumerable<string> scopes,
            string successUrl,
            string failureUrl)
        {
            _credentials = credentials;
            _scopes = scopes;
            _successUrl = successUrl;
            _failureUrl = failureUrl;
        }

        /// <summary>
        /// Runs the login flow, opening the default browser and starting a <seealso cref="HttpListener"/> instance
        /// waiting on a random port to receive the server response. This method supports cancellation and will throw
        /// <seealso cref="OperationCanceledException"/> if the operation is cancelled.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A task that will contain the refresh token in case of login success or null if user failed to login.</returns>
        public async Task<string> RunFlowAsync(CancellationToken token)
        {
            var selectedPort = await SelectPortAsync();
            var redirectUrl = GetRedirectUrl(selectedPort);
            Debug.WriteLine($"Using redirect url {redirectUrl}");

            var initialUrl = OAuthManager.GetInitialOAuthUrl(_credentials, redirectUrl, _scopes);

            // 1) Start the listener, so it is ready by the time the browser opens.
            var accessCodeTask = Task.Run(async () =>
            {
                var result = await RunListener(redirectUrl, token);
                return result.AccessCode;
            });

            // 2) Start the browser in parallel.
            Debug.WriteLine($"Starting browser on url {initialUrl}");
            Process.Start(initialUrl);

            // 3) Receive the access code back from the listener, or throw OperationCancelledException if
            // the operation is cancelled.
            var accessCode = await accessCodeTask;
            if (accessCode == null)
            {
                Debug.WriteLine("The login flow was canceled.");
                return null;
            }

            return await OAuthManager.EndOAuthFlow(_credentials, redirectUrl, accessCode);
        }

        private string GetRedirectUrl(object selectedPort) => $"http://localhost:{selectedPort}/";

        /// <summary>
        /// This method is designed to run on a background thread, running a most simple Httplistener
        /// waiting for the OAUTH code to come back. This method supports cancellation through the provided
        /// <paramref name="token"/> and will throw OperationCancelledException if the operation is cancelled.
        /// </summary>
        private async Task<FlowResult> RunListener(string redirectUrl, CancellationToken token)
        {
            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(redirectUrl);
                try
                {
                    listener.Start();
                    Debug.WriteLine($"Starting listening for OAUTH code in url {redirectUrl}");

                    while (true)
                    {
                        // Wait for the context and possibly the cancellation of the operation.
                        var contextTask = await GetCancellableTaskAsync(
                            listener.GetContextAsync(),
                            token);

                        // Awaits the resulting task. If the task is the cancelled one it will throw, otherwise
                        // it will just return the context.
                        var context = await contextTask;

                        // Ensure that this is the URL that we expect, Internet Explorer, and possibly other browsers,
                        // might ask for the favicon or other metadata pages before actually redirecting with the code.
                        if (context.Request.Url.AbsolutePath != "/")
                        {
                            Debug.WriteLine($"Got request for path {context.Request.Url}");
                            using (var response = context.Response)
                            {
                                response.StatusCode = 404;
                            }
                            continue;
                        }

                        var accessCode = context.Request.QueryString["code"];
                        var error = context.Request.QueryString["error"];

                        // Redirect to the appropiate website depending on success or failure of the login
                        // operation.
                        using (var response = context.Response)
                        {
                            Debug.WriteLineIf(String.IsNullOrEmpty(accessCode), $"Failed to authenticate the user OAUTH login flow.");
                            response.StatusCode = 303;
                            response.RedirectLocation = String.IsNullOrEmpty(accessCode) ? _failureUrl : _successUrl;
                        }

                        // Wait for a bit for the reponse to be sent.
                        await Task.Delay(500);

                        return new FlowResult { AccessCode = accessCode, Error = error };
                    }
                }
                finally
                {
                    Debug.WriteLine($"Shutting down listener for redirect url {redirectUrl}");
                    listener.Stop();
                }
            }
        }

        /// <summary>
        /// This method selects a port on which to run.
        /// </summary>
        private Task<int> SelectPortAsync()
        {
            return Task.Run(() =>
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                try
                {
                    listener.Start();
                    return ((IPEndPoint)listener.LocalEndpoint).Port;
                }
                finally
                {
                    listener.Stop();
                }
            });
        }

        /// <summary>
        /// Returns a task that can be awaited to get the task with the data. (There are two levels of await here). If
        /// the operation completed normally then <paramref name="sourceTask"/> is returend and when awaited the result of the
        /// operation is returned. If the operation is cancelled then a cancelled dummy task is returned, when awaited 
        /// <seealso cref="OperationCanceledException"/> is thrown. This way any task can be made cancellable if the original 
        /// source of the task doesn't support cancellation.
        /// </summary>
        private static Task<Task<T>> GetCancellableTaskAsync<T>(Task<T> sourceTask, CancellationToken token)
        {
            var taskSource = new TaskCompletionSource<T>();
            token.Register(() => taskSource.TrySetCanceled());
            return Task.WhenAny<T>(sourceTask, taskSource.Task);
        }
    }
}
