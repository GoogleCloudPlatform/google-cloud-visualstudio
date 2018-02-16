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
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.Analytics.AnalyticsOptInDialog
{
    /// <summary>
    /// View model for user control AnalyticsOptInWindowContent.xaml.
    /// </summary>
    public class AnalyticsOptInWindowViewModel : ViewModelBase
    {
        private readonly AnalyticsOptInWindow _owner;

        /// <summary>
        /// Result of the view model after the dialog window is closed. Remains
        /// false until an action buttion is clicked.
        /// </summary>
        public bool Result { get; private set; }

        /// <summary>
        /// Command for opting in into sharing Analytics info with Google.
        /// </summary>
        public ProtectedCommand OptInCommand { get; }

        public AnalyticsOptInWindowViewModel(AnalyticsOptInWindow owner)
        {
            _owner = owner.ThrowIfNull(nameof(owner));

            OptInCommand = new ProtectedCommand(OnOptInCommand);
        }

        private void OnOptInCommand()
        {
            Result = true;
            _owner.Close();
        }
    }    
}
