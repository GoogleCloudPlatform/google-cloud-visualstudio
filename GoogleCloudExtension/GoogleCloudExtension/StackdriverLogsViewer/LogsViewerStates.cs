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

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Stores global states for Logs Viewer.
    /// TODO: persist the state to machine cache.
    /// </summary>
    public class StackdriverLogsViewerStates
    {
        private static Lazy<StackdriverLogsViewerStates> s_logsViewerStates;

        /// <summary>
        /// Singleton pattern. Disable creation outside this class.
        /// </summary>
        private StackdriverLogsViewerStates() { }

        /// <summary>
        /// Gets the global states for Stackdriver logs viewer feature. 
        /// </summary>
        public static StackdriverLogsViewerStates Current => s_logsViewerStates.Value;

        /// <summary>
        /// When navigating from log entry to source line,
        /// this flag indicates to use the current project whose version does not match the verion info of the log line.
        /// </summary>
        public bool ContinueWithVersionMismatchAssemblyFlag { get; private set; } = false;

        /// <summary>
        /// Set <seealso cref="ContinueWithVersionMismatchAssemblyFlag"/> to true.
        /// </summary>
        public void SetContinueWithVersionMismatchAssemblyFlag() => ContinueWithVersionMismatchAssemblyFlag = true;
    }
}
