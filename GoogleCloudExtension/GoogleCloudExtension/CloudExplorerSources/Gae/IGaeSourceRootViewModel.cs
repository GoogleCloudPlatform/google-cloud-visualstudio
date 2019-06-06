using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;

namespace GoogleCloudExtension.CloudExplorerSources.Gae
{
    internal interface IGaeSourceRootViewModel: ISourceRootViewModelBase
    {
        TreeLeaf ApiNotEnabledPlaceholder { get; }
        IGaeDataSource DataSource { get; }
        TreeLeaf ErrorPlaceholder { get; }
        Application GaeApplication { get; }
        TreeLeaf LoadingPlaceholder { get; }
        TreeLeaf NoItemsPlaceholder { get; }
        IList<string> RequiredApis { get; }
        string RootCaption { get; }

        void Initialize(ICloudSourceContext context);
        Task InvalidateServiceAsync(string id);

        /// <summary>
        /// Returns the context in which this source root view model is working.
        /// </summary>
        ICloudSourceContext Context { get; }
    }
}