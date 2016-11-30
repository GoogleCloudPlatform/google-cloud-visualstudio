using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string Name => _row.FileName;

        [Category("File")]
        [DisplayName("Size")]
        public string Size => _row.Size.ToString();

        [Category("File")]
        [DisplayName("Last Modified")]
        public string LasModified => _row.LastModified;
           
    }
}
