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

using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.FirewallManagement
{
    /// <summary>
    /// This class is the model for the checkbox to enable/disable ports in the firwall.
    /// </summary>
    public class PortModel : Model
    {
        private readonly PortInfo _port;
        private readonly bool _originalIsEnabled;
        private bool _isEnabled;

        /// <summary>
        /// The underlying port info.
        /// </summary>
        public PortInfo PortInfo => _port;

        /// <summary>
        /// The name of the port.
        /// </summary>
        public string Name => _port.Name;

        /// <summary>
        /// The number of the port.
        /// </summary>
        public int Port => _port.Port;

        /// <summary>
        /// Whether the port is enabled or not.
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetValueAndRaise(ref _isEnabled, value); }
        }

        /// <summary>
        /// Whether the user actually changed the value of the enabled property.
        /// </summary>
        public bool Changed => IsEnabled != _originalIsEnabled;

        public PortModel(PortInfo port, bool enabled)
        {
            _port = port;
            _originalIsEnabled = enabled;
            _isEnabled = enabled;
        }
    }
}
