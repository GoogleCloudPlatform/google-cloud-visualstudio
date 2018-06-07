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
using GoogleCloudExtension.Analytics.AnalyticsOptInDialog;
using GoogleCloudExtension.Options;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.Analytics
{
    /// <summary>
    /// Helper class to deal with analytics events.
    /// </summary>
    internal static class EventsReporterWrapper
    {
        /// <summary>
        /// Mockable static method for testing.
        /// </summary>
        internal static Func<bool> PromptAnalyticsOptIn { private get; set; } = AnalyticsOptInWindow.PromptUser;
        public const string ExtensionEventType = "visualstudio";
        public const string ExtensionEventSource = "virtual.visualstudio";

        private const string PropertyId = "UA-36037335-1";

        // For testing.
        internal static Lazy<IEventsReporter> ReporterLazy { private get; set; } = new Lazy<IEventsReporter>(CreateReporter);

        private static IEventsReporter Reporter => ReporterLazy.Value;


        /// <summary>
        /// Used by unit test to prevent analytics from running.
        /// </summary>
        internal static void DisableReporting()
        {
            ReporterLazy = new Lazy<IEventsReporter>(() => null);
        }

        /// <summary>
        /// Ensures that the opt-in dialog is shown to the user.
        /// </summary>
        public static void EnsureAnalyticsOptIn()
        {
            AnalyticsOptions settings = GoogleCloudExtensionPackage.Instance.AnalyticsSettings;
            if (!settings.DialogShown)
            {
                Debug.WriteLine("Showing the opt-in dialog.");
                settings.OptIn = PromptAnalyticsOptIn();
                settings.DialogShown = true;
                settings.SaveSettingsToStorage();
            }
        }

        /// <summary>
        /// Called when the user changes the opt-in state to invalidate the state of the reporter.
        /// </summary>
        public static void AnalyticsOptInStateChanged()
        {
            ReporterLazy = new Lazy<IEventsReporter>(CreateReporter);
        }

        /// <summary>
        /// Called to report an interesting event to analytics. If there's a queue of events it will be
        /// flushed as well.
        /// </summary>
        public static void ReportEvent(AnalyticsEvent eventData)
        {
            Reporter?.ReportEvent(
                source: ExtensionEventSource,
                eventType: ExtensionEventType,
                eventName: eventData.Name,
                userLoggedIn: CredentialsStore.Default.CurrentAccount != null,
                projectNumber: CredentialsStore.Default.CurrentProjectNumericId,
                metadata: eventData.Metadata);
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
                    appName: GoogleCloudExtensionPackage.Instance.ApplicationName,
                    appVersion: GoogleCloudExtensionPackage.Instance.ApplicationVersion,
                    debug: true,
                    userAgent: GoogleCloudExtensionPackage.Instance.VersionedApplicationName);
                return new DebugEventReporter(analyticsReporter);

#else
                var analyticsReporter = new AnalyticsReporter(PropertyId,
                    clientId: settings.ClientId,
                    appName: GoogleCloudExtensionPackage.Instance.ApplicationName,
                    appVersion: GoogleCloudExtensionPackage.Instance.ApplicationVersion,
                    debug: false,
                    userAgent: GoogleCloudExtensionPackage.Instance.VersionedApplicationName);
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
