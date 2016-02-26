using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorer
{
    public abstract class CloudExplorerSourceBase : ICloudExplorerSource
    {
        public virtual TreeHierarchy GetRoot()
        {
            throw new NotFiniteNumberException();
        }

        public virtual IEnumerable<ButtonDefinition> GetButtons() => Enumerable.Empty<ButtonDefinition>();

        public virtual void Refresh()
        {
            throw new NotImplementedException();
        }
    }
}
