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

using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;

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
        private static IServiceProvider s_serviceProvider;

        /// <summary>
        /// Initialize the helper, most importantly provides the service provider to use
        /// to get to the log service.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use to get the log service.</param>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            s_serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Logs a information entry into the log.
        /// </summary>
        /// <param name="entry">The log entry.</param>
        public static void LogInfo(string entry)
        {
            Debug.WriteLine($"Info: {entry}");
            var log = GetActivityLog();
            log?.LogEntry(
                (int)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
                nameof(GoogleCloudExtensionPackage),
                entry);
        }

        /// <summary>
        /// Logs an error entry into the log.
        /// </summary>
        /// <param name="entry">The log entry.</param>
        public static void LogError(string entry)
        {
            Debug.WriteLine($"Error: {entry}");
            var log = GetActivityLog();
            log.LogEntry(
                (int)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                nameof(GoogleCloudExtensionPackage),
                entry);
        }

        /// <summary>
        /// This method retrieves the activity log service, as per the documentation a new interface must
        /// be fetched everytime it is needed, which is why not caching is performed.
        /// See https://msdn.microsoft.com/en-us/library/bb166359.aspx for more details.
        /// </summary>
        /// <returns>The current instance of IVsACtivityLog to use.</returns>
        private static IVsActivityLog GetActivityLog()
        {
            var log = s_serviceProvider.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
            Debug.WriteLineIf(log == null, "Failed to obtain IVsActivityLog interface.");
            return log;
        }
    }
}
