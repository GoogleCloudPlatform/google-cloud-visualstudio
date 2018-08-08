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

using EnvDTE80;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Options;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using IServiceProvider = System.IServiceProvider;
using IVsServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace GoogleCloudExtension
{
    /// <summary>
    /// Interface extracted from <see cref="GoogleCloudExtensionPackage"/> mainly for testing purposes.
    /// </summary>
    public interface IGoogleCloudExtensionPackage
        : IVsPackage,
            IVsServiceProvider,
            IOleCommandTarget,
            IVsPersistSolutionOpts,
            IServiceContainer,
            IServiceProvider,
            IVsUserSettings,
            IVsUserSettingsMigration,
            IVsUserSettingsQuery,
            IVsToolWindowFactory,
            IVsToolboxItemProvider,
            IAsyncServiceProvider
    {
        JoinableTaskFactory JoinableTaskFactory { get; }
        AnalyticsOptions GeneralSettings { get; }
        string VsVersion { get; }

        /// <summary>
        /// The application name to use everywhere one is needed. Analytics, data sources, etc...
        /// </summary>
        string ApplicationName { get; }

        /// <summary>
        /// The version of the extension's main assembly.
        /// </summary>
        string ApplicationVersion { get; }

        /// <summary>
        /// Returns the versioned application name in the right format for analytics, etc...
        /// </summary>
        string VersionedApplicationName { get; }

        DTE2 Dte { get; }

        /// <summary>
        /// The default <see cref="IShellUtils"/> service.
        /// </summary>
        IShellUtils ShellUtils { get; }

        /// <summary>
        /// The default <see cref="IGcpOutputWindow"/> service.
        /// </summary>
        IGcpOutputWindow GcpOutputWindow { get; }

        /// <summary>
        /// The default <see cref="IProcessService"/>.
        /// </summary>
        IProcessService ProcessService { get; }

        /// <summary>
        /// The default <see cref="IStatusbarService"/>.
        /// </summary>
        IStatusbarService StatusbarHelper { get; }

        /// <summary>
        /// The default <see cref="IUserPromptService"/>.
        /// </summary>
        IUserPromptService UserPromptService { get; }

        /// <summary>
        /// The default <see cref="IDataSourceFactory"/> service.
        /// </summary>
        IDataSourceFactory DataSourceFactory { get; }

        /// <summary>
        /// The default <see cref="ICredentialsStore"/> service.
        /// </summary>
        ICredentialsStore CredentialsStore { get; }

        /// <summary>
        /// The edition of Visual Studio currently running.
        /// </summary>
        string VsEdition { get; }

        T GetDialogPage<T>() where T : DialogPage;
        bool IsWindowActive();

        void SubscribeClosingEvent(EventHandler handler);

        void UnsubscribeClosingEvent(EventHandler handler);

        /// <summary>
        /// Gets a service registered as one type and used as a different type.
        /// </summary>
        /// <typeparam name="I">The type the service is used as (e.g. IVsService).</typeparam>
        /// <typeparam name="S">The type the service is registered as (e.g. SVsService).</typeparam>
        /// <returns>The service.</returns>
        Task<I> GetServiceAsync<S, I>();

        /// <summary>
        /// Gets an exported MEF service.
        /// </summary>
        /// <typeparam name="T">The type of the service.</typeparam>
        /// <returns>The service.</returns>
        T GetMefService<T>() where T : class;

        /// <summary>
        /// Gets a lazily initialized <see href="https://docs.microsoft.com/en-us/dotnet/framework/mef/">MEF</see>
        /// service.
        /// </summary>
        /// <typeparam name="T">The type the service is exported as.</typeparam>
        /// <returns>A <see cref="Lazy{T}"/> that evaluates to the service.</returns>
        Lazy<T> GetMefServiceLazy<T>() where T : class;

        /// <summary>
        /// Finds and returns an instance of the given tool window.
        /// </summary>
        /// <typeparam name="TToolWindow">The type of tool window to get.</typeparam>
        /// <param name="create">Whether to create a new tool window if the given one is not found.</param>
        /// <param name="id">The instance id of the tool window. Defaults to 0.</param>
        /// <returns>
        /// The tool window instance, or null if the given id does not already exist and create was false.
        /// </returns>
        TToolWindow FindToolWindow<TToolWindow>(bool create, int id = 0) where TToolWindow : ToolWindowPane;

        /// <summary>
        /// Gets a service registered as one type and used as a different type.
        /// </summary>
        /// <typeparam name="I">The type the service is used as (e.g. IVsService).</typeparam>
        /// <typeparam name="S">The type the service is registered as (e.g. SVsService).</typeparam>
        /// <returns>The service.</returns>
        I GetService<S, I>();
    }
}
