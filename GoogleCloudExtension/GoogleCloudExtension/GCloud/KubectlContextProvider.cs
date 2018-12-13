// Copyright 2018 Google Inc. All Rights Reserved.
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

using Google.Apis.Container.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.Utils;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// A service that can create an <see cref="IKubectlContext"/> from a cluster.
    /// </summary>
    [Export(typeof(IKubectlContextProvider))]
    public class KubectlContextProvider : IKubectlContextProvider
    {
        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly Lazy<IProcessService> _processService;
        private readonly Lazy<ICredentialsStore> _credentialsStore;

        private ICredentialsStore CredentialsStore => _credentialsStore.Value;
        private IFileSystem FileSystem => _fileSystem.Value;
        private IProcessService ProcessService => _processService.Value;

        [ImportingConstructor]
        public KubectlContextProvider(
            Lazy<IFileSystem> fileSystem,
            Lazy<ICredentialsStore> credentialsStore,
            Lazy<IProcessService> processService)
        {
            _fileSystem = fileSystem;
            _credentialsStore = credentialsStore;
            _processService = processService;
        }

        /// <summary>
        /// Returns the <seealso cref="KubectlContext"/> instance to use for the given <paramref name="cluster"/> when
        /// performing Kubernetes operations.
        /// </summary>
        /// <param name="cluster">The cluster to create credentials for.</param>
        /// <returns>The <seealso cref="KubectlContext"/> for the given <paramref name="cluster"/>.</returns>
        public async Task<IKubectlContext> GetKubectlContextForClusterAsync(Cluster cluster)
        {
            var kubectlContext = new KubectlContext(FileSystem, _processService, CredentialsStore);
            if (!await kubectlContext.InitClusterCredentialsAsync(cluster))
            {
                throw new GCloudException($"Failed to get credentials for cluster {cluster.Name}");
            }

            return kubectlContext;
        }
    }
}