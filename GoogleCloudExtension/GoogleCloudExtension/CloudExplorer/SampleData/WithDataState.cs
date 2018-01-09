using GoogleCloudExtension.Utils.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorer.SampleData
{
    /// <summary>
    /// Mocked version of <seealso cref="CloudExplorerViewModel"/> that has same sample data to show.
    /// </summary>
    public class WithDataState : SampleDataBase
    {
        public bool LoadingProject { get; } = false;

        public AsyncProperty<string> ProfileNameAsync { get; } = new AsyncProperty<string>("User Name");

        public string ProjectDisplayString { get; } = "Project-Id";

        public IList<TreeHierarchy> Roots { get; } = new List<TreeHierarchy>
        {
            new TreeHierarchy(new List<TreeNode>
            {
                new TreeLeaf { Caption = "Node 1", Icon = s_instanceIcon.Value },
                new TreeLeaf { Caption = "Node 2", Icon = s_instanceIcon.Value }
            })
            {
                Caption = "Container 1",
                Icon = s_containerIcon.Value,
                IsExpanded = true,
            },
            new TreeHierarchy(new List<TreeNode>
            {
                new TreeLeaf { Caption = "Node1", Icon = s_instanceIcon.Value }
            })
            {
                Caption = "Container 2",
                Icon = s_containerIcon.Value,
            },
            new TreeHierarchy(new List<TreeNode>
            {
                new TreeLeaf { Caption = "Warning", IsWarning = true },
                new TreeLeaf { Caption = "Error", IsError = true },
                new TreeLeaf { Caption = "Loading", IsLoading = true }

            })
            {
                Caption = "Variants",
                Icon = s_containerIcon.Value,
                IsExpanded = true
            }
        };
    }
}
