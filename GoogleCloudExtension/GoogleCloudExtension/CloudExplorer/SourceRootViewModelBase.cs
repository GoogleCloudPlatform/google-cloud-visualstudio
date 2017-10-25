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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.Theming;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This class implements the basic behaviors for a cloud source's root view model. All source view models
    /// _should_ derive from this class but it is not mandatory. This class offers all place holder functionality,
    /// common credentials and project check and setting the right state depending on the results from the 
    /// underlying data source.
    /// </summary>
    public abstract class SourceRootViewModelBase : TreeHierarchy, ISourceRootViewModelBase
    {
        private static readonly TreeLeaf s_noCredentialsPlacehodler =
            new TreeLeaf
            {
                IsError = true,
                Caption = Resources.CloudExplorerNoLoggedInMessage,
            };
        private static readonly TreeLeaf s_noProjectPlaceholder =
            new TreeLeaf
            {
                IsError = true,
                Caption = Resources.CloudExplorerNoProjectSelectedMessage,
            };

        /// <summary>
        /// Returns whether this view model is busy loading data.
        /// </summary>
        private bool _isLoadingState;

        /// <summary>
        /// Returns whether this view model is already loaded with data.
        /// </summary>
        private bool _isLoadedState;

        /// <summary>
        /// Returns the icon to use for the root for this data source. By default all sources use the
        /// default <seealso cref="CommonImageResources.s_logoIcon"/>. 
        /// Each node can override the icon with a custom one if desired.
        /// </summary>
        public virtual ImageSource RootIcon => CommonImageResources.CloudLogo16By16;

        /// <summary>
        /// Returns the caption to use for the root node for this data source.
        /// </summary>
        public abstract string RootCaption { get; }

        /// <summary>
        /// Returns the tree node to use when there's an error loading data.
        /// </summary>
        public abstract TreeLeaf ErrorPlaceholder { get; }

        /// <summary>
        /// Returns the tree node to use when there's no data returned by this data source.
        /// </summary>
        public abstract TreeLeaf NoItemsPlaceholder { get; }

        /// <summary>
        /// Returns the tree node to use while loading data.
        /// </summary>
        public abstract TreeLeaf LoadingPlaceholder { get; }

        /// <summary>
        /// Returns the tree node to use when we detect that the necessary APIs are not enabled.
        /// </summary>
        public abstract TreeLeaf ApiNotEnabledPlaceholder { get; }

        /// <summary>
        /// Returns the names of the required APIs for the source.
        /// </summary>
        public abstract IList<string> RequiredApis { get; }

        /// <summary>
        /// Returns the context in which this source root view model is working.
        /// </summary>
        public ICloudSourceContext Context { get; private set; }

        /// <summary>
        /// The task that tracks the loading from the data source.
        /// </summary>
        internal Task LoadingTask { get; private set; } = Task.FromResult(false);

        public virtual void Initialize(ICloudSourceContext context)
        {
            Context = context;
            Icon = RootIcon;
            Caption = RootCaption;

            Children.Add(LoadingPlaceholder);
        }

        public virtual void Refresh()
        {
            if (!_isLoadedState)
            {
                return;
            }

            _isLoadedState = false;
            LoadingTask = LoadDataWrapper();
        }

        public virtual void InvalidateProjectOrAccount()
        { }

        protected override void OnIsExpandedChanged(bool newValue)
        {
            if (_isLoadingState)
            {
                return;
            }

            if (newValue && !_isLoadedState)
            {
                LoadingTask = LoadDataWrapper();
            }
        }

        /// <summary>
        /// Override this function to load and display the data in the control.
        /// </summary>
        protected abstract Task LoadDataOverride();

        private async Task LoadDataWrapper()
        {
            try
            {
                _isLoadingState = true;
                Children.Clear();

                if (CredentialsStore.Default.CurrentAccount == null)
                {
                    Children.Add(s_noCredentialsPlacehodler);
                    return;
                }

                if (CredentialsStore.Default.CurrentProjectId == null)
                {
                    Children.Add(s_noProjectPlaceholder);
                    return;
                }

                Children.Add(LoadingPlaceholder);

                if (!await ApiManager.Default.AreServicesEnabledAsync(RequiredApis))
                {
                    Children.Clear();
                    Children.Add(ApiNotEnabledPlaceholder);
                    return;
                }

                await LoadDataOverride();
                if (Children.Count == 0)
                {
                    Children.Add(NoItemsPlaceholder);
                }
            }
            catch (CloudExplorerSourceException)
            {
                Children.Clear();
                Children.Add(ErrorPlaceholder);
            }
            finally
            {
                _isLoadingState = false;
                _isLoadedState = true;
            }
        }
    }
}

