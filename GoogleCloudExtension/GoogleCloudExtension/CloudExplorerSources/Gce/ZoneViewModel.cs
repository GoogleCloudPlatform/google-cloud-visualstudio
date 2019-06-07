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
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class ZoneViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/Gce/Resources/zone_icon.png";

        private static readonly Lazy<ImageSource> s_zoneIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private readonly GceSourceRootViewModel _owner;
        private readonly Zone _zone;

        #region ICloudExplorerItemSource implementation

        event EventHandler ICloudExplorerItemSource.ItemChanged
        {
            add { }
            remove { }
        }

        object ICloudExplorerItemSource.Item => GetItem();

        #endregion

        internal ZoneViewModel(GceSourceRootViewModel owner, Zone zone, IReadOnlyCollection<GceInstanceViewModel> instances)
        {
            _owner = owner;
            _zone = zone;

            Caption = string.Format(Resources.CloudExplorerGceZoneCaption, zone.Name, instances.Count);
            Icon = s_zoneIcon.Value;

            foreach (GceInstanceViewModel instance in instances)
            {
                Children.Add(instance);
            }

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.CloudExplorerGceNewInstanceMenuHeader, Command = new ProtectedCommand(OnNewInstanceCommand) },
                new MenuItem { Header = Resources.UiPropertiesMenuHeader, Command = new ProtectedAsyncCommand(OnPropertiesCommandAsync) }
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private async Task OnPropertiesCommandAsync()
        {
            await _owner.Context.ShowPropertiesWindowAsync(GetItem());
        }

        private void OnNewInstanceCommand()
        {
            var url = $"https://console.cloud.google.com/compute/instancesAdd?project={_owner.Context.CurrentProject.ProjectId}&zone={_zone.Name}";
            Process.Start(url);
        }

        private object GetItem() => new ZoneItem(_zone);
    }
}