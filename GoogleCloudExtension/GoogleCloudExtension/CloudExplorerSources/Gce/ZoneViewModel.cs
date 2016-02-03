using GoogleCloudExtension.CloudExplorer;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class ZoneViewModel : TreeHierarchy
    {
        public ZoneViewModel(string key, IEnumerable<GceInstance> instances)
        {
            Content = key;

            var viewModels = instances.Select(x => new GceInstanceViewModel(x));
            foreach (var viewModel in viewModels)
            {
                Children.Add(viewModel);
            }
        }
    }
}