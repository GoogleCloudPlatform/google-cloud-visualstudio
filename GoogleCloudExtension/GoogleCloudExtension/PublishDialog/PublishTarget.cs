using GoogleCloudExtension.Utils;
using System.Windows.Controls;

namespace GoogleCloudExtension.PublishDialog
{
    public class PublishTarget : Model
    {
        public string Name { get; }

        public UserControl TargetUi { get; }
    }
}
