// Copyright 2017 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// A leaf node that describes a Pubsub subscription.
    /// </summary>
    internal class SubscriptionViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/PubSub/Resources/subscription_icon.png";

        private static readonly Lazy<ImageSource> s_subscriptionIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private readonly ITopicViewModelBase _owner;
        private readonly SubscriptionItem _subscriptionItem;

        /// <summary>
        /// Returns the context in which this view model is working.
        /// </summary>
        private ICloudSourceContext Context => _owner.Context;

        /// <summary>
        /// The datasource for the item.
        /// </summary>
        private IPubsubDataSource DataSource => _owner.DataSource;

        #region ICloudExplorerItemSource implementation.

        /// <summary>
        /// The item this tree node represents.
        /// </summary>
        object ICloudExplorerItemSource.Item => _subscriptionItem;

        event EventHandler ICloudExplorerItemSource.ItemChanged
        {
            add { }
            remove { }
        }

        #endregion

        public SubscriptionViewModel(ITopicViewModelBase owner, Subscription subscription)
        {
            _owner = owner;
            _subscriptionItem = new SubscriptionItem(subscription);
            Caption = _subscriptionItem.Name;
            Icon = s_subscriptionIcon.Value;

            ContextMenu = new ContextMenu
            {
                ItemsSource = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Header = Resources.CloudExplorerPubSubDeleteSubscriptionMenuHeader,
                        Command = new ProtectedCommand(OnDeleteSubscriptionCommand)
                    },
                    new MenuItem
                    {
                        Header = Resources.UiPropertiesMenuHeader,
                        Command = new ProtectedCommand(OnPropertiesWindowCommand)
                    }
                }
            };
        }

        /// <summary>
        /// Prompts the user about deleting the subscription, then calls the datasource to delete it.
        /// </summary>
        internal async void OnDeleteSubscriptionCommand()
        {
            IsLoading = true;
            try
            {
                bool doDelete = UserPromptUtils.ActionPrompt(
                    string.Format(Resources.PubSubDeleteSubscriptionWindowMessage, _subscriptionItem.Name),
                    Resources.PubSubDeleteSubscriptionWindowHeader,
                    actionCaption: Resources.UiDeleteButtonCaption);
                if (doDelete)
                {
                    try
                    {
                        await DataSource.DeleteSubscriptionAsync(_subscriptionItem.FullName);
                        EventsReporterWrapper.ReportEvent(PubSubSubscriptionDeletedEvent.Create(CommandStatus.Success));
                    }
                    catch (DataSourceException e)
                    {
                        Debug.Write(e.Message, "Delete Subscription");
                        EventsReporterWrapper.ReportEvent(PubSubSubscriptionDeletedEvent.Create(CommandStatus.Failure));
                        UserPromptUtils.ErrorPrompt(
                            Resources.PubSubDeleteSubscriptionErrorMessage,
                            Resources.PubSubDeleteSubscriptionErrorHeader,
                            e.Message);
                    }
                    await _owner.Refresh();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Opens the properties window for the subscription item.
        /// </summary>
        internal void OnPropertiesWindowCommand()
        {
            Context.ShowPropertiesWindow(_subscriptionItem);
        }
    }
}
