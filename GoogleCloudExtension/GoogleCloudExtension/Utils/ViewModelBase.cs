// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.


namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class is to be used as the base class for all view models for the extension.
    /// Provies useful properties common to almost all view models (such as a loading state) as well
    /// as whether gcloud is installed or not.
    /// </summary>
    public class ViewModelBase : Model
    {
        private bool _loading;
        private string _loadingMessage;

        public bool Loading
        {
            get { return _loading; }
            set { SetValueAndRaise(ref _loading, value); }
        }

        public string LoadingMessage
        {
            get { return _loadingMessage; }
            set { SetValueAndRaise(ref _loadingMessage, value); }
        }
    }
}
