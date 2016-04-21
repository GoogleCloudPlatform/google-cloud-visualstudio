// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Accounts;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    public abstract class SourceRootViewModelBase : TreeHierarchy
    {
        private static TreeLeaf s_noCredentialsPlacehodler =
            new TreeLeaf
            {
                IsError = true,
                Content = "No credentials, please login.",
            };
        private static TreeLeaf s_noProjectPlaceholder =
            new TreeLeaf
            {
                IsError = true,
                Content = "No project selected.",
            };

        public bool IsLoadingState { get; private set; }

        public bool IsLoadedState { get; private set; }

        public abstract ImageSource RootIcon { get; }

        public abstract string RootCaption { get; }

        public abstract TreeLeaf ErrorPlaceholder { get; }

        public abstract TreeLeaf NoItemsPlaceholder { get; }

        public abstract TreeLeaf LoadingPlaceholder { get; }

        public ICloudExplorerSource Owner { get; private set; }

        public virtual void Initialize(ICloudExplorerSource owner)
        {
            Icon = RootIcon;
            Content = RootCaption;
            Owner = owner;

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

        public virtual void InvalidateCredentials()
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

                if (AccountsManager.CurrentAccount == null)
                {
                    Children.Add(s_noCredentialsPlacehodler);
                    return;
                }

                if (Owner.CurrentProject == null)
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

