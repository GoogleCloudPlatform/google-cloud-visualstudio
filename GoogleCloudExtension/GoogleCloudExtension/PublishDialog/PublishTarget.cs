using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.PublishDialog
{
    public class PublishTarget : Model
    {
        public string Name { get; }

        public UserControl TargetUi { get; }
    }
}
