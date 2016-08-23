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
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Theming;
using System;

namespace GoogleCloudExtension.WindowsCredentialsChooser
{
    public class WindowsCredentialsChooserWindow : CommonDialogWindowBase
    {
        public class Options
        {
            public string Title { get; set; }

            public string Message { get; set; }
        }

        private WindowsCredentialsChooserViewModel ViewModel { get; }

        private WindowsCredentialsChooserWindow(Instance instance, Options options) :
            base(options.Title, 300, 150)
        {
            ViewModel = new WindowsCredentialsChooserViewModel(instance, options, this);
            Content = new WindowsCredentialsChooserWindowContent { DataContext = ViewModel };
        }

        public static WindowsInstanceCredentials PromptUser(Instance instance, Options options)
        {
            var dialog = new WindowsCredentialsChooserWindow(instance, options);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
