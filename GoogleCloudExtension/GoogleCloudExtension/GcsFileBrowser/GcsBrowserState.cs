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
using System.Diagnostics;
using System.Linq;

namespace GoogleCloudExtension.GcsFileBrowser
{
    /// <summary>
    /// This class represents a single step in a path wihin a bucket.
    /// </summary>
    public class PathStep
    {
        /// <summary>
        /// The name of the step, a single token in the path within a bucket.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The full path to this step, containing previous parts of the path within the bucket.
        /// </summary>
        public string Path { get; }

        public PathStep(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }

    /// <summary>
    /// This class contains the browser state at a given step in the navigation path.
    /// </summary>
    public class GcsBrowserState
    {
        /// <summary>
        /// The list of individual items (or blobs) at this part on the path.
        /// </summary>
        public IList<GcsRow> Items { get; }

        /// <summary>
        /// The pathsteps to get this state.
        /// </summary>
        public IEnumerable<PathStep> PathSteps { get; }

        /// <summary>
        /// The current path within the bucket.
        /// </summary>
        public string CurrentPath
        {
            get
            {
                var path = String.Join("/", PathSteps.Select(x => x.Name));
                if (String.IsNullOrEmpty(path))
                {
                    return path;
                }
                return path + "/";
            }
        }

        public GcsBrowserState(IEnumerable<GcsRow> items, string name)
        {
            Items = items.ToList();

            if (String.IsNullOrEmpty(name))
            {
                PathSteps = Enumerable.Empty<PathStep>();
            }
            else
            {
                Debug.Assert(name.Last() == '/');
                var stepNames = GcsDataSourceExtensions.ParseDiretoryPath(name);
                var steps = new List<PathStep>();
                string currentPath = "";
                foreach (var stepName in stepNames)
                {
                    currentPath = currentPath + $"{stepName}/";
                    steps.Add(new PathStep(name: stepName, path: currentPath));
                }
                PathSteps = steps;
            }
        }
    }
}
