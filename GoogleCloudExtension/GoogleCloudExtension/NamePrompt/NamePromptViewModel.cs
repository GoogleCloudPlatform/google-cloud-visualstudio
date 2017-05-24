// Copyright 2017 Google Inc. All Rights Reserved.
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
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.NamePrompt
{
    /// <summary>
    /// The view model for the name prompt window.
    /// </summary>
    public class NamePromptViewModel : ViewModelBase
    {
        private readonly NamePromptWindow _owner;
        private string _name;

        /// <summary>
        /// The name being entered.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { SetValueAndRaise(ref _name, value); }
        }

        /// <summary>
        /// The command for the ok button.
        /// </summary>
        public ICommand OkCommand { get; }

        /// <summary>
        /// The command for the cancel button.
        /// </summary>
        public ICommand CancelCommand { get; }

        public NamePromptViewModel(NamePromptWindow owner)
        {
            _owner = owner;

            OkCommand = new ProtectedCommand(OnOkCommand);
            CancelCommand = new ProtectedCommand(OnCancelCommand);
        }

        private void OnCancelCommand()
        {
            Name = null;
            _owner.Close();
        }

        private void OnOkCommand()
        {
            if (!Validate())
            {
                return;
            }

            _owner.Close();
        }

        private bool Validate()
        {
            if (String.IsNullOrEmpty(Name))
            {
                UserPromptUtils.ErrorPrompt(Resources.NamePromptEmptyNameMessage, Resources.UiErrorCaption);
                return false;
            }

            if (Name.Contains('/'))
            {
                UserPromptUtils.ErrorPrompt(Resources.NamePromptInvalidCharsMessage, Resources.UiErrorCaption);
                return false;
            }

            return true;
        }
    }
}
