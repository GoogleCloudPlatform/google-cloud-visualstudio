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

using GoogleCloudExtension.Utils;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// A node in the UI tree for the cloud explorer.
    /// </summary>
    public class TreeNode : Model
    {
        private const string ErrorIconPath = "CloudExplorer/Resources/error_icon.png";
        private readonly static Lazy<ImageSource> s_ErrorIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(ErrorIconPath));

        private string _caption;
        private ContextMenu _contextMenu;
        private ImageSource _icon;
        private bool _isLoading;
        private bool _isError;

        /// <summary>
        /// The icon to use when a node is in the error state.
        /// </summary>
        public static ImageSource ErrorIcon => s_ErrorIcon.Value;

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
        /// Whether the custom node icon is to be used or not, only in normal mode.
        /// </summary>
        public bool IconIsVisible => !IsError && !IsLoading;

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
    }
}
