using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    public class GkeDeploymentResult
    {
        public string ServiceIpAddress { get; }

        public bool WasExposed { get; }

        public bool DeploymentUpdated { get; }

        public GkeDeploymentResult(string serviceIpAddress, bool wasExposed, bool deploymentUpdated)
        {
            ServiceIpAddress = serviceIpAddress;
            WasExposed = wasExposed;
            DeploymentUpdated = deploymentUpdated;
        }
    }
}
