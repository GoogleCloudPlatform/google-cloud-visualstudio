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
using EnvDTE80;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.AttachDebuggerDialog;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.GenerateConfigurationCommand;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.MenuBarControls;
using GoogleCloudExtension.Options;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.StackdriverErrorReporting;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.Utils;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [ProvideToolWindow(typeof(CloudExplorerToolWindow))]
    [ProvideToolWindow(typeof(LogsViewerToolWindow), DocumentLikeTool = true, Transient = true)]
    [ProvideToolWindow(typeof(ErrorReportingToolWindow), DocumentLikeTool = true, Transient = true)]
    [ProvideToolWindow(typeof(ErrorReportingDetailToolWindow), DocumentLikeTool = true, Transient = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(AnalyticsOptions), OptionsCategoryName, AnalyticsOptions.PageName, 1, 2, false)]
    [ProvideUIProvider(GcpMenuBarControlFactory.GuidString, "GCP Main Frame Control Factory", PackageGuidString)]
    [ProvideMainWindowFrameControl(
        typeof(GcpMenuBarControl),
        GcpMenuBarControlFactory.GcpMenuBarControlCommandId,
        typeof(GcpMenuBarControlFactory))]
    public sealed class GoogleCloudExtensionPackage : AsyncPackage, IGoogleCloudExtensionPackage
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

        private Lazy<IShellUtils> _shellUtilsLazy;
        private Lazy<IGcpOutputWindow> _gcpOutputWindowLazy;
        private Lazy<IProcessService> _processService;
        private Lazy<IStatusbarService> _statusbarService;
        private Lazy<IUserPromptService> _userPromptService;
        private Lazy<IDataSourceFactory> _dataSourceFactory;
        private IComponentModel _componentModel;

        private event EventHandler ClosingEvent;

        /// <summary>
        /// The initalized instance of the package.
        /// </summary>
        public static IGoogleCloudExtensionPackage Instance { get; internal set; }

        public AnalyticsOptions GeneralSettings
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return GetDialogPage<AnalyticsOptions>();
            }
        }

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
        public string VsVersion => Dte.Version;

        /// <summary>
        /// The edition of Visual Studio currently running.
        /// </summary>
        public string VsEdition => Dte.Edition;

        /// <summary>
        /// Returns the versioned application name in the right format for analytics, etc...
        /// </summary>
        public string VersionedApplicationName => $"{ApplicationName}/{ApplicationVersion}";

        public DTE2 Dte { get; private set; }

        /// <summary>
        /// The default <see cref="IShellUtils"/> service.
        /// </summary>
        public IShellUtils ShellUtils => _shellUtilsLazy.Value;

        /// <summary>
        /// The default <see cref="IGcpOutputWindow"/> service.
        /// </summary>
        public IGcpOutputWindow GcpOutputWindow => _gcpOutputWindowLazy.Value;

        /// <summary>
        /// The default <see cref="IProcessService"/>
        /// </summary>
        public IProcessService ProcessService => _processService.Value;

        /// <summary>
        /// The default <see cref="IStatusbarService"/>.
        /// </summary>
        public IStatusbarService StatusbarHelper => _statusbarService.Value;

        /// <summary>
        /// The default <see cref="IUserPromptService"/>.
        /// </summary>
        public IUserPromptService UserPromptService => _userPromptService.Value;

        /// <summary>
        /// The default <see cref="IDataSourceFactory"/> service.
        /// </summary>
        public IDataSourceFactory DataSourceFactory => _dataSourceFactory.Value;

        /// <summary>
        /// The default <see cref="ICredentialsStore"/> service.
        /// </summary>
        public ICredentialsStore CredentialsStore { get; private set; }

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
        public bool IsWindowActive() => Dte.MainWindow?.WindowState != vsWindowState.vsWindowStateMinimize;

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
        protected override async Task InitializeAsync(CancellationToken token, IProgress<ServiceProgressData> progress)
        {
            try
            {
                _componentModel = await GetServiceAsync<SComponentModel, IComponentModel>();
                CredentialsStore = _componentModel.GetService<ICredentialsStore>();
                ExportProvider mefExportProvider = _componentModel.DefaultExportProvider;
                _shellUtilsLazy = mefExportProvider.GetExport<IShellUtils>();
                _gcpOutputWindowLazy = mefExportProvider.GetExport<IGcpOutputWindow>();
                _processService = mefExportProvider.GetExport<IProcessService>();
                _statusbarService = mefExportProvider.GetExport<IStatusbarService>();
                _userPromptService = mefExportProvider.GetExport<IUserPromptService>();
                _dataSourceFactory = mefExportProvider.GetExport<IDataSourceFactory>();

                Dte = await GetServiceAsync<SDTE, DTE2>();

                // Remember the package.
                Instance = this;

                // Activity log utils, to aid in debugging.
                IVsActivityLog activityLog = await GetServiceAsync<SVsActivityLog, IVsActivityLog>();
                await activityLog.LogInfoAsync("Starting Google Cloud Tools.");

                // Register the command handlers.
                await Task.WhenAll(
                    CloudExplorerCommand.InitializeAsync(this, token),
                    ManageAccountsCommand.InitializeAsync(this, token),
                    PublishProjectMainMenuCommand.InitializeAsync(this, token),
                    PublishProjectContextMenuCommand.InitializeAsync(this, token),
                    LogsViewerToolWindowCommand.InitializeAsync(this, token),
                    GenerateConfigurationContextMenuCommand.InitializeAsync(this, token),
                    ErrorReportingToolWindowCommand.InitializeAsync(this, token));


                // Update the installation status of the package.
                await CheckInstallationStatusAsync();

                // Ensure the commands UI state is updated when the GCP project changes.
                CredentialsStore.CurrentProjectIdChanged += (o, e) => ShellUtils.InvalidateCommandsState();

                // With this setting we allow more concurrent connections from each HttpClient instance created
                // in the process. This will allow all GCP API services to have more concurrent connections with
                // GCP servers. The first benefit of this is that we can upload more concurrent files to GCS.
                ServicePointManager.DefaultConnectionLimit = MaximumConcurrentConnections;

                IVsRegisterUIFactories registerUIFactories =
                    await GetServiceAsync<SVsUIFactory, IVsRegisterUIFactories>();
                var controlFactory = _componentModel.GetService<GcpMenuBarControlFactory>();
                await registerUIFactories.RegisterUIFactoryAsync(controlFactory, token);
            }
            catch (Exception e)
            {
                IVsActivityLog activityLog = await GetServiceAsync<SVsActivityLog, IVsActivityLog>();
                await activityLog.LogErrorAsync(e.Message);
                await activityLog.LogErrorAsync(e.StackTrace);
            }
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
        /// Gets a service registered as one type and used as a different type.
        /// </summary>
        /// <typeparam name="I">The type the service is used as (e.g. IVsService).</typeparam>
        /// <typeparam name="S">The type the service is registered as (e.g. SVsService).</typeparam>
        /// <returns>The service.</returns>
        public async Task<I> GetServiceAsync<S, I>()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            return (I)await GetServiceAsync(typeof(S));
        }

        /// <summary>
        /// Gets an <see href="https://docs.microsoft.com/en-us/dotnet/framework/mef/">MEF</see> service.
        /// </summary>
        /// <typeparam name="T">The type the service is exported as.</typeparam>
        /// <returns>The service.</returns>
        public T GetMefService<T>() where T : class => _componentModel.GetService<T>();

        /// <summary>
        /// Gets an <see href="https://docs.microsoft.com/en-us/dotnet/framework/mef/">MEF</see> service.
        /// </summary>
        /// <typeparam name="T">The type the service is exported as.</typeparam>
        /// <returns>The <see cref="Lazy{T}"/> that initalizes the service.</returns>
        public Lazy<T> GetMefServiceLazy<T>() where T : class => _componentModel.DefaultExportProvider.GetExport<T>();

        #endregion

        #region User Settings

        /// <summary>
        /// Gets the options page of the given type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="DialogPage"/> to get.</typeparam>
        /// <returns>The options page of the given type.</returns>
        public T GetDialogPage<T>() where T : DialogPage
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return (T)GetDialogPage(typeof(T));
        }

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
        public TToolWindow FindToolWindow<TToolWindow>(
            bool create,
            int id = 0) where TToolWindow : ToolWindowPane
        {
            ToolWindowPane toolWindowPane = FindToolWindow(typeof(TToolWindow), id, create);
            return toolWindowPane as TToolWindow;
        }

        /// <summary>
        /// Checks the installed version vs the version that is running, and will report either a new install
        /// if no previous version is found, or an upgrade if a lower version is found. If the same version
        /// is found, nothing is reported.
        /// </summary>
        private async Task CheckInstallationStatusAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            AnalyticsOptions settings = GeneralSettings;
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

                if (!Version.TryParse(ApplicationVersion, out Version current))
                {
                    Debug.WriteLine($"Invalid application version: {ApplicationVersion}");
                    return;
                }
                if (!Version.TryParse(settings.InstalledVersion, out Version installed))
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
