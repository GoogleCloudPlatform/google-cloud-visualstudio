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

namespace GoogleCloudExtension.ApiManagement
{
    /// <summary>
    /// This class contains the well known API names of the APIs used by the extension.
    /// </summary>
    public static class KnownApis
    {
        /// <summary>
        /// The API necessary to use App Engine services.
        /// </summary>
        public const string AppEngineAdminApiName = "appengine.googleapis.com";

        /// <summary>
        /// The API necessary to use GCE.
        /// </summary>
        public const string ComputeEngineApiName = "compute.googleapis.com";

        /// <summary>
        /// The API necessary to use GCS.
        /// </summary>
        public const string CloudStorageApiName = "storage-api.googleapis.com";

        /// <summary>
        /// The API necessary to use GKS.
        /// </summary>
        public const string ContainerEngineApiName = "container.googleapis.com";

        /// <summary>
        /// The API necessary to use the Cloud Builder.
        /// </summary>
        public const string CloudBuildApiName = "cloudbuild.googleapis.com";

        /// <summary>
        /// The API necessary to manage Cloud SQL instances.
        /// </summary>
        public const string CloudSQLApiName = "sqladmin.googleapis.com";

        /// <summary>
        /// The API necessary to manage Pub/Sub subscriptions.
        /// </summary>
        public const string PubSubApiName = "pubsub.googleapis.com";

        /// <summary>
        /// The API necessary to clone/create Google Cloud Source Repositories.
        /// </summary>
        public const string CloudSourceRepositoryApiName = "sourcerepo.googleapis.com";
    }
}
