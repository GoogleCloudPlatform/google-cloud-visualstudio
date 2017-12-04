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

using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GoogleCloudExtension.CloudExplorer.Options
{
    public class CloudExplorerOptions : UIElementDialogPage
    {
        private static readonly IReadOnlyList<string> s_defaultPubSubTopicFilters = new[]
        {
            "asia\\.gcr\\.io%2F",
            "eu\\.gcr\\.io%2F",
            "gcr\\.io%2F",
            "us\\.gcr\\.io%2F",
            "cloud-builds",
            "repository-changes\\."
        };

        private readonly CloudExplorerOptionsPage _child;

        /// <summary>
        /// The list of regexes used to filter pub sub topics.
        /// </summary>
        public IEnumerable<string> PubSubTopicFilters
        {
            get { return _child.ViewModel.PubSubTopicFilters.Select(s => s.Regex); }
            set
            {
                _child.ViewModel.PubSubTopicFilters = value
                    .Select(s => new CloudExplorerOptionsPageViewModel.PubSubTopicRegex(s)).ToList();
            }
        }

        /// <inheritdoc />
        public override object AutomationObject { get; }

        /// <inheritdoc />
        protected override UIElement Child => _child;

        public event EventHandler SavingSettingToStorage;

        public CloudExplorerOptions() : this(null)
        {
        }

        internal CloudExplorerOptions(CloudExplorerOptionsPage child)
        {
            _child = child ?? new CloudExplorerOptionsPage(this);
            AutomationObject = new SerializableCloudExplorerOptions(this);
        }

        /// <inheritdoc />
        public override void ResetSettings()
        {
            PubSubTopicFilters = s_defaultPubSubTopicFilters;
            base.ResetSettings();
        }

        /// <inheritdoc />
        public override void LoadSettingsFromStorage()
        {
            PubSubTopicFilters = s_defaultPubSubTopicFilters;
            base.LoadSettingsFromStorage();
        }

        /// <inheritdoc />
        public override void SaveSettingsToStorage()
        {
            SavingSettingToStorage?.Invoke(this, EventArgs.Empty);
            base.SaveSettingsToStorage();
        }
    }
}
