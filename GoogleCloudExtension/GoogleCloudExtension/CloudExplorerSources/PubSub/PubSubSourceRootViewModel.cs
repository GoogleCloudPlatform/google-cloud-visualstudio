// // Copyright 2016 Google Inc. All Rights Reserved.
// //
// // Licensed under the Apache License, Version 2.0 (the "License");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.

using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.CloudExplorer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudExplorerSources.PubSub
{
    internal class PubSubSourceRootViewModel : SourceRootViewModelBase
    {

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubLoadingCaption,
            IsLoading = true
        };
        private static readonly TreeLeaf s_noItemsPlacehoder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubNoTopicsFoundCaption,
            IsWarning = true
        };
        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = Resources.CloudExplorerPubSubListTopicsErrorCaption,
            IsError = true
        };

        public override string RootCaption { get; } = Resources.CloudExplorerPubSubRootCaption;
        public override TreeLeaf ErrorPlaceholder { get; } = s_errorPlaceholder;
        public override TreeLeaf NoItemsPlaceholder { get; } = s_noItemsPlacehoder;
        public override TreeLeaf LoadingPlaceholder { get; } = s_loadingPlaceholder;

        private Lazy<PubSubDataSource> _dataSource = new Lazy<PubSubDataSource>(CreateDataSource);

        private static PubSubDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId != null)
            {
                return new PubSubDataSource(
                    CredentialsStore.Default.CurrentProjectId,
                    CredentialsStore.Default.CurrentGoogleCredential,
                    GoogleCloudExtensionPackage.ApplicationName);
            }
            else
            {
                return null;
            }
        }

        public override void Initialize(ICloudSourceContext context)
        {
            base.Initialize(context);
            ContextMenu = new ContextMenu
            {
                ItemsSource = new[]
                {
                    new MenuItem
                    {
                        Header = Resources.CloudExplorerPubSubNewTopicMenuHeader
                    }
                }
            };
        }

        public override void InvalidateProjectOrAccount()
        {
            Debug.WriteLine("New credentials, invalidating the GCS source.");
            _dataSource = new Lazy<PubSubDataSource>(CreateDataSource);
        }

        protected override async Task LoadDataOverride()
        {
            Children.Clear();
            Children.Add(LoadingPlaceholder);
            try
            {
                IEnumerable<PubSubTopicViewModel> topicModels = await ListTopicViewModelsAsync();
                Children.Clear();
                foreach (var topicModel in topicModels)
                {
                    Children.Add(topicModel);
                }
                if (Children.Count == 0)
                {
                    Children.Add(NoItemsPlaceholder);
                }
            }
            catch
            {
                Children.Clear();
                Children.Add(ErrorPlaceholder);
                throw;
            }
        }

        private async Task<IEnumerable<PubSubTopicViewModel>> ListTopicViewModelsAsync()
        {
            IEnumerable<Topic> topics = await _dataSource.Value.GetTopicListAsync();
            return topics.Select(topic => new PubSubTopicViewModel(this, topic));
        }
    }
}