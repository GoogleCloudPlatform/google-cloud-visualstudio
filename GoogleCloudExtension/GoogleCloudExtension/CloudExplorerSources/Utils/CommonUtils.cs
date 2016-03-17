using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.Utils
{
    public static class CommonUtils
    {
        public static TreeLeaf GetErrorItem(GCloudValidationResult gcloudValidationResult)
        {
            return new TreeLeaf
            {
                IsError = true,
                Content = gcloudValidationResult.GetDisplayString(),
            };
        }
    }
}
