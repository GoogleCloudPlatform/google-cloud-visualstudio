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
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorer.Options
{
    public class CloudExplorerOptionsPageViewModel : ViewModelBase
    {
        private IList<PubSubTopicRegex> _pubSubTopicFilters;

        public IList<PubSubTopicRegex> PubSubTopicFilters
        {
            get { return _pubSubTopicFilters; }
            set { SetValueAndRaise(ref _pubSubTopicFilters, value); }
        }

        public ProtectedCommand ResetToDefaults { get; }

        public CloudExplorerOptionsPageViewModel(Action resetSettings)
        {
            ResetToDefaults = new ProtectedCommand(resetSettings);
        }

        public class PubSubTopicRegex : ViewModelBase, IEditableObject
        {
            private string _regex;
            private string _uneditedRegex;

            public string Regex
            {
                get { return _regex; }
                set { SetValueAndRaise(ref _regex, value); }
            }

            public PubSubTopicRegex(string regex)
            {
                _regex = regex;
            }

            // Required for adding new rows to DataGrid.
            // ReSharper disable once UnusedMember.Global
            public PubSubTopicRegex() : this("") { }

            /// <inheritdoc />
            public override string ToString()
            {
                return Regex;
            }

            /// <inheritdoc />
            public void BeginEdit()
            {
                if (_uneditedRegex == null)
                {
                    _uneditedRegex = _regex;
                }
            }

            /// <inheritdoc />
            public void EndEdit()
            {
                _uneditedRegex = null;
            }

            /// <inheritdoc />
            public void CancelEdit()
            {
                if (_uneditedRegex != null)
                {
                    Regex = _uneditedRegex;
                    _uneditedRegex = null;
                }
            }
        }
    }
}
