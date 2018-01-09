using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer.SampleData
{
    public abstract class SampleDataBase
    {
        private const string ContainerIconResourcePath = "CloudExplorerSources/Gce/Resources/zone_icon.png";
        private const string InstanceIconResourcePath = "CloudExplorerSources/Gce/Resources/instance_icon_running.png";

        protected static readonly Lazy<ImageSource> s_containerIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(ContainerIconResourcePath));
        protected static readonly Lazy<ImageSource> s_instanceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(InstanceIconResourcePath));

        public IList<ButtonDefinition> Buttons { get; } = new List<ButtonDefinition>
        {
            new ButtonDefinition { Icon = s_containerIcon.Value },
            new ButtonDefinition { Icon = s_instanceIcon.Value }
        };
    }
}
