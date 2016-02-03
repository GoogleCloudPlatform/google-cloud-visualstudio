using System;
using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceInstanceViewModel : TreeLeaf
    {
        private GceInstance _instance;

        public GceInstanceViewModel(GceInstance instance)
        {
            Content = instance.Name;
            _instance = instance;
        }
    }      
}