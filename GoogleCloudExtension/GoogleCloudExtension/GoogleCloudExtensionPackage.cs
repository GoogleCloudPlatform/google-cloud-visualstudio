﻿// Copyright 2016 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
    [ProvideOptionPage(typeof(AnalyticsOptionsPage), "Google Cloud Tools", "Usage Report", 0, 0, false)]
    public sealed class GoogleCloudExtensionPackage : Package
    {
        /// <summary>
        /// DeployToGaePackage GUID string.
        /// </summary>
        public const string PackageGuidString = "3784fd98-7fcc-40fc-be3b-b68334735af2";

        /// <summary>
        /// Option keys for the extension options.
        /// </summary>
        private const string CurrentGcpProjectKey = "google_current_gcp_project";
        private const string CurrentGcpAccountKey = "google_current_gcp_credentials";
        private const string NoneValue = "/none";

        // The properties that are stored in the .suo file.
        private static readonly Dictionary<string, Func<string>> s_propertySources = new Dictionary<string, Func<string>>
        {
            { CurrentGcpProjectKey, () => CredentialsStore.Default.CurrentProjectId },
            { CurrentGcpAccountKey, () => CredentialsStore.Default.CurrentAccount?.AccountName },
        };

        private DTE _dteInstance;
        private Dictionary<string, string> _properties;

        public GoogleCloudExtensionPackage()
        {
            // Register all of the properties.
            foreach (var key in s_propertySources.Keys)
            {
                AddOptionKey(key);
            }
        }

        #region Persistence of solution options

        protected override void OnLoadOptions(string key, Stream stream)
        {
            if (s_propertySources.Keys.Contains(key))
            {
                StoreLoadedProperty(key, stream);
            }
            else
            {
                base.OnLoadOptions(key, stream);
            }
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            Func<string> valueSource;
            if (!s_propertySources.TryGetValue(key, out valueSource))
            {
                return;
            }

            var value = valueSource();
            WriteOptionStream(stream, value ?? NoneValue);
        }

        private void StoreLoadedProperty(string key, Stream stream)
        {
            if (_properties == null)
            {
                _properties = new Dictionary<string, string>();
            }
            _properties[key] = ReadOptionStream(stream);


            if (_properties.Count == s_propertySources.Count)
            {
                // All of the properties have been loaded, commit them.
                CommitProperties();
                _properties = null;
            }
        }

        private void CommitProperties()
        {
            if (_properties[CurrentGcpAccountKey] != null)
            {
                Debug.WriteLine("Setting the user and project.");
                CredentialsStore.Default.ResetCredentials(
                    accountName: _properties[CurrentGcpAccountKey],
                    projectId: _properties[CurrentGcpProjectKey]);
            }
            else
            {
                Debug.WriteLine("No user loaded.");
            }
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

            // Activity log utils, to aid in debugging.
            ActivityLogUtils.Initialize(this);
            ActivityLogUtils.LogInfo("Starting Google Cloud Tools.");

            // Analytics reporting.
            ExtensionAnalytics.ReportStartSession();

            _dteInstance = (DTE)Package.GetGlobalService(typeof(DTE));
            _dteInstance.Events.DTEEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;
        }

        public static GoogleCloudExtensionPackage Instance { get; private set; }

        private void DTEEvents_OnBeginShutdown()
        {
            ActivityLogUtils.LogInfo("Shutting down Google Cloud Tools.");
            ExtensionAnalytics.ReportEndSession();
        }

        #endregion

        #region User Settings

        public AnalyticsOptionsPage AnalyticsSettings => (AnalyticsOptionsPage)GetDialogPage(typeof(AnalyticsOptionsPage));

        #endregion
    }
}
