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
        [Description("The name of the directory.")]
        public string Name => _directory.FileName;
    }
}