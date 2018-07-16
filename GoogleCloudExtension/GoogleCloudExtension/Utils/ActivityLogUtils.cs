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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This utility class implements helpers to log to the activity log, which will help
    /// debug issues with the extension that only our customers can reproduce. The log is only
    /// written to if the /log parameter is passed to Visual Studio on startup, other than that
    /// writing to the log is a noop.
    /// </summary>
    internal static class ActivityLogUtils
    {

        /// <summary>
        /// Logs a information entry into the log.
        /// </summary>
        /// <param name="activityLogService">The <see cref="IVsActivityLog"/> to log the entry.</param>
        /// <param name="entry">The log entry.</param>
        public static void LogInfo(this IVsActivityLog activityLogService, string entry)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            activityLogService.LogEntry(
                (int)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
                nameof(GoogleCloudExtensionPackage),
                entry);
        }

        /// <summary>
        /// Logs an error entry into the log.
        /// </summary>
        /// <param name="activityLogService">The <see cref="IVsActivityLog"/> to log the entry.</param>
        /// <param name="entry">The log entry.</param>
        public static void LogError(this IVsActivityLog activityLogService, string entry)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            activityLogService.LogEntry(
                (int)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                nameof(GoogleCloudExtensionPackage),
                entry);
        }
    }
}
