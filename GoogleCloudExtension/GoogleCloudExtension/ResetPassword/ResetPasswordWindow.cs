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

using Google.Apis.Compute.v1.Data;
using Microsoft.VisualStudio.PlatformUI;

namespace GoogleCloudExtension.ResetPassword
{
    public class ResetPasswordWindow : DialogWindow
    {
        private ResetPasswordWindow(Instance instance, string projectId)
        {
            Title = $"Reset Password for {instance.Name}";
            Width = 350;
            Height = 160;
            ResizeMode = System.Windows.ResizeMode.NoResize;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            Content = new ResetPasswordWindowContent
            {
                DataContext = new ResetPasswordViewModel(this, instance, projectId)
            };
        }

        public static void PromptUser(Instance instance, string projectId)
        {
            var dialog = new ResetPasswordWindow(instance, projectId);
            dialog.ShowModal();
        }
    }
}
