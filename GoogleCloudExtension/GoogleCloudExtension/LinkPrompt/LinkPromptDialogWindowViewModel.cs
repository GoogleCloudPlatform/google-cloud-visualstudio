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

using GoogleCloudExtension.Utils;
using System.Diagnostics;
using System.Windows.Input;

namespace GoogleCloudExtension.LinkPrompt
{
    public class LinkPromptDialogWindowViewModel : ViewModelBase
    {
        private readonly LinkInfo _link;

        public string Text { get; }

        public string LinkCaption => _link.Caption;

        public ICommand NavigateCommand { get; }

        public LinkPromptDialogWindowViewModel(string text, LinkInfo link)
        {
            Text = text;
            NavigateCommand = new ProtectedCommand(OnNavigateCommand);

            _link = link;
        }

        private void OnNavigateCommand()
        {
            Process.Start(_link.NavigateUrl);
        }
    }
}
