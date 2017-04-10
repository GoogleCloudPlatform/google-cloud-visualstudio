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

using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;
using System.Linq;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// Data object that backs the new topic window. Contains necessicary data for creating a new topic.
    /// </summary>
    public class NewTopicViewModel : ValidatingViewModelBase
    {
        private readonly CommonDialogWindowBase _owner;
        private string _topicName;

        /// <summary>
        /// The id of the project that will own the new topic.
        /// </summary>
        public string Project { get; }

        /// <summary>
        /// The name of the new topic.
        /// </summary>
        public string TopicName
        {
            get { return _topicName; }
            set
            {
                SetValueAndRaise(ref _topicName, value);
                SetValidationResults(PubSubNameValidationRule.Validate(value));
            }
        }

        /// <summary>
        /// The name of the new topic. Null until CreateCommand is run.
        /// </summary>
        public string Result { get; private set; }

        /// <summary>
        /// The command to run to create the new topic.
        /// </summary>
        public ProtectedCommand CreateCommand { get; }

        public NewTopicViewModel(string project, CommonDialogWindowBase owner)
        {
            _owner = owner;
            Project = project;
            CreateCommand = new ProtectedCommand(OnCreateCommand);
        }

        /// <summary>
        /// The execution of the create command.
        /// </summary>
        private void OnCreateCommand()
        {
            if (ValidateInput())
            {
                Result = TopicName;
                _owner.Close();
            }
        }

        /// <summary>
        /// Validates the input. Prompts the user on validation errors.
        /// </summary>
        /// <returns>Returns true if validation succeded, false if the user had to be prompted</returns>
        private bool ValidateInput()
        {
            var results = PubSubNameValidationRule.Validate(TopicName);
            var details = string.Join("\n", results.Select(result => result.Message));
            if (!string.IsNullOrEmpty(details))
            {
                string message = string.Format(Resources.PubSubNewTopicNameInvalidMessage, TopicName);
                UserPromptUtils.ErrorPrompt(message, Resources.PubSubNewTopicNameInvalidTitle, details);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}