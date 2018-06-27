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

namespace GoogleCloudExtension.Services
{
    /// <summary>
    /// An <see cref="IEnvironment"/> implementation that delegates to <see cref="Environment"/>.
    /// </summary>
    [Export(typeof(IEnvironment))]
    public class EnvironmentService : IEnvironment
    {
        /// <inheritdoc cref="Environment.ExpandEnvironmentVariables(string)"/>
        public string ExpandEnvironmentVariables(string name) => Environment.ExpandEnvironmentVariables(name);

        /// <inheritdoc cref="Environment.GetFolderPath(Environment.SpecialFolder)"/>
        public string GetFolderPath(Environment.SpecialFolder folder) => Environment.GetFolderPath(folder);
    }
}