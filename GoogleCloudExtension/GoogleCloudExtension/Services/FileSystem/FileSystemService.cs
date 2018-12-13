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

using System;
using System.ComponentModel.Composition;

namespace GoogleCloudExtension.Services.FileSystem
{
    /// <summary>
    /// Service for performing file system operations.
    /// </summary>
    [Export(typeof(IFileSystem))]
    public class FileSystemService : IFileSystem
    {
        private readonly Lazy<IFile> _file;
        private readonly Lazy<IXDocument> _xDocument;
        private readonly Lazy<IDirectory> _directory;
        private readonly Lazy<IPath> _path;

        [ImportingConstructor]
        public FileSystemService(Lazy<IFile> file, Lazy<IXDocument> xDocument, Lazy<IDirectory> directory, Lazy<IPath> path)
        {
            _file = file;
            _xDocument = xDocument;
            _directory = directory;
            _path = path;
        }

        /// <summary>
        /// File operations. Matches the static methods of <see cref="System.IO.File"/>.
        /// </summary>
        public IFile File => _file.Value;

        /// <summary>
        /// XDocument load operations. Matches the static methods of <see cref="System.Xml.Linq.XDocument"/>.
        /// </summary>
        public IXDocument XDocument => _xDocument.Value;

        /// <summary>
        /// Directory operations. Matches the static members of <see cref="System.IO.Directory"/>.
        /// </summary>
        public IDirectory Directory => _directory.Value;

        /// <summary>
        /// Path operations. Matches the static members of <see cref="System.IO.Path"/>.
        /// </summary>
        public IPath Path => _path.Value;
    }
}
