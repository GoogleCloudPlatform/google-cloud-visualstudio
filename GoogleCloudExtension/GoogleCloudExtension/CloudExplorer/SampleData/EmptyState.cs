using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorer.SampleData
{
    /// <summary>
    /// This class provides a mocked <seealso cref="CloudExplorerViewModel"/> in the no account state.
    /// </summary>
    public class EmptyState : SampleDataBase
    {
        public bool IsEmptyState { get; } = true;

        public string EmptyStateMessage { get; } = Resources.CloudExplorerNoAccountMessage;

        public string EmptyStateButtonCaption { get; } = Resources.CloudExplorerNoAccountButtonCaption;
    }
}
