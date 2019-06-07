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
using System.Windows.Controls;
using System.Windows.Media;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// A node in the UI tree for the cloud explorer.
    /// </summary>
    public class TreeNode : Model, ITreeNode
    {
        private const string ErrorIconPath = "CloudExplorer/Resources/error_icon.png";
        private const string WarningIconPath = "CloudExplorer/Resources/warning_icon.png";

        private static readonly Lazy<ImageSource> s_errorIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(ErrorIconPath));
        private static readonly Lazy<ImageSource> s_warningIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(WarningIconPath));

        private string _caption;
        private ContextMenu _contextMenu;
        private ImageSource _icon;
        private bool _isLoading;
        private bool _isError;
        private bool _isWarning;

        /// <summary>
        /// The icon to use when a node is in the error state.
        /// </summary>
        public static ImageSource ErrorIcon => s_errorIcon.Value;

        /// <summary>
        /// The icon to use when a nide is in the warning state.
        /// </summary>
        public static ImageSource WarningIcon => s_warningIcon.Value;

        /// <summary>
        /// Whether this node is in the loading state.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                SetValueAndRaise(ref _isLoading, value);
                RaisePropertyChanged(nameof(IconIsVisible));
            }
        }

        /// <summary>
        /// Whether this node is in the error state.
        /// </summary>
        public bool IsError
        {
            get { return _isError; }
            set
            {
                SetValueAndRaise(ref _isError, value);
                RaisePropertyChanged(nameof(IconIsVisible));
            }
        }

        /// <summary>
        /// Whether this node is in the warning state.
        /// </summary>
        public bool IsWarning
        {
            get { return _isWarning; }
            set
            {
                SetValueAndRaise(ref _isWarning, value);
                RaisePropertyChanged(nameof(IconIsVisible));
            }
        }

        /// <summary>
        /// Whether the custom node icon is to be used or not, only in normal mode.
        /// </summary>
        public bool IconIsVisible => !IsError && !IsLoading && !IsWarning;

        /// <summary>
        /// The icon to use in the UI for this item.
        /// </summary>
        public ImageSource Icon
        {
            get { return _icon; }
            set { SetValueAndRaise(ref _icon, value); }
        }

        /// <summary>
        /// The content to display for this item.
        /// </summary>
        public string Caption
        {
            get { return _caption; }
            set { SetValueAndRaise(ref _caption, value); }
        }

        /// <summary>
        /// The context menu for this item.
        /// </summary>
        public ContextMenu ContextMenu
        {
            get { return _contextMenu; }
            set { SetValueAndRaise(ref _contextMenu, value); }
        }

        /// <summary>
        /// Some context menu item needs to be enabled/disabled dynamically.
        /// Override this method to update menu item state.
        /// </summary>
        public virtual void OnMenuItemOpen()
        { }

        /// <summary>
        /// Disables all items in the context menu if the node is in the IsError or IsLoading state. It is expected
        /// that after the state changes the node will be reloaded and the context menu replaced.
        /// </summary>
        protected void SyncContextMenuState()
        {
            if (IsError || IsLoading)
            {
                foreach (var item in ContextMenu.ItemsSource)
                {
                    var menu = item as MenuItem;
                    if (menu != null)
                    {
                        if (menu.Command is ProtectedCommand)
                        {
                            var cmd = (ProtectedCommand)menu.Command;
                            cmd.CanExecuteCommand = false;
                        }
                    }
                }
            }
        }
    }
}
