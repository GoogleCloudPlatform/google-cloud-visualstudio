using GoogleCloudExtension.Utils;
using System.ComponentModel;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsFileItem : PropertyWindowItemBase
    {
        private readonly GcsRow _row;

        public GcsFileItem(GcsRow row) :
            base("GCS File", row.FileName)
        {
            _row = row;
        }

        [Category("File")]
        [DisplayName("Name")]
        [Description("Name of the blob.")]
        public string Name => _row.FileName;

        [Category("File")]
        [DisplayName("Size")]
        [Description("Size of the blob in bytes.")]
        public string Size => _row.Size.ToString();

        [Category("File")]
        [DisplayName("Last Modified")]
        [Description("The last modified time stamp of the blob.")]
        public string LasModified => _row.LastModified;

        [Category("File")]
        [DisplayName("GCS Path")]
        [Description("The full path to the blob.")]
        public string GcsPath => $"gs://{_row.Bucket}/{_row.Name}";
           
    }
}
