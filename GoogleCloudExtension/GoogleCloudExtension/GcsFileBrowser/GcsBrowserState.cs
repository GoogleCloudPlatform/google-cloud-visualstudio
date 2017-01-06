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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class PathStep
    {
        public string Name { get; }

        public string Path { get; }

        public PathStep(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }

    public class GcsBrowserState
    {
        public IList<GcsRow> Items { get; }

        public IEnumerable<PathStep> PathSteps { get; }

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
                var stepNames = GcsDataSourceExtensions.ParsePath(name);
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
