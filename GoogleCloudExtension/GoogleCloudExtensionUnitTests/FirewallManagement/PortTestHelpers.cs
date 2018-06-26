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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.FirewallManagement;
using System.Collections.Generic;

namespace GoogleCloudExtensionUnitTests.FirewallManagement
{
    internal static class PortTestHelpers
    {
        public static Instance GetInstanceWithEnabledPort(PortInfo enabledPort, string instanceName = "name") =>
            new Instance
            {
                Name = instanceName,
                Tags = new Tags { Items = new List<string> { enabledPort.GetTag(instanceName) } }
            };
    }
}