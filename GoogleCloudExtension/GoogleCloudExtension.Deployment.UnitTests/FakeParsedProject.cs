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

namespace GoogleCloudExtension.Deployment.UnitTests
{
    public class FakeParsedProject : IParsedProject
    {
        /// <summary>
        /// The name of the project.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The full path to the project, including the project file.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// The full path to the directory that contains the project file.
        /// </summary>
        public string DirectoryPath { get; set; }

        /// <summary>
        /// The type of the project.
        /// </summary>
        public KnownProjectTypes ProjectType { get; set; }
    }
}
