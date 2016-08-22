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
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.TerminalServer
{
    public class TerminalServerManagerWindow : CommonDialogWindowBase
    {
        private TerminalServerManagerViewModel ViewModel { get; }

        private TerminalServerManagerWindow(Instance instance):
            base(GoogleCloudExtension.Resources.TerminalServerManagerWindowTitle, 300, 150)
        {
            ViewModel = new TerminalServerManagerViewModel(instance, this);
            Content = new TerminalServerManagerWindowContent { DataContext = ViewModel };
        }

        public static void PromptUser(Instance instance)
        {
            var dialog = new TerminalServerManagerWindow(instance);
            dialog.ShowModal();
        }
    }
}
