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
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
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
    [Guid(GoogleCloudExtensionPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideToolWindow(typeof(CloudExplorerToolWindow))]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    public sealed class GoogleCloudExtensionPackage : Package
    {
        /// <summary>
        /// DeployToGaePackage GUID string.
        /// </summary>
        public const string PackageGuidString = "3784fd98-7fcc-40fc-be3b-b68334735af2";

        private DTE _dteInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeployToAppEngine"/> class.
        /// </summary>
        public GoogleCloudExtensionPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Register the command handlers.
            CloudExplorerCommand.Initialize(this);
            ManageAccountsCommand.Initialize(this);

            // Activity log utils, to aid in debugging.
            ActivityLogUtils.Initialize(this);
            ActivityLogUtils.LogInfo("Starting Google Cloud Tools.");

            // Analytics reporting.
            ExtensionAnalytics.Initialize(this);
            ExtensionAnalytics.ReportStartSession();

            _dteInstance = (DTE)Package.GetGlobalService(typeof(DTE));
            _dteInstance.Events.DTEEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;

            // An d remember the package.
            Instance = this;
        }

        public static GoogleCloudExtensionPackage Instance { get; private set; }

        private void DTEEvents_OnBeginShutdown()
        {
            ActivityLogUtils.LogInfo("Shutting down Google Cloud Tools.");
            ExtensionAnalytics.ReportEndSession();
        }

        #endregion

        #region Global state of the extension

        private static bool s_isDeploying;
        public static bool IsDeploying
        {
            get { return s_isDeploying; }
            set
            {
                if (s_isDeploying != value)
                {
                    s_isDeploying = value;
                    ShellUtils.InvalidateCommandUIStatus();
                }
            }
        }

        #endregion
    }
}
