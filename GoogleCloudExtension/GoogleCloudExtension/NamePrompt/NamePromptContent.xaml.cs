﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.NamePrompt
{
    /// <summary>
    /// Interaction logic for NamePromptContent.xaml
    /// </summary>
    public partial class NamePromptContent : UserControl
    {
        public NamePromptContent()
        {
            InitializeComponent();

            _nameBox.Focus();

            Loaded += OnLoaded;
        }

        /// <summary>
        /// This method selects all the current text stored in the textbox. The Task.Yield method is
        /// used to ensure that the code runs on the **next** tick of the UI. This way the SelectAll() call
        /// will happen **after** the bindings are evaluated.
        /// </summary>
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await Task.Yield();
            _nameBox.SelectAll();
        }
    }
}
