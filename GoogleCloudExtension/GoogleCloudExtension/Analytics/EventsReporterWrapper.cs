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
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GoogleCloudExtension.Analytics
{
    /// <summary>
    /// Helper class to deal with analytics events.
    /// </summary>
    internal abstract class EventsReporterWrapper
    {
        public const string ExtensionEventType = "vsextension";

        private const string PropertyId = "UA-36037335-1";

        private static Lazy<IEventsReporter> s_reporter = new Lazy<IEventsReporter>(CreateReporter);

        /// <summary>
        /// Ensures that the opt-in dialog is shown to the user.
        /// </summary>
        public static void EnsureAnalyticsOptIn()
        {
            var settings = GoogleCloudExtensionPackage.Instance.AnalyticsSettings;
            if (!settings.DialogShown)
            {
                Debug.WriteLine("Showing the opt-in dialog.");
                settings.OptIn = UserPromptUtils.YesNoPrompt(Resources.AnalyticsPromptMessage, Resources.AnalyticsPromptTitle);
                settings.DialogShown = true;
                settings.SaveSettingsToStorage();
            }
        }

        /// <summary>
        /// Called when the state if the opt-in changed, to enable/disable reporting after that.
        /// </summary>
        public static void AnalyticsOptInStateChanged()
        {
            s_reporter = new Lazy<IEventsReporter>(CreateReporter);
        }

        /// <summary>
        /// Called to report an event.
        /// </summary>
        public static void ReportEvent(
            string eventName,
            params string[] metadata)
        {
            var reporter = s_reporter.Value;
            if (reporter != null)
            {
                reporter.ReportEvent(
                    eventType: ExtensionEventType,
                    eventName: eventName,
                    projectNumber: CredentialsStore.Default.CurrentProjectNumericId,
                    metadata: GetMetadataFromParams(metadata));
            }
        }

        private static Dictionary<string, string> GetMetadataFromParams(string[] args)
        {
            if (args.Length == 0)
            {
                return null;
            }

            if ((args.Length % 2) != 0)
            {
                Debug.WriteLine($"Invalid count of params: {args.Length}");
                return null;
            }

            Dictionary<string, string> result = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i += 2)
            {
                result.Add(args[i], args[i + 1]);
            }
            return result;
        }

        private static IEventsReporter CreateReporter()
        {
            var settings = GoogleCloudExtensionPackage.Instance.AnalyticsSettings;

            if (settings.OptIn)
            {
                Debug.WriteLine("Analytics report enabled.");
#if DEBUG
                var analyticsReporter = new AnalyticsReporter(PropertyId,
                    clientId: settings.ClientId,
                    appName: GoogleCloudExtensionPackage.ApplicationName,
                    appVersion: GoogleCloudExtensionPackage.ApplicationVersion,
                    debug: true,
                    userAgent: GoogleCloudExtensionPackage.VersionedApplicationName);
                return new DebugEventReporter(new EventsReporter(analyticsReporter));

#else
                var analyticsReporter = new AnalyticsReporter(PropertyId,
                    clientId: settings.ClientId,
                    appName: GoogleCloudExtensionPackage.ApplicationName,
                    appVersion: GoogleCloudExtensionPackage.ApplicationVersion,
                    debug: false,
                    userAgent: GoogleCloudExtensionPackage.VersionedApplicationName);
                 return new EventsReporter(analyticsReporter);
#endif
            }
            else
            {
                Debug.WriteLine("Analytics report disabled.");
                return null;
            }
        }
    }
}
