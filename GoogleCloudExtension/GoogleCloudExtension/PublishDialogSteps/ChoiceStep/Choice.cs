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

namespace GoogleCloudExtension.PublishDialogSteps.ChoiceStep
{
    /// <summary>
    /// This class contains all of the data for a choice of target in the <seealso cref="ChoiceStepViewModel"/> step.
    /// </summary>
    public class Choice : Model
    {
        /// <summary>
        /// Returns the name of the choice, App Engine, GCE, etc...
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The tooltip string for the choice button.
        /// </summary>
        public string ToolTip { get; set; }

        /// <summary>
        /// The command to execute when the user presses on the choice.
        /// </summary>
        public ICommand Command { get; set; }

        /// <summary>
        /// The icon to show for the choice.
        /// </summary>
        public ImageSource Icon { get; set; }
    }
}
