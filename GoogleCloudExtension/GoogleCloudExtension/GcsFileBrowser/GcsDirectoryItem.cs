using GoogleCloudExtension.Utils;
using System.ComponentModel;

namespace GoogleCloudExtension.GcsFileBrowser
{
    internal class GcsDirectoryItem : PropertyWindowItemBase
    {
        private readonly GcsRow _directory;

        public GcsDirectoryItem(GcsRow row) :
            base("GCS Directory", row.FileName)
        {
            _directory = row;
        }

        [Category("Directory")]
        [DisplayName("Name")]
        public string Name => _directory.FileName;
    }
}