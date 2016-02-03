using System;
using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceInstanceViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private GceInstance _instance;

        public GceInstanceViewModel(GceInstance instance)
        {
            Content = instance.Name;
            _instance = instance;
        }

        public object Item
        {
            get
            {
                return new GcsInstanceItem(_instance);
            }
        }
    }      
}