using GoogleCloudExtension.CloudExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class GcsSource : ICloudExplorerSource
    {
        private readonly GcsSourceRootViewModel _root = new GcsSourceRootViewModel();

        #region ICloudExplorerSource implementation.

        public TreeHierarchy GetRoot()
        {
            return _root;
        }

        public void Refresh()
        {
            _root.Refresh();   
        }

        #endregion
    }
}
