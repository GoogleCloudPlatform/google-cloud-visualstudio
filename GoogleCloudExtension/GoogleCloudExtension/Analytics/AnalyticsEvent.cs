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

using System.Collections.Generic;
using System.Diagnostics;

namespace GoogleCloudExtension.Analytics
{
    internal class AnalyticsEvent
    {
        private const string VersionName = "version";
        private const string VsVersionName = "vsversion";
        private const string VsEditionName = "vsedition";

        public string Name { get; }

        public Dictionary<string, string> Metadata { get; }

        public AnalyticsEvent(string name, params string[] metadata)
        {
            Name = name;
            Metadata = GetMetadataFromParams(metadata);
        }

        private static Dictionary<string, string> GetMetadataFromParams(string[] args)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (args.Length != 0)
            {
                if ((args.Length % 2) != 0)
                {
                    Debug.WriteLine($"Invalid count of params: {args.Length}");
                    return null;
                }

                for (int i = 0; i < args.Length; i += 2)
                {
                    result.Add(args[i], args[i + 1]);
                }
            }

            result[VersionName] = GoogleCloudExtensionPackage.ApplicationVersion;
            result[VsVersionName] = GoogleCloudExtensionPackage.VsVersion;
            result[VsEditionName] = GoogleCloudExtensionPackage.VsEdition;
            return result;
        }
    }
}
