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

using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.CloudExplorerSources.PubSub;
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System.Linq;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// Data objet that backs the new subscription window. It contains the information needed to create a new
    /// subscription.
    /// </summary>
    public class NewSubscriptionViewModel : ValidatingViewModelBase
    {
        private readonly CommonDialogWindowBase _owner;
        private static readonly string s_unlabeledSubscriptionName = Resources.NewSubscriptionWindowNameLabel.Unlabel();

        /// <summary>
        /// The name of the topic the subscription belongs to.
        /// </summary>
        public string TopicName => PubsubDataSource.GetPathLeaf(Subscription.Topic);

        /// <summary>
        /// If PubSub should send a push notification rather than waiting for a pull.
        /// </summary>
        public bool Push
        {
            get { return Subscription.PushConfig == PushConfig; }
            set
            {
                if (value != Push)
                {
                    Subscription.PushConfig = value ? PushConfig : null;
                    RaisePropertyChanged(nameof(Push));
                }
            }
        }

        /// <summary>
        /// The PushConfig object that will be part of the subscription if the
        /// push option is selected. The object always exists so we don't lose
        /// data if the user temporarily selects the pull option.
        /// </summary>
        public PushConfig PushConfig { get; }

        /// <summary>
        /// The Subscription object this view model models.
        /// </summary>
        public Subscription Subscription { get; }

        public string SubscriptionName
        {
            get { return Subscription.Name; }
            set
            {
                Subscription.Name = value;
                RaisePropertyChanged(nameof(SubscriptionName));
                SetValidationResults(PubSubNameValidationRule.Validate(value, s_unlabeledSubscriptionName));
            }
        }

        /// <summary>
        /// The command called to actually create the subscription.
        /// </summary>
        public ProtectedCommand CreateCommand { get; }

        /// <summary>
        /// The modeled subscription. Null until the create command is run.
        /// </summary>
        public Subscription Result { get; private set; }

        /// <summary>
        /// Creates a new NewSubscriptionViewModel
        /// </summary>
        /// <param name="subscription">The subscription object to model</param>
        /// <param name="owner">The owner dialog.</param>
        public NewSubscriptionViewModel(Subscription subscription, CommonDialogWindowBase owner)
        {
            _owner = owner;
            Subscription = subscription;
            CreateCommand = new ProtectedCommand(OnCreateCommand);
            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(HasErrors))
                {
                    CreateCommand.CanExecuteCommand = !HasErrors;
                }
            };
            PushConfig = subscription.PushConfig ?? new PushConfig();
        }

        /// <summary>
        /// The execution of the CreateCommand.
        /// </summary>
        private void OnCreateCommand()
        {
            if (ValidateInput())
            {
                Result = Subscription;
                _owner.Close();
            }
        }

        /// <summary>
        /// Validates the model input data. Prompts the user on validation errors.
        /// </summary>
        /// <returns>True if the data is valid, false if the user was prompted about invalid data.</returns>
        private bool ValidateInput()
        {
            var results = PubSubNameValidationRule.Validate(Subscription.Name, s_unlabeledSubscriptionName);
            var details = string.Join("\n", results.Select(result => result.Message));
            if (!string.IsNullOrEmpty(details))
            {
                string message = string.Format(Resources.PubSubNewSubscriptionNameInvalidMessage, Subscription.Name);
                UserPromptUtils.ErrorPrompt(message, Resources.PubSubNewSubscriptionNameInvalidTitle, details);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
