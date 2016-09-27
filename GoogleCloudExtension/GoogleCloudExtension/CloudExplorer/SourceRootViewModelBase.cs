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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Utils;
using System;
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
    public abstract class SourceRootViewModelBase : TreeHierarchy
    {
        private const string NodeIconPath = "CloudExplorer/Resources/logo_cloud.png";

        private static readonly TreeLeaf s_noCredentialsPlacehodler =
            new TreeLeaf
            {
                IsError = true,
                Caption = "No credentials, please login.",
            };
        private static readonly TreeLeaf s_noProjectPlaceholder =
            new TreeLeaf
            {
                IsError = true,
                Caption = "No project selected.",
            };
        private static readonly Lazy<ImageSource> s_nodeIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(NodeIconPath));

        /// <summary>
        /// Returns whether this view model is busy loading data.
        /// </summary>
        public bool IsLoadingState { get; private set; }

        /// <summary>
        /// Returns whether this view model is already loaded with data.
        /// </summary>
        public bool IsLoadedState { get; private set; }

        /// <summary>
        /// Returns the icon to use for the root for this data source. By default all sources use the
        /// default <seealso cref="s_nodeIcon"/>. Each node can override the icon with a custom one if desired.
        /// </summary>
        public virtual ImageSource RootIcon => s_nodeIcon.Value;

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
        /// Returns the context in which this source root view model is working.
        /// </summary>
        public ICloudSourceContext Context { get; private set; }

        public virtual void Initialize(ICloudSourceContext context)
        {
            Context = context;
            Icon = RootIcon;
            Caption = RootCaption;

            Children.Add(LoadingPlaceholder);
        }

        public virtual async void Refresh()
        {
            if (!IsLoadedState)
            {
                return;
            }

            IsLoadedState = false;
            await LoadDataWrapper();
        }

        public virtual void InvalidateProjectOrAccount()
        { }

        protected override async void OnIsExpandedChanged(bool newValue)
        {
            if (IsLoadingState)
            {
                return;
            }

            if (newValue && !IsLoadedState)
            {
                await LoadDataWrapper();
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
                IsLoadingState = true;
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

                await LoadDataOverride();
                if (Children.Count == 0)
                {
                    Children.Add(NoItemsPlaceholder);
                }
            }
            catch (CloudExplorerSourceException ex)
            {
                Children.Clear();
                Children.Add(ErrorPlaceholder);
            }
            finally
            {
                IsLoadingState = false;
                IsLoadedState = true;
            }
        }
    }
}

