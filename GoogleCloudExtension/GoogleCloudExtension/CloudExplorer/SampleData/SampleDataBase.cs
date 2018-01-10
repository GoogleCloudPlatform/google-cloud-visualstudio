// Copyright 2018 Google Inc. All Rights Reserved.
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
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer.SampleData
{
    /// <summary>
    /// Common base class for all design time data.
    /// </summary>
    public abstract class SampleDataBase
    {
        private const string ContainerIconResourcePath = "CloudExplorerSources/Gce/Resources/zone_icon.png";
        private const string InstanceIconResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon_running.png";

        protected static readonly Lazy<ImageSource> s_containerIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(ContainerIconResourcePath));
        protected static readonly Lazy<ImageSource> s_instanceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(InstanceIconResourcePath));

        public IList<ButtonDefinition> Buttons { get; } = new List<ButtonDefinition>
        {
            new ButtonDefinition { Icon = s_containerIcon.Value },
            new ButtonDefinition { Icon = s_instanceIcon.Value }
        };
    }
}
