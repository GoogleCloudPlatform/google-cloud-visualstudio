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

using System.Windows.Input;
using System.Windows.Media;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.PublishDialog.Steps.Choice
{
    /// <summary>
    /// The enum of IDs for the choices.
    /// </summary>
    public enum ChoiceType
    {
        None = 0,
        Gce,
        Gae,
        Gke
    }

    /// <summary>
    /// This class contains all of the data for a choice of target in the <seealso cref="ChoiceStepViewModel"/> step.
    /// </summary>
    public class Choice : Model
    {
        /// <summary>
        /// Gets the ID string of the choice. i.e. Gae, Gce, Gke.
        /// </summary>
        public ChoiceType Id { get; }

        /// <summary>
        /// Returns the name of the choice, App Engine, GCE, etc...
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The tooltip string for the choice button.
        /// </summary>
        public string ToolTip { get; }

        /// <summary>
        /// The icon to show for the choice.
        /// </summary>
        public ImageSource Icon { get; }

        /// <summary>
        /// The command to execute when the user presses on the choice.
        /// </summary>
        public ICommand Command { get; }

        public Choice(ChoiceType id, string name, string toolTip, ImageSource icon, ICommand command)
        {
            Id = id;
            Name = name;
            ToolTip = toolTip;
            Icon = icon;
            Command = command;
        }
    }
}
