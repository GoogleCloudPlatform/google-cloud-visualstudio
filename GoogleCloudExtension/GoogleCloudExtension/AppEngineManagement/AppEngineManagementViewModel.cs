using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.AppEngineManagement
{
    public class AppEngineManagementViewModel : ViewModelBase
    {
        private readonly AppEngineManagementWindow _owner;
        private LocationName _selectedLocation;

        public LocationName SelectedLocation
        {
            get { return _selectedLocation; }
            set { SetValueAndRaise(ref _selectedLocation, value); }
        }

        public string Result { get; private set; }

        public string Message { get; }

        public ICommand ActionCommand { get; }

        public AsyncProperty<IEnumerable<LocationName>> Locations { get; }

        public AppEngineManagementViewModel(AppEngineManagementWindow owner, string projectId)
        {
            _owner = owner;

            Locations = new AsyncProperty<IEnumerable<LocationName>>(ListAllLocationsAsync());
            ActionCommand = new ProtectedCommand(OnActionCommand);
            Message = string.Format(Resources.AppEngineManagementAppCreationMessage, projectId);
        }

        private void OnActionCommand()
        {
            Result = SelectedLocation.DisplayName;
            _owner.Close();
        }

        private async Task<IEnumerable<LocationName>> ListAllLocationsAsync()
        {
            var source = new GaeDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.VersionedApplicationName);
            IEnumerable<LocationName> result = (await source.GetFlexLocationsAsync()).OrderBy(x => x.DisplayName);
            SelectedLocation = result.FirstOrDefault(x => x.DisplayName == "us-central");
            return result;
        }
    }
}
