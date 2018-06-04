﻿// Copyright 2018 Google Inc. All Rights Reserved.
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
        private readonly Lazy<IFile> _fileLazy;
        private readonly Lazy<IXDocument> _xDocumentLazy;

        [ImportingConstructor]
        public FileSystemService(Lazy<IFile> fileLazy, Lazy<IXDocument> xDocumentLazy)
        {
            _fileLazy = fileLazy;
            _xDocumentLazy = xDocumentLazy;
        }

        /// <summary>
        /// Subservice for file operations. Matches the static methods of <see cref="System.IO.File"/>.
        /// </summary>
        public IFile File => _fileLazy.Value;

        /// <summary>
        /// XDocument load operations. Matches the static methods of <see cref="System.Xml.Linq.XDocument"/>.
        /// </summary>
        public IXDocument XDocument => _xDocumentLazy.Value;
    }
}
