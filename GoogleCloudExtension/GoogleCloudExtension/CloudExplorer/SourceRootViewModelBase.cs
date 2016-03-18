// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Utils;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    public abstract class SourceRootViewModelBase : TreeHierarchy
    {
        public bool IsLoadingState { get; private set; }

        public bool IsLoadedState { get; private set; }

        public abstract ImageSource RootIcon { get; }

        public abstract string RootCaption { get; }

        public abstract TreeLeaf ErrorPlaceholder { get; }

        public abstract TreeLeaf NoItemsPlaceholder { get; }

        public abstract TreeLeaf LoadingPlaceholder { get; }

        public void Initialize()
        {
            Icon = RootIcon;
            Content = RootCaption;

            Children.Add(LoadingPlaceholder);
        }

        public async void Refresh()
        {
            if (!IsLoadedState)
            {
                return;
            }

            IsLoadedState = false;
            await LoadDataWrapper();
        }

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
                Children.Add(LoadingPlaceholder);

                var gcloudValidationResult = await EnvironmentUtils.ValidateGCloudInstallation();
                if (!gcloudValidationResult.IsValidGCloudInstallation())
                {
                    Children.Clear();
                    Children.Add(GetErrorItem(gcloudValidationResult));
                    return;
                }

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

        private static TreeLeaf GetErrorItem(GCloudValidationResult gcloudValidationResult)
        {
            return new TreeLeaf
            {
                IsError = true,
                Content = gcloudValidationResult.GetDisplayString(),
            };
        }
    }
}

