using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorer.SampleData
{
    /// <summary>
    /// Mocked version of <seealso cref="CloudExplorerViewModel"/> in the busy state.
    /// </summary>
    public class LoadingState : SampleDataBase
    {
        public bool IsBusy { get; } = true;
    }
}
