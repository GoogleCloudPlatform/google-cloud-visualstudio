using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.AppEngineManagement
{
    public class AppEngineManagementViewModel : ViewModelBase
    {
        private readonly AppEngineManagementWindow _owner;
        private string _selectedLocation;

        public string SelectedLocation
        {
            get { return _selectedLocation; }
            set { SetValueAndRaise(ref _selectedLocation, value); }
        }

        public AsyncProperty<IEnumerable<string>> Locations { get; }

        public AppEngineManagementViewModel(AppEngineManagementWindow owner)
        {
            _owner = owner;

            Locations = new AsyncProperty<IEnumerable<string>>(ListAllLocationsAsync());
        }

        private async Task<IEnumerable<string>> ListAllLocationsAsync()
        {
            var source = new GaeDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.VersionedApplicationName);
            var possibleLocations = await source.GetAvailableLocationsAsync();

            return possibleLocations.Where(x => x.IsFlexEnabled()).Select(x => x.LocationId);
        }
    }
}
