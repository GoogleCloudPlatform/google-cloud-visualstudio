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

using Google.Apis.Auth.OAuth2;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using System;
using System.ComponentModel.Composition;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    ///  Holder of data source factory methods.
    /// </summary>
    [Export(typeof(IDataSourceFactory))]
    public class DataSourceFactory : IDataSourceFactory
    {
        [Obsolete("This makes a call to MEF every time. Instead, import IDataSourceFactory from MEF and save to an instance member.")]
        public static IDataSourceFactory Default => GoogleCloudExtensionPackage.Instance.GetService<IDataSourceFactory>();

        public ResourceManagerDataSource CreateResourceManagerDataSource()
        {
            GoogleCredential currentCredential = CredentialsStore.Default.CurrentGoogleCredential;
            if (currentCredential != null)
            {
                return new ResourceManagerDataSource(
                    currentCredential, GoogleCloudExtensionPackage.Instance.VersionedApplicationName);
            }
            else
            {
                return null;
            }
        }

        public IGPlusDataSource CreatePlusDataSource()
        {
            GoogleCredential currentCredential = CredentialsStore.Default.CurrentGoogleCredential;
            return CreatePlusDataSource(currentCredential);
        }

        public IGPlusDataSource CreatePlusDataSource(GoogleCredential credential)
        {
            if (credential != null)
            {
                return new GPlusDataSource(currentCredential, GoogleCloudExtensionPackage.Instance.VersionedApplicationName);
            }
            else
            {
                return null;
            }
        }
    }
}