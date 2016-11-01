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
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.UserPrompt
{
    public class UserPromptWindowViewModel : ViewModelBase
    {
        private readonly UserPromptWindow _owner;
        private readonly UserPromptWindow.Options _options;

        public string Prompt => _options.Prompt;

        public string Message => _options.Message;

        public ImageSource Icon => _options.Icon;

        public ICommand ActionCommand { get; }

        public string ActionButtonCaption => _options.ActionButtonCaption;

        public bool HasActionButton => !String.IsNullOrEmpty(_options.ActionButtonCaption);

        public bool DoesNotHaveActionButton => !HasActionButton;

        public string CancelButtonCaption => _options.CancelButtonCaption;

        public bool Result { get; private set; }

        public UserPromptWindowViewModel(UserPromptWindow owner, UserPromptWindow.Options options)
        {
            _owner = owner;
            _options = options;

            ActionCommand = new ProtectedCommand(OnActionCommand);
        }

        private void OnActionCommand()
        {
            Result = true;
            _owner.Close();
        }
    }
}
