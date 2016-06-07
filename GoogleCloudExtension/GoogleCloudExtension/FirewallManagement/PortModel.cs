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
    public class PortModel : Model
    {
        private readonly PortInfo _port;
        private readonly bool _originalIsEnabled;
        private bool _isEnabled;

        public PortInfo PortInfo => _port;

        public string Name => _port.Name;

        public int Port => _port.Port;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetValueAndRaise(ref _isEnabled, value); }
        }

        public bool Changed => IsEnabled != _originalIsEnabled;

        public PortModel(PortInfo port, bool enabled)
        {
            _port = port;
            _originalIsEnabled = enabled;
            _isEnabled = enabled;
        }
    }
}
