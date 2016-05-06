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

using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// The type of operation.
    /// </summary>
    public enum OperationType
    {
        StopInstance,
        StartInstance,
    }

    /// <summary>
    /// This class represents a an operation on a Google Compute Engine VM instance.
    /// </summary>
    public class GceOperation
    {
        /// <summary>
        /// The type of operation this instance represents.
        /// </summary>
        public OperationType OperationType { get; }

        /// <summary>
        /// The project id on which this operation was created.
        /// </summary>
        public string ProjectId { get; }

        /// <summary>
        /// The zone in which this operation was created.
        /// </summary>
        public string ZoneName { get; }

        /// <summary>
        /// The name for the instance for which this operation is being done.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The task that will be completed once the operaiton is finished.
        /// </summary>
        public Task OperationTask { get; internal set; }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="operationType"></param>
        /// <param name="projectId"></param>
        /// <param name="zoneName"></param>
        /// <param name="name"></param>
        public GceOperation(OperationType operationType, string projectId, string zoneName, string name)
        {
            OperationType = operationType;
            ProjectId = projectId;
            ZoneName = zoneName;
            Name = name;
        }
    }
}
