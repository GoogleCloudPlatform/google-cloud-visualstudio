using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This enum describe the well known components for gcloud.
    /// </summary>
    public enum GCloudComponent
    {
        /// <summary>
        /// Placeholder for no component.
        /// </summary>
        None = 0,

        /// <summary>
        /// The beta component, contains the beta features for gcloud, only depend on this if
        /// absolutely necessary as things change rapidly.
        /// </summary>

        Beta,

        /// <summary>
        /// The kubectl component, which installs the necessary tools to work with Kubernetes clusters.
        /// </summary>
        Kubectl,
    }
}
