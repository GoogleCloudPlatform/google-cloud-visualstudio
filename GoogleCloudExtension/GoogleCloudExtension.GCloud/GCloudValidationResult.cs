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

using System;

namespace GoogleCloudExtension.GCloud
{
    public class GCloudValidationResult
    {
        public Version CloudSdkVersion { get; }

        public bool IsCloudSdkInstalled { get; }

        public bool IsCloudSdkUpdated { get; }

        public bool IsRequiredComponentInstalled { get; }

        public bool IsValid => IsCloudSdkInstalled && IsCloudSdkUpdated && IsRequiredComponentInstalled;

        public GCloudValidationResult(
            bool isCloudSdkInstalled = false,
            bool isCloudSdkUpdated = false,
            bool isRequiredComponentInstalled = false,
            Version cloudSdkVersion = null)
        {
            IsCloudSdkInstalled = isCloudSdkInstalled;
            IsCloudSdkUpdated = isCloudSdkUpdated;
            IsRequiredComponentInstalled = isRequiredComponentInstalled;
            CloudSdkVersion = cloudSdkVersion;
        }
    }
}