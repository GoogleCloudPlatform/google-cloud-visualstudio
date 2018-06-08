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
using GoogleCloudExtension.Options;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.StackdriverErrorReporting;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

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
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideToolWindow(typeof(CloudExplorerToolWindow))]
    [ProvideToolWindow(typeof(LogsViewerToolWindow), DocumentLikeTool = true, Transient = true)]
    [ProvideToolWindow(typeof(ErrorReportingToolWindow), DocumentLikeTool = true, Transient = true)]
    [ProvideToolWindow(typeof(ErrorReportingDetailToolWindow), DocumentLikeTool = true, Transient = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    [ProvideOptionPage(typeof(AnalyticsOptions), OptionsCategoryName, "Usage Report", 0, 0, false, Sort = 0)]
    public sealed class GoogleCloudExtensionPackage : Package, IGoogleCloudExtensionPackage
    {
        private static readonly Lazy<string> s_appVersion = new Lazy<string>(() => Assembly.GetExecutingAssembly().GetName().Version.ToString());

        /// <summary>
        /// DeployToGaePackage GUID string.
        /// </summary>
        private const string PackageGuidString = "3784fd98-7fcc-40fc-be3b-b68334735af2";

        /// <summary>
        /// Option keys for the extension options.
        /// </summary>
        private const string NoneValue = "/none";

        // This value is used to change the maximum concurrent connections of the HttpClient instances created
        // in the VS process, including the ones used by GCP API services.
        private const int MaximumConcurrentConnections = 10;

        private const string OptionsCategoryName = "Google Cloud Tools";

        // The properties that are stored in the .suo file.
        private static readonly List<SolutionUserOptions> s_userSettings = new List<SolutionUserOptions>
        {
            new SolutionUserOptions(CurrentAccountProjectSettings.Current),
            new SolutionUserOptions(AttachDebuggerSettings.Current)
        };

        private DTE _dteInstance;
        private Lazy<IShellUtils> _shellUtilsLazy;
        private Lazy<IGcpOutputWindow> _gcpOutputWindowLazy;
        private event EventHandler ClosingEvent;

        /// <summary>
        /// The application name to use everywhere one is needed. Analytics, data sources, etc...
        /// </summary>
        public string ApplicationName { get; } = "google-cloud-visualstudio";

        /// <summary>
        /// The version of the extension's main assembly.
        /// </summary>
        public string ApplicationVersion => s_appVersion.Value;

        /// <summary>
        /// The version of Visual Studio currently running.
        /// </summary>
        public string VsVersion { get; private set; }

        /// <summary>
        /// The edition of Visual Studio currently running.
        /// </summary>
        public static string VsEdition { get; private set; }

        /// <summary>
        /// Returns the versioned application name in the right format for analytics, etc...
        /// </summary>
        public string VersionedApplicationName => $"{ApplicationName}/{ApplicationVersion}";

        public IShellUtils ShellUtils => _shellUtilsLazy.Value;
        public IGcpOutputWindow GcpOutputWindow => _gcpOutputWindowLazy.Value;

        /// <summary>
        /// The initalized instance of the package.
        /// </summary>
        public static IGoogleCloudExtensionPackage Instance { get; internal set; }

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
            ClosingEvent += handler;
        }

        /// <summary>
        /// Unsubscribe to the solution/package closing event.
        /// </summary>
        public void UnsubscribeClosingEvent(EventHandler handler)
        {
            ClosingEvent -= handler;
        }

        /// <summary>
        /// Check whether the main window is not minimized.
        /// </summary>
        /// <returns>true/false based on whether window is minimized or not</returns>
        public bool IsWindowActive()
        {
            return _dteInstance.MainWindow?.WindowState != vsWindowState.vsWindowStateMinimize;
        }

        protected override int QueryClose(out bool canClose)
        {
            ClosingEvent?.Invoke(this, EventArgs.Empty);
            return base.QueryClose(out canClose);
        }

        #region Persistence of solution options

        private void RegisterSolutionOptions()
        {
            foreach (string key in s_userSettings.SelectMany(setting => setting.Keys))
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

            string value = userSettings.Read(key);
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
                string value = reader.ReadLine();
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

            _dteInstance = (DTE)GetService(typeof(DTE));
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

            ExportProvider mefExportProvider = GetService<SComponentModel, IComponentModel>().DefaultExportProvider;
            _shellUtilsLazy = mefExportProvider.GetExport<IShellUtils>();
            _gcpOutputWindowLazy = mefExportProvider.GetExport<IGcpOutputWindow>();
        }

        /// <summary>Gets type-based services from the VSPackage service container.</summary>
        /// <param name="serviceType">The type of service to retrieve.</param>
        /// <returns>An instance of the requested service, or null if the service could not be found.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="serviceType" /> is null.</exception>
        protected override object GetService(Type serviceType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return base.GetService(serviceType) ?? ServiceProvider.GlobalProvider.GetService(serviceType);
        }

        /// <summary>
        /// Gets a service registered as one type and used as a different type.
        /// </summary>
        /// <typeparam name="I">The type the service is used as (e.g. IVsService).</typeparam>
        /// <typeparam name="S">The type the service is registered as (e.g. SVsService).</typeparam>
        /// <returns>The service.</returns>
        public I GetService<S, I>()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return (I)GetService(typeof(S));
        }

        /// <summary>
        /// Gets an <see href="https://docs.microsoft.com/en-us/dotnet/framework/mef/">MEF</see> service.
        /// </summary>
        /// <typeparam name="T">The type the service is exported as.</typeparam>
        /// <returns>The service.</returns>
        public T GetMefService<T>() where T : class
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetService<SComponentModel, IComponentModel>().GetService<T>();
        }

        /// <summary>
        /// Gets an <see href="https://docs.microsoft.com/en-us/dotnet/framework/mef/">MEF</see> service.
        /// </summary>
        /// <typeparam name="T">The type the service is exported as.</typeparam>
        /// <returns>The <see cref="Lazy{T}"/> that initalizes the service.</returns>
        public Lazy<T> GetMefServiceLazy<T>() where T : class
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetService<SComponentModel, IComponentModel>().DefaultExportProvider.GetExport<T>();
        }

        #endregion

        #region User Settings

        public AnalyticsOptions AnalyticsSettings => GetDialogPage<AnalyticsOptions>();

        /// <summary>
        /// Gets the options page of the given type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="DialogPage"/> to get.</typeparam>
        /// <returns>The options page of the given type.</returns>
        public T GetDialogPage<T>() where T : DialogPage => (T)GetDialogPage(typeof(T));

        /// <summary>
        /// Displays the options page of the given type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="DialogPage"/> to display.</typeparam>
        public void ShowOptionPage<T>() where T : DialogPage => ShowOptionPage(typeof(T));

        #endregion

        /// <summary>
        /// Finds and returns an instance of the given tool window.
        /// </summary>
        /// <typeparam name="TToolWindow">The type of tool window to get.</typeparam>
        /// <param name="create">Whether to create a new tool window if the given one is not found.</param>
        /// <param name="id">The instance id of the tool window. Defaults to 0.</param>
        /// <returns>
        /// The tool window instance, or null if the given id does not already exist and create was false.
        /// </returns>
        public TToolWindow FindToolWindow<TToolWindow>(bool create, int id = 0) where TToolWindow : ToolWindowPane
        {
            return FindToolWindow(typeof(TToolWindow), id, create) as TToolWindow;
        }

        /// <summary>
        /// Checks the installed version vs the version that is running, and will report either a new install
        /// if no previous version is found, or an upgrade if a lower version is found. If the same version
        /// is found, nothing is reported.
        /// </summary>
        private void CheckInstallationStatus()
        {
            AnalyticsOptions settings = AnalyticsSettings;
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
