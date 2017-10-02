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

using EnvDTE;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.AttachDebuggerDialog;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.GenerateConfigurationCommand;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.StackdriverErrorReporting;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using SystemTasks = System.Threading.Tasks;

namespace GoogleCloudExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GoogleCloudExtensionPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideToolWindow(typeof(CloudExplorerToolWindow))]
    [ProvideToolWindow(typeof(LogsViewerToolWindow), DocumentLikeTool = true, Transient = true)]
    [ProvideToolWindow(typeof(ErrorReportingToolWindow), DocumentLikeTool = true, Transient = true)]
    [ProvideToolWindow(typeof(ErrorReportingDetailToolWindow), DocumentLikeTool = true, Transient = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    [ProvideOptionPage(typeof(AnalyticsOptionsPage), "Google Cloud Tools", "Usage Report", 0, 0, false)]
    [ProvideToolWindow(typeof(GcsFileBrowser.GcsFileBrowserWindow), MultiInstances = true, Transient = true, DocumentLikeTool = true)]
    public sealed class GoogleCloudExtensionPackage : Package
    {
        private static readonly Lazy<string> s_appVersion = new Lazy<string>(() => Assembly.GetExecutingAssembly().GetName().Version.ToString());

        /// <summary>
        /// DeployToGaePackage GUID string.
        /// </summary>
        public const string PackageGuidString = "3784fd98-7fcc-40fc-be3b-b68334735af2";

        /// <summary>
        /// Option keys for the extension options.
        /// </summary>
        private const string NoneValue = "/none";

        // This value is used to change the maximum concurrent connections of the HttpClient instances created
        // in the VS process, including the ones used by GCP API services.
        private const int MaximumConcurrentConnections = 10;

        // The properties that are stored in the .suo file.
        private static List<SolutionUserOptions> s_userSettings = new List<SolutionUserOptions>()
        {
            new SolutionUserOptions(CurrentAccountProjectSettings.Current),
            new SolutionUserOptions(AttachDebuggerSettings.Current),
        };

        private DTE _dteInstance;
        private event EventHandler _closingEvent;

        /// <summary>
        /// The application name to use everywhere one is needed. Analytics, data sources, etc...
        /// </summary>
        public static string ApplicationName => "google-cloud-visualstudio";

        /// <summary>
        /// The version of the extension's main assembly.
        /// </summary>
        public static string ApplicationVersion => s_appVersion.Value;

        /// <summary>
        /// The version of Visual Studio currently running.
        /// </summary>
        public static string VsVersion { get; private set; }

        /// <summary>
        /// The edition of Visual Studio currently running.
        /// </summary>
        public static string VsEdition { get; private set; }

        /// <summary>
        /// Returns the versioned application name in the right format for analytics, etc...
        /// </summary>
        public static string VersionedApplicationName => $"{ApplicationName}/{ApplicationVersion}";

        public GoogleCloudExtensionPackage()
        {
            // Register all of the properties.
            RegisterSolutionOptions();
        }

        /// <summary>
        /// Subscribe to the solution/package closing event.
        /// </summary>
        public void SubscribeClosingEvent(EventHandler handler)
        {
            _closingEvent += handler;
        }

        /// <summary>
        /// Unsubscribe to the solution/package closing event.
        /// </summary>
        public void UnsubscribeClosingEvent(EventHandler handler)
        {
            _closingEvent -= handler;
        }

        protected override int QueryClose(out bool canClose)
        {
            _closingEvent?.Invoke(this, EventArgs.Empty);
            if (_closingEvent != null)
            {
                var tasks = new List<SystemTasks.Task>();
                foreach (var handler in _closingEvent.GetInvocationList().OfType<EventHandler>())
                {
                    tasks.Add(SystemTasks.Task.Run(() => handler(this, EventArgs.Empty)));
                };

                SystemTasks.Task.WaitAll(tasks.ToArray(), TimeSpan.FromMilliseconds(1000));
            }
            return base.QueryClose(out canClose);
        }

        #region Persistence of solution options

        private void RegisterSolutionOptions()
        {
            foreach (var key in s_userSettings.SelectMany(setting => setting.Keys))
            {
                AddOptionKey(key);
            }
        }

        protected override void OnLoadOptions(string key, Stream stream)
        {
            SolutionUserOptions userSettings = s_userSettings.FirstOrDefault(x => x.Contains(key));
            if (userSettings != null)
            {
                userSettings.Set(key, ReadOptionStream(stream));
            }
            else
            {
                base.OnLoadOptions(key, stream);
            }
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            SolutionUserOptions userSettings = s_userSettings.FirstOrDefault(x => x.Contains(key));
            if (userSettings == null)
            {
                return;
            }

            var value = userSettings.Read(key);
            WriteOptionStream(stream, value ?? NoneValue);
        }

        private void WriteOptionStream(Stream stream, string value)
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(value);
            }
        }

        private string ReadOptionStream(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var value = reader.ReadLine();
                return value == NoneValue ? null : value;
            }
        }

        #endregion

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // An remember the package.
            Instance = this;

            // Register the command handlers.
            CloudExplorerCommand.Initialize(this);
            ManageAccountsCommand.Initialize(this);
            PublishProjectMainMenuCommand.Initialize(this);
            PublishProjectContextMenuCommand.Initialize(this);
            LogsViewerToolWindowCommand.Initialize(this);
            GenerateConfigurationContextMenuCommand.Initialize(this);
            ErrorReportingToolWindowCommand.Initialize(this);

            // Activity log utils, to aid in debugging.
            ActivityLogUtils.Initialize(this);
            ActivityLogUtils.LogInfo("Starting Google Cloud Tools.");

            _dteInstance = (DTE)Package.GetGlobalService(typeof(DTE));
            VsVersion = _dteInstance.Version;
            VsEdition = _dteInstance.Edition;

            // Update the installation status of the package.
            CheckInstallationStatus();

            // Ensure the commands UI state is updated when the GCP project changes.
            CredentialsStore.Default.Reset += (o, e) => ShellUtils.InvalidateCommandsState();
            CredentialsStore.Default.CurrentProjectIdChanged += (o, e) => ShellUtils.InvalidateCommandsState();

            // With this setting we allow more concurrent connections from each HttpClient instance created
            // in the process. This will allow all GCP API services to have more concurrent connections with
            // GCP servers. The first benefit of this is that we can upload more concurrent files to GCS.
            ServicePointManager.DefaultConnectionLimit = MaximumConcurrentConnections;
        }

        public static GoogleCloudExtensionPackage Instance { get; private set; }

        #endregion

        #region User Settings

        public AnalyticsOptionsPage AnalyticsSettings => (AnalyticsOptionsPage)GetDialogPage(typeof(AnalyticsOptionsPage));

        #endregion

        /// <summary>
        /// Checks the installed version vs the version that is running, and will report either a new install
        /// if no previous version is found, or an upgrade if a lower version is found. If the same version
        /// is found, nothing is reported.
        /// </summary>
        private void CheckInstallationStatus()
        {
            var settings = AnalyticsSettings;
            if (settings.InstalledVersion == null)
            {
                // This is a new installation.
                Debug.WriteLine("New installation detected.");
                EventsReporterWrapper.ReportEvent(NewInstallEvent.Create());
            }
            else if (settings.InstalledVersion != ApplicationVersion)
            {
                // This is an upgrade (or different version installed).
                Debug.WriteLine($"Found new version {settings.InstalledVersion} different than current {ApplicationVersion}");

                Version current, installed;
                if (!Version.TryParse(ApplicationVersion, out current))
                {
                    Debug.WriteLine($"Invalid application version: {ApplicationVersion}");
                    return;
                }
                if (!Version.TryParse(settings.InstalledVersion, out installed))
                {
                    Debug.WriteLine($"Invalid installed version: {settings.InstalledVersion}");
                    return;
                }

                if (installed < current)
                {
                    Debug.WriteLine($"Upgrade to version {ApplicationVersion} detected.");
                    EventsReporterWrapper.ReportEvent(UpgradeEvent.Create());
                }
            }
            else
            {
                Debug.WriteLine($"Same version {settings.InstalledVersion} detected.");
            }

            // Update the stored settings with the current version.
            settings.InstalledVersion = ApplicationVersion;
            settings.SaveSettingsToStorage();
        }
    }
}
