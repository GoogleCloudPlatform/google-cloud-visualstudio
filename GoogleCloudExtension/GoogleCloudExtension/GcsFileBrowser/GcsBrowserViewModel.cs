using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsBrowserViewModel : ViewModelBase
    {
        private readonly static GcsBrowserState s_emptyState =
            new GcsBrowserState(Enumerable.Empty<GcsItem>(), "/");

        private Bucket _bucket;
        private GcsDataSource _dataSource;
        private bool _isLoading;
        private readonly List<GcsBrowserState> _stateStack = new List<GcsBrowserState>();

        public Bucket Bucket
        {
            get { return _bucket; }
            set
            {
                SetValueAndRaise(ref _bucket, value);
                InvalidateBucket();
            }
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

        public GcsBrowserState Top
        {
            get
            {
                if (_stateStack.Count == 0)
                {
                    return s_emptyState;
                }
                return _stateStack.Last();
            }
        }

        public bool IsReady => !IsLoading;

        public ICommand PopAllCommand { get; }

        public ICommand NavigateToCommand { get; }

        public ICommand ShowDirectoryCommand { get; }

        public GcsBrowserViewModel()
        {
            PopAllCommand = new ProtectedCommand(OnPopAllCommand);
            NavigateToCommand = new ProtectedCommand<string>(OnNavigateToCommand);
            ShowDirectoryCommand = new ProtectedCommand<GcsItem>(OnShowDirectoryCommand);
        }

        private void OnNavigateToCommand(string step)
        {
            PopToState(step);
        }

        private void OnPopAllCommand()
        {
            PopToRoot();
        }

        private void PopToRoot()
        {
            _stateStack.RemoveRange(1, _stateStack.Count - 1);
            RaisePropertyChanged(nameof(Top));
        }

        private void PopToState(string step)
        {
            var idx = _stateStack.FindIndex(x => x.Name == step);
            if (idx == -1)
            {
                Debug.WriteLine($"Could not find {step}");
            }

            _stateStack.RemoveRange(idx + 1, _stateStack.Count - (idx + 1));
            RaisePropertyChanged(nameof(Top));
        }

        private async void PushToDirectory(string name)
        {
            var state = await LoadStateForDirectoryAsync(name);
            _stateStack.Add(state);
            RaisePropertyChanged(nameof(Top));
        }

        private void OnShowDirectoryCommand(GcsItem dir)
        {
            PushToDirectory(dir.Name);
        }

        private void InvalidateBucket()
        {
            _dataSource = new GcsDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            PushToDirectory("");
        }

        private async Task<GcsBrowserState> LoadStateForDirectoryAsync(string name)
        {
            try
            {
                IsLoading = true;

                var dir = await _dataSource.GetDirectoryListAsync(Bucket.Name, name);
                var items = Enumerable.Concat<GcsItem>(
                    dir.Prefixes.OrderBy(x => x).Select(x => new GcsItem(x)),
                    dir.Items.OrderBy(x=> x.Name).Select(x => new GcsItem(x)));

                return new GcsBrowserState(items, name);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
