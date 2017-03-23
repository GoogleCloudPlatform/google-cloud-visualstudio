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
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.Analytics
{
    /// <summary>
    /// Helper class to deal with analytics events.
    /// </summary>
    internal abstract class EventsReporterWrapper
    {
        public const string ExtensionEventType = "visualstudio";
        public const string ExtensionEventSource = "virtual.visualstudio";

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
                settings.OptIn = UserPromptUtils.ActionPrompt(
                    Resources.AnalyticsPromptMessage,
                    Resources.AnalyticsPromptTitle,
                    actionCaption: Resources.UiYesButtonCaption,
                    cancelCaption: Resources.UiNoButtonCaption);
                settings.DialogShown = true;
                settings.SaveSettingsToStorage();
            }
        }

        /// <summary>
        /// Called when the user changes the opt-in state to invalidate the state of the reporter.
        /// </summary>
        public static void AnalyticsOptInStateChanged()
        {
            s_reporter = new Lazy<IEventsReporter>(CreateReporter);
        }

        /// <summary>
        /// Called to report an interesting event to analytics. If there's a queue of events it will be
        /// flushed as well.
        /// </summary>
        /// <param name="eventData"></param>
        public static void ReportEvent(AnalyticsEvent eventData)
        {
            s_reporter.Value?.ReportEvent(
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
                    appName: GoogleCloudExtensionPackage.ApplicationName,
                    appVersion: GoogleCloudExtensionPackage.ApplicationVersion,
                    debug: true,
                    userAgent: GoogleCloudExtensionPackage.VersionedApplicationName);
                return new DebugEventReporter(analyticsReporter);

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
