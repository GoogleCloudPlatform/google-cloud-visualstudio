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
        /// <summary>
        /// The display name for the Windows OS installed in the instance.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The major version for the Windows OS isntalled in the instance, for example 2012.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// The subversion for the Windows OS installed in the instance, for example RC2.
        /// </summary>
        public string Subversion { get; }

        public WindowsInstanceInfo(string displayName, string version, string subversion = null)
        {
            DisplayName = displayName;
            Version = version;
            Subversion = subversion;
        }
    }
}
