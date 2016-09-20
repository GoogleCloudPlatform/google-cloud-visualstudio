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

        private static bool s_queueProcessed = false;
        private static Lazy<IEventsReporter> s_reporter = new Lazy<IEventsReporter>(CreateReporter);
        private static Lazy<List<Action>> s_eventQueue = new Lazy<List<Action>>();

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
            // Play all of the queue of events after the user been shown the opt-in dialog.
            if (!s_queueProcessed)
            {
                if (s_eventQueue.IsValueCreated)
                {
                    foreach (var action in s_eventQueue.Value)
                    {
                        action();
                    }
                    s_eventQueue.Value.Clear();
                }
                s_queueProcessed = true;
            }
        }

        /// <summary>
        /// Queues an event sending <paramref name="action"/> to be sent once the user is shown the
        /// opt-in dialog. If the opt-in dialog is already shown to the user then the action is invoked
        /// right away.
        /// </summary>
        public static void QueueEventCall(AnalyticsOptionsPage settings, Action action)
        {
            if (settings.DialogShown)
            {
                // If the opt-in dialog has already been shown to the user then we can send the
                // the event (or not) directly.
                action();
            }
            else
            {
                // If the opt-in dialog has not been shown yet to the user save the events for when it is.
                // Note: this has the risk that if the dialog is never shown we can miss this event.
                s_eventQueue.Value.Add(action);
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
