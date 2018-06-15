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

using System;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// Represents an MSBuild target command line argument.
    /// </summary>
    public class MSBuildTarget
    {
        /// <summary>
        /// The name of the target for MSBuild to run.
        /// </summary>
        public string Target { get; }

        /// <summary>
        /// Creates an MSBuild Target command line argument.
        /// </summary>
        /// <param name="target">The name of the MSBuild target to run.</param>
        public MSBuildTarget(string target)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        /// <summary>Returns the target argument.</summary>
        /// <returns>The target formatted as an argument to MSBuild.</returns>
        public override string ToString() => $"/t:{Target}";
    }
}