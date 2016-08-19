using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    /// <summary>
    /// This class is the view model for the location of a bucket, showing the buckets
    /// under this location and the right icon.
    /// </summary>
    internal class BucketLocationViewModel : TreeHierarchy
    {
        private const string IconResourcePath = "CloudExplorerSources/Gcs/Resources/zone_icon.png";

        private static readonly Lazy<ImageSource> s_zoneIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(IconResourcePath));

        private readonly GcsSourceRootViewModel _owner;
        private IEnumerable<BucketViewModel> _buckets;

        public BucketLocationViewModel(GcsSourceRootViewModel owner, string name, IEnumerable<BucketViewModel> buckets)
        {
            _owner = owner;
            _buckets = buckets;

            Caption = $"{name} ({buckets.Count()})";
            Icon = s_zoneIcon.Value;
            foreach (var bucket in _buckets)
            {
                Children.Add(bucket);
            }
        }
    }
}
