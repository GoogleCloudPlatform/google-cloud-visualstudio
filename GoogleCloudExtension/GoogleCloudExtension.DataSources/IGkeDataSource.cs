using Google.Apis.Container.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This interface defines the generic GkeDataSource, which allows dependency injection of sources.
    /// </summary>
    public interface IGkeDataSource
    {
        /// <summary>
        /// Lists all of the clusters in the current project.
        /// </summary>
        /// <returns>The list of clusters.</returns>
        Task<IList<Cluster>> GetClusterListAsync();
    }
}
