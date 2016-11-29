using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsBrowserViewModel : ViewModelBase
    {
        private Bucket _bucket;
        private IEnumerable<string> _pathSteps;
        private GcsDataSource _dataSource;
        private IEnumerable<GcsItem> _items;
        private bool _isLoading;

        public Bucket Bucket
        {
            get { return _bucket; }
            set
            {
                SetValueAndRaise(ref _bucket, value);
                InvalidateBucket();
            }
        }

        public IEnumerable<string> PathSteps
        {
            get { return _pathSteps; }
            private set { SetValueAndRaise(ref _pathSteps, value); }
        }

        public string CurrentPath => String.Join("/", PathSteps);

        public IEnumerable<GcsItem> Items
        {
            get { return _items; }
            private set { SetValueAndRaise(ref _items, value); }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            private set
            {
                SetValueAndRaise(ref _isLoading, value);
                RaisePropertyChanged(nameof(IsReady));
            }
        }

        public bool IsReady => !IsLoading;

        private void InvalidateBucket()
        {
            _dataSource = new GcsDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            PathSteps = Enumerable.Empty<string>();
            ReloadItems();
        }

        private async void ReloadItems()
        {
            if (IsLoading)
            {
                return;
            }

            try
            {
                IsLoading = true;

                var dir = await _dataSource.GetDirectoryListAsync(Bucket.Name, CurrentPath);
                Items = Enumerable.Concat<GcsItem>(
                    dir.Prefixes.OrderBy(x => x).Select(x => new GcsItem(x)),
                    dir.Items.OrderBy(x=> x.Name).Select(x => new GcsItem(x)));
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
