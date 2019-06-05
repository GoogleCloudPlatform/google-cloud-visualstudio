// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Utils;
using Microsoft.TeamFoundation.Controls;
using System;
using System.Windows.Input;

namespace GoogleCloudExtension.TeamExplorerExtension
{
    /// <summary>
    /// Implement interface <seealso cref="ITeamExplorerUtils"/>. 
    /// These methods has dependencies on Microsoft.TeamFoundation.Controls.dll.
    /// VS2015 and VS2017 have different versions of Microsoft.TeamFoundation.Controls.dll.
    /// This source code will be compiled into two separate assemblies. 
    /// One is with VS2015 version of Microsoft.TeamFoundation.Controls.dll, 
    /// The other one is with VS2017 version of Microsoft.TeamFoundation.Controls.dll.
    /// </summary>
    internal class TeamExplorerUtils : ITeamExplorerUtils
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly GitExtensionWrapper _gitExtension;

        /// <summary>
        /// Returns ITeamExplorer interface, return value can be null.
        /// </summary>
        private ITeamExplorer TeamExplorer => _serviceProvider.GetService(typeof(ITeamExplorer)) as ITeamExplorer;

        /// <summary>
        /// Returns ITeamExplorerNotificationManager interface, return value can be null.
        /// </summary>
        private ITeamExplorerNotificationManager NotificationManager =>
            TeamExplorer as ITeamExplorerNotificationManager;

        /// <summary>
        /// Initializes an instance of class <seealso cref="ITeamExplorerUtils"/>
        /// </summary>
        /// <param name="serviceProvider">
        /// Visual Studio service provider object that is used to query for service providers.
        /// </param>
        public TeamExplorerUtils(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider.ThrowIfNull(nameof(serviceProvider));
            _gitExtension = new GitExtensionWrapper(_serviceProvider);
        }

        #region Implement interface ITeamExplorerUtils

        public string GetActiveRepository() => _gitExtension.GetActiveRepository();

        public void ShowHomeSection() => TeamExplorer?.NavigateToPage(new Guid(TeamExplorerPageIds.Home), null);

        public void ShowMessage(string message, ICommand command) =>
            NotificationManager?.ShowNotification(
                message,
                NotificationType.Information,
                NotificationFlags.None,
                command,
                default(Guid));

        public void ShowError(string message) =>
            NotificationManager?.ShowNotification(
                message,
                NotificationType.Error,
                NotificationFlags.RequiresConfirmation,
                null,
                default(Guid));

        #endregion
    }
}
