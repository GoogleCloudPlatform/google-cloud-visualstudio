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

using GoogleCloudExtension.Utils.Async;
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorer.SampleData
{
    /// <summary>
    /// Mocked version of <seealso cref="CloudExplorerViewModel"/> that has same sample data to show.
    /// Note: the pattern ("string") is used in this file to make sure that these strings don't trigger the
    /// detector of strings in need of localization.
    /// </summary>
    public class WithDataState : SampleDataBase
    {
        public bool LoadingProject { get; } = false;

        public AsyncProperty<string> ProfileNameAsync { get; } = new AsyncProperty<string>("User Name");

        public string ProjectDisplayString { get; } = ("Project-Id");

        public IList<TreeHierarchy> Roots { get; } = new List<TreeHierarchy>
        {
            new TreeHierarchy(new List<TreeNode>
            {
                new TreeLeaf { Caption = ("Node 1"), Icon = s_instanceIcon.Value },
                new TreeLeaf { Caption = ("Node 2"), Icon = s_instanceIcon.Value }
            })
            {
                Caption = ("Container 1"),
                Icon = s_containerIcon.Value,
                IsExpanded = true,
            },
            new TreeHierarchy(new List<TreeNode>
            {
                new TreeLeaf { Caption = ("Node1"), Icon = s_instanceIcon.Value }
            })
            {
                Caption = ("Container 2"),
                Icon = s_containerIcon.Value,
            },
            new TreeHierarchy(new List<TreeNode>
            {
                new TreeLeaf { Caption = ("Warning"), IsWarning = true },
                new TreeLeaf { Caption = ("Error"), IsError = true },
                new TreeLeaf { Caption = ("Loading"), IsLoading = true }

            })
            {
                Caption = ("Variants"),
                Icon = s_containerIcon.Value,
                IsExpanded = true
            }
        };
    }
}
