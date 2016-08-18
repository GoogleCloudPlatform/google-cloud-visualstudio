using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class BucketLocationViewModel : TreeHierarchy
    {
        private const string IconResourcePath = "CloudExplorerSources/Gce/Resources/zone_icon.png";

        private static readonly Lazy<ImageSource> s_zoneIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private readonly GcsSourceRootViewModel _owner;
        private IEnumerable<BucketViewModel> _buckets;

        public BucketLocationViewModel(GcsSourceRootViewModel owner, string name, IEnumerable<BucketViewModel> buckets)
        {
            _owner = owner;
            _buckets = buckets;

            Caption = name;
            Icon = s_zoneIcon.Value;
            foreach (var bucket in _buckets)
            {
                Children.Add(bucket);
            }
        }
    }
}
