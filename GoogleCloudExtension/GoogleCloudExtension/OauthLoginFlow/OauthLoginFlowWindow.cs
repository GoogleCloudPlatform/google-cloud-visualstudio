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

using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.OAuth;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace GoogleCloudExtension.OauthLoginFlow
{
    public class OAuthLoginFlowWindow : DialogWindow
    {
        // The amount of time to wait for the code to come back, if the user doesn't do anything.
        private static readonly TimeSpan s_accessTokenTimeout = new TimeSpan(0, 15, 0);

        private readonly OAuthLoginFlow _flow;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private OAuthLoginFlowViewModel ViewModel { get; }

        private OAuthLoginFlowWindow(OAuthCredentials credentials, IEnumerable<string> scopes)
        {
            ExtensionAnalytics.ReportScreen(nameof(OAuthLoginFlowWindow));

            _flow = new OAuthLoginFlow(credentials, scopes);

            Title = "Provide Credentials";
            Width = 300;
            Height = 300;

            ViewModel = new OAuthLoginFlowViewModel(this);
            var windowContent = new OauthLoginFlowWindowContent
            {
                DataContext = ViewModel,
            };

            Content = windowContent;

            StartLoginFlow();
        }

        private async void StartLoginFlow()
        {
            try
            {
                _tokenSource.CancelAfter(s_accessTokenTimeout);
                var refreshToken = await _flow.RunFlowAsync(_tokenSource.Token);
                Debug.WriteLine($"Got refresh token: {refreshToken}");
                ViewModel.RefreshCode = refreshToken;
                Close();
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine($"The login was cancelled: {ex.Message}");
            }
        }

        public static string PromptUser(OAuthCredentials credentials, IEnumerable<string> scopes)
        {
            var dialog = new OAuthLoginFlowWindow(credentials, scopes);
            dialog.ShowModal();
            return dialog.ViewModel.RefreshCode;
        }

        public void CancelOperation()
        {
            Debug.WriteLine("The user cancelled the operation.");
            _tokenSource.Cancel();
            ViewModel.RefreshCode = null;
            Close();
        }
    }
}
