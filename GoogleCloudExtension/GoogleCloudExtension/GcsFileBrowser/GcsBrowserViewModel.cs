using Google.Apis.Storage.v1.Data;
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
            set { SetValueAndRaise(ref _pathSteps, value); }
        }

        public ObservableCollection<GcsItem> Items { get; } = new ObservableCollection<GcsItem>();

        private void InvalidateBucket()
        {
                        
        }
    }
}
