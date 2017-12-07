﻿// Copyright 2017 Google Inc. All Rights Reserved.
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GoogleCloudExtension.CloudExplorer.Options
{
    public class CloudExplorerOptionsPageViewModel : ViewModelBase
    {
        private ObservableCollection<EditableModel<string>> _pubSubTopicFilters;

        public IEnumerable<EditableModel<string>> PubSubTopicFilters
        {
            get { return _pubSubTopicFilters; }
            set { SetValueAndRaise(ref _pubSubTopicFilters, new ObservableCollection<EditableModel<string>>(value)); }
        }

        public ProtectedCommand ResetToDefaults { get; }

        public CloudExplorerOptionsPageViewModel(Action resetSettings)
        {
            ResetToDefaults = new ProtectedCommand(resetSettings);
        }
    }
}
