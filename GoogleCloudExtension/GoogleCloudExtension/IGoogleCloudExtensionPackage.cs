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

        T GetDialogPage<T>() where T : DialogPage;
        void ShowOptionPage<T>() where T : DialogPage;
        void SubscribeClosingEvent(EventHandler handler);
        void UnsubscribeClosingEvent(EventHandler handler);
        ToolWindowPane FindToolWindow(Type toolWindowType, int id, bool create);
    }
}