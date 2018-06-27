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
using GoogleCloudExtension.Utils;
using System;

namespace GoogleCloudExtension.FirewallManagement
{
    /// <summary>
    /// This class is the model for the checkbox to enable/disable ports in the firwall.
    /// </summary>
    public class PortModel : Model
    {
        private bool _isEnabled;

        /// <summary>
        /// The underlying port info.
        /// </summary>
        public PortInfo PortInfo { get; }

        /// <summary>
        /// The Google Compute Engine VM Instance.
        /// </summary>
        public Instance Instance { get; }

        /// <summary>
        /// The display string to user.
        /// </summary>
        public string DisplayString =>
            string.Format(Resources.PortManagerDisplayStringFormat, PortInfo.Description, PortInfo.Name, PortInfo.Port);

        /// <summary>
        /// Whether the port is enabled or not.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                SetValueAndRaise(ref _isEnabled, value);
                RaisePropertyChanged(nameof(Changed));
            }
        }

        /// <summary>
        /// Whether the user actually changed the value of the enabled property.
        /// </summary>
        public bool Changed => IsEnabled != IsEnabledOnInstance();


        public PortModel(PortInfo port, Instance instance)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            PortInfo = port ?? throw new ArgumentNullException(nameof(port));
            IsEnabled = IsEnabledOnInstance();
        }

        /// <summary>
        /// Returns the tag to be used for the port to target <see cref="Instance"/>.
        /// </summary>
        public string GetPortInfoTag() => PortInfo.GetTag(Instance.Name);

        private bool IsEnabledOnInstance() => Instance.Tags?.Items?.Contains(GetPortInfoTag()) ?? false;
    }
}
