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
    public class ButtonDefinition : Model
    {
        private string _toolTip;
        private ImageSource _icon;
        private ICommand _command;
        private bool _isChecked;

        public string ToolTip
        {
            get { return _toolTip; }
            set { SetValueAndRaise(ref _toolTip, value); }
        }

        public ImageSource Icon
        {
            get { return _icon; }
            set { SetValueAndRaise(ref _icon, value); }
        }

        public ICommand Command
        {
            get { return _command; }
            set { SetValueAndRaise(ref _command, value); }
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetValueAndRaise(ref _isChecked, value); }
        }
    }
}
