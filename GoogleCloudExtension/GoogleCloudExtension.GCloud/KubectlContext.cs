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

using System;
using System.Diagnostics;
using System.IO;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class owns the context on which to run kubectl commands. This class owns
    /// the config file, when the instance is disposed it will delete the file.
    /// </summary>
    public class KubectlContext : IDisposable
    {
        /// <summary>
        /// Path to the config file that identifies the cluster for kubcectl commands.
        /// </summary>
        public string ConfigPath { get; private set; }

        /// <summary>
        /// Path to the application credentials to use while calling into kubectl.
        /// </summary>
        public string CredentialsPath { get; private set; }

        public KubectlContext(string configPath, string credentialsPath)
        {
            ConfigPath = configPath;
            CredentialsPath = credentialsPath;
        }

        #region IDisposable implementation.

        /// <summary>
        /// Release the resources owned by this instance, deletes the files silently.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (ConfigPath == null)
            {
                return;
            }

            try
            {
                File.Delete(ConfigPath);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to delete {ConfigPath}: {ex.Message}");
            }
            finally
            {
                ConfigPath = null;
            }
        }

        #endregion
    }
}