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