﻿// Copyright 2016 Google Inc. All Rights Reserved.
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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudExplorer
{
    internal class DisabledApiWarning : TreeLeaf
    {
        private readonly string _apiName;
        private readonly Project _project;

        public DisabledApiWarning(string apiName, string caption, Project project)
        {
            _apiName = apiName;
            _project = project;

            IsWarning = true;
            Caption = caption;

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.CloudExploreDisabledApiEnableApiMenuHeader, Command=new ProtectedCommand(OnEnableApiCommand) },
            };

            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void OnEnableApiCommand()
        {
            var url = $"https://console.developers.google.com/apis/api/{_apiName}/overview?project={_project.ProjectNumber}";
            Process.Start(url);
        }
    }
}
