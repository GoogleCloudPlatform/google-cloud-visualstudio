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
    public static class KnownApis
    {
        // The API necessary to use App Engine services.
        public const string AppEngineAdminApiName = "appengine.googleapis.com";

        // The API necessary to use GCE.
        public const string ComputeEngineApiName = "compute.googleapis.com";

        // The API necessary to use GCS.
        public const string CloudStorageApiName = "storage-api.googleapis.com";

        // The API necessary to use GKS.
        public const string ContainerEngineApiName = "container.googleapis.com";

        // The API necessary to use the Cloud Builder.
        public const string CloudBuildApiName = "cloudbuild.googleapis.com";

        // The API necessary to manage Cloud SQL instances.
        public const string CloudSQLApiName = "sqladmin.googleapis.com";

        // The API necessary to manage Pub/Sub subscriptions.
        public const string PubSubApiName = "pubsub.googleapis.com";
    }
}
