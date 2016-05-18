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
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This class represents a button in the action bar in the Cloud Explorer UI.
    /// </summary>
    public class ButtonDefinition : Model
    {
        private string _toolTip;
        private ImageSource _icon;
        private ICommand _command;
        private bool _isChecked;

        /// <summary>
        /// The text for the tooltip for the button in the command bar.
        /// </summary>
        public string ToolTip
        {
            get { return _toolTip; }
            set { SetValueAndRaise(ref _toolTip, value); }
        }

        /// <summary>
        /// The icon for the button.
        /// </summary>
        public ImageSource Icon
        {
            get { return _icon; }
            set { SetValueAndRaise(ref _icon, value); }
        }

        /// <summary>
        /// The command to execute when the button is pressed.
        /// </summary>
        public ICommand Command
        {
            get { return _command; }
            set { SetValueAndRaise(ref _command, value); }
        }

        /// <summary>
        /// Whether the button is in the checked state or not. This is independent on the pressed state;
        /// it applies to buttons that toggle state.
        /// </summary>
        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetValueAndRaise(ref _isChecked, value); }
        }
    }
}
