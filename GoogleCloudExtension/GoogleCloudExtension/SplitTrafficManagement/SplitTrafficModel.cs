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

namespace GoogleCloudExtension.SplitTrafficManagement
{
    /// <summary>
    /// This class is the model for a GAE traffic allocation.
    /// </summary>
    public class SplitTrafficModel : Model
    {
        private string _versionId;
        private int _trafficAllocation;

        /// <summary>
        /// The id of the GAE version.
        /// </summary>
        public string VersionId
        {
            get { return _versionId; }
            set { SetValueAndRaise(out _versionId, value); }
        }

        /// <summary>
        /// The percentage allocation for the version as an integer.
        /// Such as: 100 -> 100% or 15 -> 15%.
        /// </summary>
        public int TrafficAllocation
        {
            get { return _trafficAllocation; }
            set { SetValueAndRaise(out _trafficAllocation, value); }
        }

        public SplitTrafficModel(string versionId, int trafficAllocation)
        {
            _versionId = versionId;
            _trafficAllocation = trafficAllocation;
        }
    }
}
