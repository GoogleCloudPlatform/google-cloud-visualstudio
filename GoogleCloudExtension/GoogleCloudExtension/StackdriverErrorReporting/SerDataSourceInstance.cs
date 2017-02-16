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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources.ErrorReporting;
using System;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Create a single instance of <seealso cref="SerDataSource"/>.
    /// <seealso cref="ErrorReportingViewModel"/> and <seealso cref="ErrorReportingDetailViewModel"/> use the same instance.
    /// </summary>
    public static class SerDataSourceInstance
    {
        private static Lazy<SerDataSource> s_instance = new Lazy<SerDataSource>(CreateDataSource);

        /// <summary>
        /// Gets an instance of 
        /// </summary>
        public static SerDataSource Instance => s_instance.Value;

        /// <summary>
        /// When the current project id is changed, the Instance needs to be recreated.
        /// </summary>
        public static void OnProjectIdChanged()
        {
            s_instance = new Lazy<SerDataSource>(CreateDataSource);
        }

        /// <summary>
        /// Create <seealso cref="LoggingDataSource"/> object with current project id.
        /// </summary>
        private static SerDataSource CreateDataSource()
        {
            if (CredentialsStore.Default.CurrentProjectId == null)
            {
                return null;
            }
            return new SerDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.VersionedApplicationName);
        }
    }
}
