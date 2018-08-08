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

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.CloudExplorerSources.CloudConsoleLinks
{
    public class ConsoleLinkGroup : TreeHierarchy
    {
        public ConsoleLinkGroup(string caption, ICloudSourceContext context, IEnumerable<LinkInfo> groupLinks) : base(
            groupLinks.Select(l => new ConsoleLink(l, context)))
        {
            Caption = caption;
        }
    }
}