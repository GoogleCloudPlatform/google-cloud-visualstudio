using System;
using System.Collections.Generic;
using System.Linq;

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
