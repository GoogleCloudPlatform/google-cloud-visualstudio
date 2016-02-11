using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    internal static class GceInstanceExtensions
    {
        public static bool IsAspnetInstance(this GceInstance instance)
        {
            return instance.Tags?.Items?.Contains("aspnet") ?? false;
        }

        public static GceCredentials GetServerCredentials(this GceInstance instance)
        {
            var credentials = instance.Metadata.Items?.FirstOrDefault(x => x.Key == "windows-credentials")?.Value;
            if (credentials != null)
            {
                return JsonConvert.DeserializeObject<GceCredentials>(credentials);
            }
            return null;
        }

        public static bool IsGaeInstance(this GceInstance instance)
        {
            return !string.IsNullOrEmpty(instance.GetGaeModule());
        }

        public static string GetPublishUrl(this GceInstance instance)
        {
            return "none";
        }

        public static string GetDestinationAppUri(this GceInstance instance)
        {
            return "none";
        }

        public static string GetGaeModule(this GceInstance instance)
        {
            return instance.Metadata.Items?.FirstOrDefault(x => x.Key == "gae_backend_name")?.Value;
        }

        public static string GetGaeVersion(this GceInstance instance)
        {
            return instance.Metadata.Items?.FirstOrDefault(x => x.Key == "gae_backend_version")?.Value;
        }

        public static string GetIpAddress(this GceInstance instance)
        {
            return instance.NetworkInterfaces?.FirstOrDefault()?.NetworkIp;
        }

        public static string GetPublicIpAddress(this GceInstance instance)
        {
            return instance.NetworkInterfaces?.FirstOrDefault()?.AccessConfigs?.FirstOrDefault()?.NatIP;
        }
    }
}
