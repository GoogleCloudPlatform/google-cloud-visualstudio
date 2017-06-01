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

namespace GoogleCloudExtension.Team
{
    /// <summary>
    /// Implement interface <seealso cref="ITeamExplorerUtils"/>. 
    /// These methods has dependencies on Microsoft.TeamFoundation.Controls etc.
    /// This must be built for VS2015 and VS2017 respectively.
    /// </summary>
    internal class TeamExplorerUtils : ITeamExplorerUtils
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly GitExtentionWrapper _gitExtension;

        /// <summary>
        /// Initializes an instance of class <seealso cref="ITeamExplorerUtils"/>
        /// </summary>
        /// <param name="serviceProvider">
        /// Visual Studio service provider object that is used to query for service providers.
        /// </param>
        public TeamExplorerUtils(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider.ThrowIfNull(nameof(serviceProvider));
            _gitExtension = new GitExtentionWrapper(_serviceProvider);
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

        #endregion

        private ITeamExplorer TeamExplorer => GetService<ITeamExplorer>();

        private ITeamExplorerNotificationManager NotificationManager => 
            TeamExplorer as ITeamExplorerNotificationManager;

        private TService GetService<TService>() where TService : class => 
            _serviceProvider.GetService(typeof(TService)) as TService;
    }
}
