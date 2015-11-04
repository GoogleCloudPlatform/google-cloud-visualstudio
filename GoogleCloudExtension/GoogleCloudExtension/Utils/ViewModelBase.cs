// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;

namespace GoogleCloudExtension.Utils
{
    public class ViewModelBase : Model
    {
        public bool IsGCloudInstalled
        {
            get { return GCloudWrapper.DefaultInstance.ValidateGCloudInstallation(); }
        }

        public bool IsGCloudNotInstalled
        {
            get { return !this.IsGCloudInstalled; }
        }

        private bool _Loading;
        public bool Loading
        {
            get { return _Loading; }
            set { SetValueAndRaise(ref _Loading, value); }
        }

        private string _LoadingMessage;
        public string LoadingMessage
        {
            get { return _LoadingMessage; }
            set { SetValueAndRaise(ref _LoadingMessage, value); }
        }
    }
}
