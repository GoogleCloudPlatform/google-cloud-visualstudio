using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    static class CommandUtils
    {
        public static bool ValidateEnvironment()
        {
            var validDNXInstallation = DnxEnvironment.ValidateDnxInstallationForRuntime(AspNetRuntime.CoreClr) || 
                DnxEnvironment.ValidateDnxInstallationForRuntime(AspNetRuntime.Mono);
            var validGCloudInstallation = GCloudWrapper.Instance.ValidateGCloudInstallation();
            return validDNXInstallation && validGCloudInstallation;
        }
    }
}
