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
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Cloud explorer node for the container for orphaned subscriptions.
    /// </summary>
    internal class OrphanedSubscriptionsViewModel : TopicViewModelBase
    {
        private const string IconResourcePath = "CloudExplorerSources/PubSub/Resources/orphaned_subscriptions_icon.png";

        private static readonly Lazy<ImageSource> s_orphanedSubscriptionIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        public OrphanedSubscriptionsViewModel(PubsubSourceRootViewModel owner, IEnumerable<Subscription> subscriptions)
            : base(owner, new OrphanedSubscriptionsItem(), subscriptions)
        {
            Icon = s_orphanedSubscriptionIcon.Value;

            // Include an invisible context menu so child context menus continue to show up.
            ContextMenu = new ContextMenu
            {
                Visibility = Visibility.Hidden
            };
        }
    }
}
