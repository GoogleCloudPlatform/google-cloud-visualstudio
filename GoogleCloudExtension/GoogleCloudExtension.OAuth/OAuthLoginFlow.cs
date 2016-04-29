﻿// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

        public OAuthLoginFlow(OAuthCredentials credentials, IEnumerable<string> scopes)
        {
            _credentials = credentials;
            _scopes = scopes;
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
            var selectedPort = await SelectPort();
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

                    // Main task for waiting for the request.
                    var contextTask = listener.GetContextAsync();

                    // Poll the context task, waiting for it to complete, periodically checking if the
                    // cancellation token was set.
                    while (true)
                    {
                        // Check that the task has been completed and we have a context.
                        var completedTask = await Task.WhenAny(contextTask, Task.Delay(500));
                        if (completedTask == contextTask)
                        {
                            Debug.WriteLine("Waiting for oauth code complete.");
                            break;
                        }

                        // Check the cancellation token to see if we need to stop the operation.
                        token.ThrowIfCancellationRequested();
                    }

                    var context = await contextTask;
                    var accessCode = context.Request.QueryString["code"];
                    var error = context.Request.QueryString["error"];

                    // Creates an appropiate response for success or failure.
                    // TODO(ivann): Redirect to an appropiate page.
                    var response = context.Response;
                    response.StatusCode = 200;
                    response.ContentType = "text/html";
                    byte[] buff;
                    if (!String.IsNullOrEmpty(accessCode))
                    {
                        // Success path.
                        buff = Encoding.UTF8.GetBytes("<html><title>Done</title><body>You are done, thank you.</body></html>");
                    }
                    else
                    {
                        // Error path.
                        buff = Encoding.UTF8.GetBytes("<html><title>Error</title><body>You are not authenticated.</body></html>");
                    }

                    response.ContentLength64 = buff.Length;
                    using (var stream = response.OutputStream)
                    {
                        stream.Write(buff, 0, buff.Length);
                        stream.Close();
                    }

                    return new FlowResult { AccessCode = accessCode, Error = error };
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
        /// <returns></returns>
        private Task<int> SelectPort()
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
    }
}
