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

using GoogleCloudExtension.Options;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;

namespace GoogleCloudExtension
{
    /// <summary>
    /// Interface extracted from <see cref="GoogleCloudExtensionPackage"/> mainly for testing purposes.
    /// </summary>
    public interface IGoogleCloudExtensionPackage : IVsPackage, Microsoft.VisualStudio.OLE.Interop.IServiceProvider, IOleCommandTarget, IVsPersistSolutionOpts, IServiceContainer, System.IServiceProvider, IVsUserSettings, IVsUserSettingsMigration, IVsUserSettingsQuery, IVsToolWindowFactory, IVsToolboxItemProvider
    {
        AnalyticsOptions AnalyticsSettings { get; }
        string VsVersion { get; }

        T GetDialogPage<T>() where T : DialogPage;
        bool IsWindowActive();
        void ShowOptionPage<T>() where T : DialogPage;
        void SubscribeClosingEvent(EventHandler handler);
        void UnsubscribeClosingEvent(EventHandler handler);
        ToolWindowPane FindToolWindow(Type toolWindowType, int id, bool create);

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
        /// <returns></returns>
        I GetService<S, I>();

        /// <summary>
        /// Gets a service registered and used as one type.
        /// </summary>
        /// <typeparam name="T">The type of the service.</typeparam>
        /// <returns></returns>
        T GetService<T>() where T : class;
    }
}
