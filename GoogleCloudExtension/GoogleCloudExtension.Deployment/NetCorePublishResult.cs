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

namespace GoogleCloudExtension.Deployment
{
    public class NetCorePublishResult
    {
        public string ProjectId { get; }

        public string Service { get; }

        public string Version { get; }

        public bool Promoted { get; }

        public NetCorePublishResult(string projectId, string service, string version, bool promoted)
        {
            ProjectId = projectId;
            Service = service;
            Version = version;
            Promoted = promoted;
        }

        public string GetDeploymentUrl()
        {
            if (Promoted)
            {
                if (Service == "default")
                {
                    return $"https://{ProjectId}.appspot.com";
                }
                else
                {
                    return $"https://{Service}-{ProjectId}.appspot.com";
                }
            }
            else
            {
                return $"https://{Version}-{Service}-{ProjectId}.appspot.com";
            }
        }
    }
}
