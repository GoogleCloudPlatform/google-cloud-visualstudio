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

using System.Windows;
using System.Windows.Controls;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.AddWindowsCredential
{
    /// <summary>
    /// Interaction logic for ResetPasswordWindowContent.xaml
    /// </summary>
    public partial class AddWindowsCredentialWindowContent : UserControl
    {
        private AddWindowsCredentialViewModel ViewModel => (AddWindowsCredentialViewModel)DataContext;

        public AddWindowsCredentialWindowContent()
        {
            InitializeComponent();

            // Ensure focus is on the textbox.
            _userName.Focus();

            // Listen for changes in the password. This is needed because the Password property is not
            // a dependency property and thus it cannot be bound to.
            _password.PasswordChanged += OnPasswordChanged;
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                ViewModel.Password = _password.Password;
            });
        }
    }
}
