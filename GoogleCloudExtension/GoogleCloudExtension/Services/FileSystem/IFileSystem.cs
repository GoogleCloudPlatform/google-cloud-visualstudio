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


namespace GoogleCloudExtension.Services.FileSystem
{
    /// <summary>
    /// Service interface for performing file system operations.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// File operations. Matches the static members of <see cref="System.IO.File"/>
        /// </summary>
        IFile File { get; }

        /// <summary>
        /// XDocument load operations. Matches the static methods of <see cref="XDocument"/>
        /// </summary>
        IXDocument XDocument { get; }
    }
}