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
    /// This class owns the context on which to run kubectl commands.
    /// </summary>
    public class KubectlContext : IDisposable
    {
        /// <summary>
        /// Path to the config file that identifies the cluster for kubcectl commands.
        /// </summary>
        public string Config { get; private set; }

        public KubectlContext(string config)
        {
            Config = config;
        }

        #region IDisposable implementation.

        /// <summary>
        /// Release the resources owned by this instance, deletes the files silently.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (Config == null)
            {
                return;
            }

            try
            {
                File.Delete(Config);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to delete {Config}: {ex.Message}");
            }
            finally
            {
                Config = null;
            }
        }

        #endregion
    }
}