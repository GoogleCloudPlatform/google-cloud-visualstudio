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

namespace GoogleCloudExtension.DataSources
{
    public class WindowsInstanceInfo
    {
        public string DisplayName { get; }

        public string Version { get; }

        public string SubVersion { get; }

        public WindowsInstanceInfo(string displayName, string version, string subVersion)
        {
            DisplayName = displayName;
            Version = version;
            SubVersion = subVersion;
        }
    }
}
