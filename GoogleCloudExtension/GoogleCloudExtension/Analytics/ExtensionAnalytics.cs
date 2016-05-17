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

using GoogleAnalyticsUtils;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.Analytics
{
    /// <summary>
    /// Helper class to deal with reporting intereting statistcs to Google Analytics.
    /// </summary>
    internal static class ExtensionAnalytics
    {
        private const string PropertyId = "UA-71653866-1";
        private const string ApplicationName = "Google Cloud Tools for Visual Studio";

        private static Lazy<AnalyticsReporter> s_reporter = new Lazy<AnalyticsReporter>(CreateReporter);

        /// <summary>
        /// Ensures that the opt-in dialog is shown to the user.
        /// </summary>
        public static void EnsureAnalyticsOptIn()
        {
            var settings = GoogleCloudExtensionPackage.Instance.AnalyticsSettings;
            if (!settings.DialogShown)
            {
                Debug.WriteLine("Showing the opt-in dialog.");
                settings.OptIn = UserPromptUtils.YesNoPrompt("Do you want to help Google by reporting usage statics?", "Usage Statistics");
                settings.DialogShown = true;
                settings.SaveSettingsToStorage();
            }
        }

        #region Report convenience methods.

        /// <summary>
        /// Reports that the given command is starting its execution, typically to be called right at the
        /// begining of the command's handler method.
        /// </summary>
        /// <param name="command">The name of the command. Must not be null.</param>
        /// <param name="invocationSource">From where the command was invoked.</param>
        public static void ReportCommand(CommandName command, CommandInvocationSource invocationSource)
        {
            ActivityLogUtils.LogInfo($"Reporting: Starting command {command} from source {invocationSource}");

            s_reporter.Value?.ReportEvent(
                category: command.ToString(),
                action: invocationSource.ToString());
        }

        /// <summary>
        /// Reports an interesting event to Google Analytics.
        /// </summary>
        /// <param name="category">The event category.</param>
        /// <param name="action">The action being taken place.</param>
        public static void ReportEvent(string category, string action)
        {
            s_reporter.Value?.ReportEvent(category, action);
        }

        public static void ReportScreen(string name)
        {
            s_reporter.Value?.ReportScreen(name);
        }

        /// <summary>
        /// Reports the begining of the session, to be called when the extension is loaded.
        /// </summary>
        public static void ReportStartSession()
        {
            ActivityLogUtils.LogInfo($"Reporting: Starting session.");

            s_reporter.Value?.ReportStartSession();
        }

        /// <summary>
        /// Reports the end of the session, to be called when the extension is unloaded.
        /// </summary>
        public static void ReportEndSession()
        {
            ActivityLogUtils.LogInfo($"Reporting: Ending session.");

            s_reporter.Value?.ReportEndSession();
        }

        #endregion

        public static void AnalyticsOptInStateChanged()
        {
            s_reporter = new Lazy<AnalyticsReporter>(CreateReporter);
        }

        private static AnalyticsReporter CreateReporter()
        {
            var settings = GoogleCloudExtensionPackage.Instance.AnalyticsSettings;

            if (settings.OptIn)
            {
                Debug.WriteLine("Analytics report enabled.");
                bool debug = false;
#if DEBUG
                debug = true;
#endif
                return new AnalyticsReporter(PropertyId,
                    clientId: settings.ClientId,
                    appName: ApplicationName,
                    debug: debug);
            }
            else
            {
                Debug.WriteLine("Analytics report disabled.");
                return null;
            }
        }
    }
}
