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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.DataSources;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.PublishDialog.Steps.Flex
{
    /// <summary>
    /// Interaction logic for FlexStepContent.xaml
    /// </summary>
    public partial class FlexStepContent : UserControl, IStepContent<FlexStepViewModel>
    {
        public FlexStepContent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates a new step instance. This method will also create the necessary view and conect both
        /// objects together.
        /// </summary>
        public FlexStepContent(
            IPublishDialog publishDialog,
            IGaeDataSource dataSource = null,
            Func<Project> pickProjectPrompt = null,
            Func<Task<bool>> setAppRegionAsyncFunc = null) : this()
        {
            ViewModel = new FlexStepViewModel(
                dataSource, pickProjectPrompt, setAppRegionAsyncFunc, publishDialog);
        }

        public FlexStepViewModel ViewModel
        {
            get => DataContext as FlexStepViewModel;
            private set => DataContext = value;
        }
    }
}
