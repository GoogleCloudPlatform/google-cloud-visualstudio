// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;

namespace GoogleCloudExtension.GCloud
{
    [Serializable]
    public sealed class GCloudException : Exception
    {
        public GCloudException()
        {
        }

        public GCloudException(string message) :
            base(message + "\nPlease ensure that the app, preview and alpha components are installed in gcloud run the command:\n" +
                    "\"gcloud components update alpha app preview\" from an administrator command line window to setup those components.\n" +
                    "Also ensure that you have gone through the initial setup with \"gcloud init\".")

        {
        }

        public GCloudException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}