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

using GoogleCloudExtension.Theming;
using System.Windows.Input;

namespace GoogleCloudExtension.ManageAccounts
{
    /// <summary>
    /// Interaction logic for ManageCredentialsWindowContent.xaml
    /// </summary>
    public partial class ManageAccountsWindowContent : CommonWindowContent<ManageAccountsViewModel>
    {
        public ManageAccountsWindowContent() : base(
            new ManageAccountsViewModel(),
            GoogleCloudExtension.Resources.ManageAccountsWindowTitle)
        {
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_accountsListBox.SelectedItem != null)
            {
                ViewModel.DoubleClickedItem((UserAccountViewModel)_accountsListBox.SelectedItem);
            }
        }
    }
}
