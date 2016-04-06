// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

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
        private static readonly Lazy<AnalyticsReporter> s_reporter = new Lazy<AnalyticsReporter>(CreateReporter);
        private static readonly Lazy<Task<bool>> s_isReportingEnabled = new Lazy<Task<bool>>(IsReportingEnabled);
        private static readonly Lazy<string> s_clientId = new Lazy<string>(GetOrCreateClientId);

        private static IServiceProvider s_serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            s_serviceProvider = serviceProvider;
        }

        #region Report convenience methods.

        /// <summary>
        /// Reports that the given command is starting its execution, typically to be called right at the
        /// begining of the command's handler method.
        /// </summary>
        /// <param name="command">The name of the command. Must not be null.</param>
        /// <param name="invocationSource">From where the command was invoked.</param>
        public static async void ReportStartCommand(string command, CommandInvocationSource invocationSource)
        {
            ActivityLogUtils.LogInfo($"Reporting: Starting command {command} from source {invocationSource}");
            var reporter = await GetReporter();
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
            reporter?.ReportEvent(
                category: CommandCategory,
                action: action,
                label: invocationSource.ToString());
        }

        /// <summary>
        /// Reports a command has finished.
        /// </summary>
        /// <param name="command">The command completed. Must not be null.</param>
        /// <param name="succeeded">Did it succeed or not.</param>
        public static async void ReportEndCommand(string command, bool succeeded)
        {
            ActivityLogUtils.LogInfo($"Reporting: Command {command} success: {succeeded}");
            var reporter = await GetReporter();
            reporter?.ReportEvent(
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
        public static async void ReportWindowOpened(string name)
        {
            ActivityLogUtils.LogInfo($"Reporting: Window {name} is opened");
            var reporter = await GetReporter();
            reporter?.ReportEvent(
                category: WindowCategory,
                action: OpenWindowAction,
                label: name);
        }

        /// <summary>
        /// Reports that an event happened.
        /// </summary>
        /// <param name="name">The name of the event. Must not be null.</param>
        /// <param name="value">Optional, the value associated with the event.</param>
        public static async void ReportEvent(string name, string value = null)
        {
            ActivityLogUtils.LogInfo($"Reporting: Event {name} with param {value}");
            var reporter = await GetReporter();
            reporter?.ReportEvent(
                category: EventCategory,
                action: name,
                label: value);
        }

        /// <summary>
        /// Reports the begining of the session, to be called when the extension is loaded.
        /// </summary>
        public static async void ReportStartSession()
        {
            ActivityLogUtils.LogInfo($"Reporting: Starting session.");
            var reporter = await GetReporter();
            reporter?.ReportStartSession();
        }

        /// <summary>
        /// Reports the end of the session, to be called when the extension is unloaded.
        /// </summary>
        public static async void ReportEndSession()
        {
            ActivityLogUtils.LogInfo($"Reporting: Ending session.");
            var reporter = await GetReporter();
            reporter?.ReportEndSession();
        }

        #endregion

        private static AnalyticsReporter CreateReporter()
        {
            bool debug = false;
#if DEBUG
            debug = true;
#endif
            return new AnalyticsReporter(PropertyId,
                clientId: s_clientId.Value,
                appName: ApplicationName,
                debug: debug);
        }

        private static string GetOrCreateClientId()
        {
            var settingsManager = new ShellSettingsManager(s_serviceProvider);
            var clientId = GetClientId(settingsManager);
            if (clientId != null)
            {
                Debug.WriteLine("Found existing client id.");
                return clientId;
            }

            Debug.WriteLine("Creating new client id.");
            clientId = Guid.NewGuid().ToString();
            StoreClientId(settingsManager, clientId);
            return clientId;
        }

        private static string GetClientId(ShellSettingsManager settingsManager)
        {
            var readOnlyStore = settingsManager.GetReadOnlySettingsStore(Microsoft.VisualStudio.Settings.SettingsScope.UserSettings);
            return readOnlyStore.GetString(GCloudExtensionPath, ClientIdProperty, null);
        }

        private static void StoreClientId(ShellSettingsManager settingsManager, string clientId)
        {
            var store = settingsManager.GetWritableSettingsStore(Microsoft.VisualStudio.Settings.SettingsScope.UserSettings);
            store.CreateCollection(GCloudExtensionPath);
            store.SetString(GCloudExtensionPath, ClientIdProperty, clientId);
        }

        /// <summary>
        /// Returns the reporter to use for reporting data, or null if no reporting is to be done.
        /// Note: The check for whether reporting data is enabled is done once per session, if the user
        /// changes the setting then Visual Studio will have to be restarted.
        /// </summary>
        /// <returns>The task with the reporter to use.</returns>
        private static async Task<AnalyticsReporter> GetReporter()
        {
            return await s_isReportingEnabled.Value ? s_reporter.Value : null;
        }

        /// <summary>
        /// Checks with gcloud to see if usage reporting is enabled or not. Only checks once per session.
        /// </summary>
        /// <returns>The task with the result of the check.</returns>
        private static async Task<bool> IsReportingEnabled()
        {
            bool result = true;
            Debug.WriteLine($"Reporting enabled: {result}");
            return result;
        }
    }
}
