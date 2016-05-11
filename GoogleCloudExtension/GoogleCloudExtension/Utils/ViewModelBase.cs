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


namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class is to be used as the base class for all view models for the extension.
    /// Provies useful properties common to almost all view models (such as a loading state) as well
    /// as whether gcloud is installed or not.
    /// </summary>
    public partial class ViewModelBase : Model
    {
        private bool _isLoading;
        private string _loadingMessage;

        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetValueAndRaise(ref _isLoading, value); }
        }

        public string LoadingMessage
        {
            get { return _loadingMessage; }
            set { SetValueAndRaise(ref _loadingMessage, value); }
        }
    }
}
