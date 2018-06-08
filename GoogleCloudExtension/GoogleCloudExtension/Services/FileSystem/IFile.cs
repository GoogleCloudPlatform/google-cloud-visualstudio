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

using System.Collections.Generic;
using System.IO;

namespace GoogleCloudExtension.Services.FileSystem
{
    /// <summary>
    /// Interface for a file service that matches the static members of <see cref="System.IO.File"/>.
    /// </summary>
    public interface IFile
    {
        /// <inheritdoc cref="File.Exists(string)"/>
        bool Exists(string path);

        /// <inheritdoc cref="File.WriteAllText(string,string)"/>
        void WriteAllText(string path, string contents);

        /// <inheritdoc cref="File.ReadLines(string)"/>
        IEnumerable<string> ReadLines(string path);

        /// <inheritdoc cref="File.Copy(string, string, bool)"/>
        void Copy(string sourceFileName, string destFileName, bool overwrite);

        /// <inheritdoc cref="File.OpenText(string)"/>
        TextReader OpenText(string path);

        /// <inheritdoc cref="File.CreateText(string)"/>
        TextWriter CreateText(string path);
    }
}