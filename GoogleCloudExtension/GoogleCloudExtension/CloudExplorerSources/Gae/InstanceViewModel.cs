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

using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    /// <summary>
    /// This class represents a view of a GAE instance in the Google Cloud Explorer Window.
    /// </summary>
    internal class InstanceViewModel : TreeHierarchy, ICloudExplorerItemSource
    {
        public const string RunningStatus = "RUNNING";
        public const string TerminatedStatus = "TERMINATED";

        private const string IconRunningResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_running.png";
        private const string IconStopedResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_stoped.png";
        private const string IconTransitionResourcePath = "CloudExplorerSources/Gae/Resources/instance_icon_transition.png";

        private static readonly Lazy<ImageSource> s_instanceRunningIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconRunningResourcePath));
        private static readonly Lazy<ImageSource> s_instanceStopedIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconStopedResourcePath));
        private static readonly Lazy<ImageSource> s_instanceTransitionIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconTransitionResourcePath));

        private readonly VersionViewModel _owner;

        public readonly GaeSourceRootViewModel root;

        private readonly Instance _instance;

        public event EventHandler ItemChanged;

        public object Item => GetItem();

        public InstanceViewModel(VersionViewModel owner, Instance instance)
        {
            _owner = owner;
            _instance = instance;
            root = _owner.root;

            Caption = _instance.Id;
            UpdateIcon();

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header = Resources.UiPropertiesMenuHeader, Command = new ProtectedCommand(OnPropertiesWindowCommand) },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private void OnPropertiesWindowCommand()
        {
            root.Context.ShowPropertiesWindow(Item);
        }

        private void UpdateIcon()
        {
            switch (_instance.VmStatus)
            {
                case RunningStatus:
                    Icon = s_instanceRunningIcon.Value;
                    break;
                case TerminatedStatus:
                    Icon = s_instanceStopedIcon.Value;
                    break;
                default:
                    Icon = s_instanceTransitionIcon.Value;
                    break;
            }
        }

        public InstanceItem GetItem() => new InstanceItem(_instance);
    }
}
