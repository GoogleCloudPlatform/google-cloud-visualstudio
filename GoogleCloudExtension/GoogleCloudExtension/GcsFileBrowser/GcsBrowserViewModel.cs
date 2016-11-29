using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsBrowserViewModel : ViewModelBase
    {
        private Bucket _bucket;

        public Bucket Bucket
        {
            get { return _bucket; }
            set { SetValueAndRaise(ref _bucket, value); }
        }
    }
}
