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

using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.CloudExplorerSources.PubSub;
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// Data objet that backs the new subscription window. It contains the information needed to create a new
    /// subscription.
    /// </summary>
    public class NewSubscriptionViewModel : ViewModelBase
    {
        private readonly CommonDialogWindowBase _owner;

        public string TopicName => PubsubSource.GetPathLeaf(Subscription.Topic);

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
                    RaisePropertyChanged();
                }
            }
        }

        public PushConfig PushConfig { get; }

        public Subscription Subscription { get; }

        public ProtectedCommand CreateCommand { get; }

        public Subscription Result { get; private set; }

        public NewSubscriptionViewModel(Subscription subscription, CommonDialogWindowBase owner)
        {
            _owner = owner;
            Subscription = subscription;
            CreateCommand = new ProtectedCommand(OnCreateCommand);
            PushConfig = subscription.PushConfig ?? new PushConfig();
        }

        private void OnCreateCommand()
        {
            Result = Subscription;
            _owner.Close();
        }
    }
}
