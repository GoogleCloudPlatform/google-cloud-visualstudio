﻿// Copyright 2018 Google Inc. All Rights Reserved.
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

using Google.Apis.Auth.OAuth2;
using GoogleCloudExtension.DataSources;
using System;

namespace GoogleCloudExtension.Utils
{
    public interface IDataSourceFactory
    {
        /// <summary>
        /// The default data source for managing GCP project resources.
        /// </summary>
        IResourceManagerDataSource ResourceManagerDataSource { get; }

        /// <summary>
        /// The default data source for managing google accounts.
        /// </summary>
        IGPlusDataSource GPlusDataSource { get; }

        /// <summary>
        /// This event is triggered when account dependent DataSources have been updated.
        /// </summary>
        event EventHandler DataSourcesUpdated;

        IResourceManagerDataSource CreateResourceManagerDataSource();

        IGPlusDataSource CreatePlusDataSource();

        IGPlusDataSource CreatePlusDataSource(GoogleCredential credential);

        IGkeDataSource CreateGkeDataSource();

        //TODO(jimwp) Add GCE and GAE data sources.
    }
}