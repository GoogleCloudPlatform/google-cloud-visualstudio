// Copyright 2017 Google Inc. All Rights Reserved.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    public static class GcsPathUtils
    {
        /// <summary>
        /// Returns the leaf on a GCS path.
        /// </summary>
        /// <param name="name">The path within a bucket.</param>
        public static string GetFileName(string name)
        {
            var cleanName = name.Last() == '/' ? name.Substring(0, name.Length - 1) : name;
            return cleanName.Split('/').Last();
        }

        /// <summary>
        /// Safely append <paramref name="child"/> to <paramref name="baseDir"/> adding the path separator
        /// if needed.
        /// </summary>
        /// <param name="baseDir">The base directory for the new path.</param>
        /// <param name="child">The child path.</param>
        public static string Combine(string baseDir, string child)
        {
            StringBuilder result = new StringBuilder(baseDir);
            if (!String.IsNullOrEmpty(baseDir) && baseDir.Last() != '/')
            {
                result.Append('/');
            }
            result.Append(child);
            return result.ToString();
        }
    }
}
