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

using System;
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorer.Options
{
    /// <summary>
    /// The options that affect the Cloud Explorer window.
    /// </summary>
    public interface ICloudExplorerOptions
    {
        /// <summary>
        /// The list of regexes used to filter Pub/Sub topics in the Cloud Explorer.
        /// </summary>
        IEnumerable<string> PubSubTopicFilters { get; set; }

        /// <summary>
        /// Triggered before this page saves its settings to storage.
        /// </summary>
        event EventHandler SavingSettings;

        /// <summary>
        /// Resets all settings on this page to default.
        /// </summary>
        void ResetSettings();
    }
}