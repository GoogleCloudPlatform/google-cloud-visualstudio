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
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics
{
    public enum CommandInvocationSource
    {
        None,
        ToolsMenu,
        ContextMenu,
        Button,
    }

    internal static class ExtensionAnalytics
    {
        private const string PropertyId = "UA-71653866-1";
        private const string ApplicationName = "Visual Studio Extension";

        private const string CommandCategory = "Command";
        private const string WindowCategory = "Window";
        private const string EventCategory = "Event";

        private const string OpenWindowAction = "OpenWindow";
        private const string EndCommandAction = "EndCommand";

        private const string SucceededLabel = "Success";
        private const string FailedLabel = "Failed";
        private const string FalseValue = "false";
        private const string GCloudExtensionPath = "GCloudVSExtension";
        private const string ClientIdProperty = "ClientId";

        private static Lazy<AnalyticsReporter> s_reporter = new Lazy<AnalyticsReporter>(CreateReporter);

        #region Report convenience methods.

        /// <summary>
        /// Reports that the given command is starting its execution, typically to be called right at the
        /// begining of the command's handler method.
        /// </summary>
        /// <param name="command">The name of the command. Must not be null.</param>
        /// <param name="invocationSource">From where the command was invoked.</param>
        public static void ReportStartCommand(string command, CommandInvocationSource invocationSource)
        {
            ActivityLogUtils.LogInfo($"Reporting: Starting command {command} from source {invocationSource}");

            string action;
            switch (invocationSource)
            {
                case CommandInvocationSource.ToolsMenu:
                    action = "StartCommandFromToolsMenu";
                    break;
                case CommandInvocationSource.ContextMenu:
                    action = "StartCommandFromContextMenu";
                    break;
                case CommandInvocationSource.Button:
                    action = "StartCommandFromButton";
                    break;
                default:
                    action = "StartCommand";
                    break;
            }

            s_reporter.Value?.ReportEvent(
                category: CommandCategory,
                action: action,
                label: invocationSource.ToString());
        }

        /// <summary>
        /// Reports a command has finished.
        /// </summary>
        /// <param name="command">The command completed. Must not be null.</param>
        /// <param name="succeeded">Did it succeed or not.</param>
        public static void ReportEndCommand(string command, bool succeeded)
        {
            ActivityLogUtils.LogInfo($"Reporting: Command {command} success: {succeeded}");

            s_reporter.Value?.ReportEvent(
                category: CommandCategory,
                action: EndCommandAction,
                label: succeeded ? SucceededLabel : FailedLabel);
        }

        /// <summary>
        /// Convenience wrapper to report a command when the command action is self contained.
        /// </summary>
        /// <param name="command">The name of the command. Must not be null.</param>
        /// <param name="source">From where the command was invoked.</param>
        /// <param name="action">The action to execute for the command.</param>
        public static void ReportCommand(string command, CommandInvocationSource source, Action action)
        {
            ReportStartCommand(command, source);
            bool succeeded = false;
            try
            {
                action();
                succeeded = true;
            }
            finally
            {
                ReportEndCommand(command, succeeded);
            }
        }

        /// <summary>
        /// Reports what tool window from the extension has been opened.
        /// </summary>
        /// <param name="name">The name of the window, typically <c>nameof(class)</c>. Must not be null.</param>
        public static void ReportWindowOpened(string name)
        {
            ActivityLogUtils.LogInfo($"Reporting: Window {name} is opened");

            s_reporter.Value?.ReportEvent(
                category: WindowCategory,
                action: OpenWindowAction,
                label: name);
        }

        /// <summary>
        /// Reports that an event happened.
        /// </summary>
        /// <param name="name">The name of the event. Must not be null.</param>
        /// <param name="value">Optional, the value associated with the event.</param>
        public static void ReportEvent(string name, string value = null)
        {
            ActivityLogUtils.LogInfo($"Reporting: Event {name} with param {value}");

            s_reporter.Value?.ReportEvent(
                category: EventCategory,
                action: name,
                label: value);
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
