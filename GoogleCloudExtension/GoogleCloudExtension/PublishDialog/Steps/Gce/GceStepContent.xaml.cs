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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using System;
using System.Windows.Controls;

namespace GoogleCloudExtension.PublishDialog.Steps.Gce
{
    /// <summary>
    /// Interaction logic for GceTargetWindowContent.xaml
    /// </summary>
    public partial class GceStepContent : UserControl, IStepContent<GceStepViewModel>
    {
        public GceStepContent()
        {
            InitializeComponent();
        }

        public GceStepContent(
            IPublishDialog publishDialog,
            IGceDataSource dataSource = null,
            Func<Google.Apis.CloudResourceManager.v1.Data.Project> pickProjectPrompt = null,
            IWindowsCredentialsStore currentWindowsCredentialStore = null,
            Action<Instance> manageCredentialsPrompt = null) : this()
        {
            ViewModel = new GceStepViewModel(
                dataSource, pickProjectPrompt,
                currentWindowsCredentialStore, manageCredentialsPrompt, publishDialog);
        }

        public GceStepViewModel ViewModel
        {
            get => DataContext as GceStepViewModel;
            private set => DataContext = value;
        }
    }
}
