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

using System.Collections.Generic;
using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.SplitTrafficManagement
{
    /// <summary>
    /// This class is the dialog to manage traffic splitting for a GAE instance.
    /// </summary>
    public class SplitTrafficWindow : CommonDialogWindowBase
    {
        private SplitTrafficViewModel ViewModel =>
            (SplitTrafficViewModel)((SplitTrafficWindowContent)Content).DataContext;

        public SplitTrafficWindow(Service service, IEnumerable<Version> versions) :
            base(GoogleCloudExtension.Resources.SplitTrafficWindowTitle)
        {
            Content = new SplitTrafficWindowContent
            {
                DataContext = new SplitTrafficViewModel(this, service, versions)
            };
        }

        /// <summary>
        /// Shows the dialog to the user.
        /// </summary>
        /// <param name="instance">The instance on which to managed traffic splitting</param>
        /// <returns>The split traffic changes or null if the user cancelled the dialog.</returns>
        public static SplitTrafficChange PromptUser(Service service, IEnumerable<Version> versions)
        {
            SplitTrafficWindow dialog = new SplitTrafficWindow(service, versions);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
