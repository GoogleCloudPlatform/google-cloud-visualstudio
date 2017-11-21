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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.AppEngineManagement
{
    /// <summary>
    /// This class is the view model for the dialog.
    /// </summary>
    public class AppEngineManagementViewModel : ViewModelBase
    {
        /// <summary>
        /// The region to select by default for the user.
        /// </summary>
        internal const string DefaultRegionName = "us-central";

        /// <summary>
        /// The placeholder for the list of regions being loaded.
        /// Note: This variable is internal so it can be accessed by tests.
        /// </summary>
        internal static readonly IEnumerable<string> s_loadingPlaceholder = new string[]
        {
            Resources.AppEngineManagementLoadingRegionsPlaceholder
        };

        private readonly ICloseable _owner;
        private string _selectedLocation;
        private readonly IGaeDataSource _dataSource;

        /// <summary>
        /// The currently selected location.
        /// </summary>
        public string SelectedLocation
        {
            get { return _selectedLocation; }
            set { SetValueAndRaise(ref _selectedLocation, value); }
        }

        /// <summary>
        /// The result of the dialog, which will be a location string.
        /// </summary>
        public string Result { get; private set; }

        /// <summary>
        /// The project id for which the dialog is being shown.
        /// </summary>
        public string ProjectId { get; }

        /// <summary>
        /// The command to execute with the action button, the OK button.
        /// </summary>
        public ICommand ActionCommand { get; }

        /// <summary>
        /// The list of locations.
        /// </summary>
        public AsyncProperty<IEnumerable<string>> Locations { get; }

        public AppEngineManagementViewModel(ICloseable owner, string projectId)
            : this(owner, projectId, CreateDataSource())
        { }

        /// <summary>
        /// Constructor used for testing.
        /// </summary>
        internal AppEngineManagementViewModel(ICloseable owner, string projectId, IGaeDataSource dataSource)
        {
            _owner = owner;
            _dataSource = dataSource;

            Locations = new AsyncProperty<IEnumerable<string>>(ListAllLocationsAsync(), s_loadingPlaceholder);
            SelectedLocation = s_loadingPlaceholder.First();
            ActionCommand = new ProtectedCommand(OnActionCommand);
            ProjectId = projectId;
        }

        private void OnActionCommand()
        {
            Result = SelectedLocation;
            _owner?.Close();
        }

        private async Task<IEnumerable<string>> ListAllLocationsAsync()
        {
            try
            {
                IEnumerable<string> result = (await _dataSource.GetFlexLocationsAsync()).OrderBy(x => x);
                SelectedLocation = DefaultRegionName;
                return result;
            }
            catch (DataSourceException ex)
            {
                UserPromptUtils.ExceptionPrompt(ex);
                _owner.Close();
                return Enumerable.Empty<string>();
            }
        }

        private static IGaeDataSource CreateDataSource()
            => new GaeDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.VersionedApplicationName);
    }
}
