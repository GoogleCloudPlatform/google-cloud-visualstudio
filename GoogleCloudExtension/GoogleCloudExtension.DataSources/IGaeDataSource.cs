using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public interface IGaeDataSource
    {
        Task<IList<string>> GetFlexLocationsAsync();
    }
}
