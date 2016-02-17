using GoogleCloudExtension.CloudExplorer;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal class GceSource : ICloudExplorerSource
    {
        private GceSourceRootViewModel _root = new GceSourceRootViewModel();

        public TreeHierarchy GetRoot()
        {
            return _root;
        }

        public void Refresh()
        {
            _root.Refresh();
        }
    }
}
