using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    public class NetCorePublishResult
    {
        public string ProjectId { get; }

        public string Service { get; }

        public string Version { get; }

        public bool Promoted { get; }

        public NetCorePublishResult(string projectId, string service, string version, bool promoted)
        {
            ProjectId = projectId;
            Service = service;
            Version = version;
            Promoted = promoted;
        }

        public string GetDeploymentUrl()
        {
            if (Promoted)
            {
                if (Service == "default")
                {
                    return $"https://{ProjectId}.appspot.com";
                }
                else
                {
                    return $"https://{Service}-{ProjectId}.appspot.com";
                }
            }
            else
            {
                return $"https://{Version}-{Service}-{ProjectId}.appspot.com";
            }
        }
    }
}
