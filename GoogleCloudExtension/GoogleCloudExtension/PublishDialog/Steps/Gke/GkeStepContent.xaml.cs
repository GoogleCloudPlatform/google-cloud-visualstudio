// Copyright 2017 Google Inc. All Rights Reserved.
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

using System.Windows.Controls;

namespace GoogleCloudExtension.PublishDialog.Steps.Gke
{
    /// <summary>
    /// Interaction logic for GkeStepContent.xaml
    /// </summary>
    public partial class GkeStepContent : UserControl, IStepContent<GkeStepViewModel>
    {

        public GkeStepContent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates a GKE step complete with behavior and visuals.
        /// </summary>
        public GkeStepContent(IPublishDialog publishDialog) : this()
        {
            ViewModel = new GkeStepViewModel(publishDialog);
        }

        public GkeStepViewModel ViewModel
        {
            get => DataContext as GkeStepViewModel;
            private set => DataContext = value;
        }
    }
}
