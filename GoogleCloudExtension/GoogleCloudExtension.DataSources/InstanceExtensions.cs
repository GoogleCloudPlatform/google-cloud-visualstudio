// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Apis.Compute.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class implements extension methods which implement behaviors on top of the instance models.
    /// </summary>
    public static class InstanceExtensions
    {
        private const string WindowsServer2012License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2012-r2-dc";
        private const string WindowsServer2008License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2008-r2-dc";
        private const string WindowsServer2016License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2016-dc";
        private const string SqlServerSaPasswordKey = "c2d-property-saPassword";

        private static readonly Dictionary<string, WindowsInstanceInfo> s_windowsLicenses = new Dictionary<string, WindowsInstanceInfo>
        {
            { WindowsServer2008License, new WindowsInstanceInfo(displayName: "Windows Server 2008 R2 Datacenter Edition", version: "2008", subversion: "RC2") },
            { WindowsServer2012License, new WindowsInstanceInfo(displayName: "Windows Server 2012 R2 Datacenter Edition", version: "2012", subversion: "RC2") },
            { WindowsServer2016License, new WindowsInstanceInfo(displayName: "Windows Server 2016", version: "2016") }
        };

        public const string ProvisioningStatus = "PROVISIONING";
        public const string StagingStatus = "STAGING";
        public const string RunningStatus = "RUNNING";
        public const string StoppingStatus = "STOPPING";
        public const string TerminatedStatus = "TERMINATED";

        /// <summary>
        /// Determines if the given instance is an ASP.NET instance.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>True if the instance is an ASP.NET instance.</returns>
        public static bool IsAspnetInstance(this Instance instance) => instance.Tags?.Items?.Contains("aspnet") ?? false;

        /// <summary>
        /// Determines if the given instance is an App Engine Flex instance.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>True if the instance is a App Engine Flex instance, false otherwise.</returns>
        public static bool IsAppEngineFlexInstance(this Instance instance) => !String.IsNullOrEmpty(instance.GetAppEngineFlexService());

        /// <summary>
        /// Determines if the given instance is a Windows instance.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>True if the instance is a Windows instance, false otherwise.</returns>
        public static bool IsWindowsInstance(this Instance instance) => GetWindowsInstanceInfo(instance) != null;

        /// <summary>
        /// Returns the instance information for this instance, if it is a Windows instance.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>If the instance is a Windows instance the <seealso cref="WindowsInstanceInfo"/> for 
        /// the instance, null otherwise</returns>
        public static WindowsInstanceInfo GetWindowsInstanceInfo(this Instance instance)
        {
            var bootDisk = instance.Disks?.FirstOrDefault(x => x.Boot ?? false);
            if (bootDisk == null || bootDisk.Licenses == null)
            {
                return null;
            }

            var license = bootDisk.Licenses.Intersect(s_windowsLicenses.Keys).FirstOrDefault();
            if (license == null)
            {
                return null;
            }
            return s_windowsLicenses[license];
        }

        /// <summary>
        /// Determines the URL to use to publish to this server. Typically used for ASP.NET 4.x apps.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>The publish URL, null otherwise.</returns>
        public static string GetPublishUrl(this Instance instance) => instance.GetPublicIpAddress();

        /// <summary>
        /// Determines the URL to use to see the ASP.NET app that runs on the given instance.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>The destination URL, null otherwise.</returns>
        public static string GetDestinationAppUri(this Instance instance) => $"http://{instance.GetPublicIpAddress()}";

        /// <summary>
        /// Returns the App Engine Flex service for a GCE instance if it part of an App Engine Flex app.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The module, null if not applicable.</returns>
        public static string GetAppEngineFlexService(this Instance instance) =>
            instance.Metadata.Items?.FirstOrDefault(x => x.Key == "gae_backend_name")?.Value;

        /// <summary>
        /// Returns the App Engine Flex version for a GCE instance that is part of an App Engine Flex app.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The version, null if not applicable.</returns>
        public static string GetAppEngineFlexVersion(this Instance instance) =>
            instance.Metadata.Items?.FirstOrDefault(x => x.Key == "gae_backend_version")?.Value;

        /// <summary>
        /// Retruns the machine type based on the GCE machine type URL.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The machine type.</returns>
        public static string GetMachineType(this Instance instance) => new Uri(instance.MachineType).Segments.Last();

        /// <summary>
        /// Returns the internal IP address for the instance.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The IP address in string form.</returns>
        public static string GetInternalIpAddress(this Instance instance) =>
            instance.NetworkInterfaces?.FirstOrDefault()?.NetworkIP;

        /// <summary>
        /// Returns the public IP address, if it exists, for the instance.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The IP address in string form.</returns>
        public static string GetPublicIpAddress(this Instance instance) =>
            instance.NetworkInterfaces?.FirstOrDefault()?.AccessConfigs?.FirstOrDefault()?.NatIP;

        /// <summary>
        /// Returns the FQDN for an instance in GCE.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns></returns>
        public static string GetFullyQualifiedDomainName(this Instance instance)
        {
            var ipAddress = instance.GetPublicIpAddress();
            if (ipAddress == null)
            {
                return "";
            }

            var reverseIp = string.Join(".", ipAddress.Split('.').Reverse());
            return $"{reverseIp}.bc.googleusercontent.com";
        }

        /// <summary>
        /// Returns a string with all of the tags for the instance.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The tags.</returns>
        public static string GetTags(this Instance instance) =>
            String.Join(", ", instance.Tags?.Items ?? Enumerable.Empty<string>());

        /// <summary>
        /// Generates the publishsettings information for a given GCE instance.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <param name="userName">The user name to use.</param>
        /// <param name="password">The passwod to use.</param>
        /// <returns>A string with the publishsettings content.</returns>
        public static string GeneratePublishSettings(this Instance instance, string userName, string password)
        {
            var doc = new XDocument(
                new XElement("publishData",
                    new XElement("publishProfile",
                        new XAttribute("profileName", instance.Name),
                        new XAttribute("publishMethod", "MSDeploy"),
                        new XAttribute("publishUrl", instance.GetPublishUrl()),
                        new XAttribute("userName", userName),
                        new XAttribute("userPWD", password),
                        new XAttribute("msdeploySite", "Default Web Site"),
                        new XAttribute("destinationAppUri", instance.GetDestinationAppUri()))));

            return doc.ToString();
        }

        /// <summary>
        /// Returns whether the given instance is in the running state.
        /// </summary>
        /// <param name="instance">The the instance to check.</param>
        public static bool IsRunning(this Instance instance) => instance.Status == RunningStatus;

        /// <summary>
        /// Returns the SQL server password stored in the instance's metadata.
        /// </summary>
        /// <param name="instance">The the instance to check.</param>
        public static string GetSqlServerPassword(this Instance instance) => instance.Metadata.Items?.FirstOrDefault(x => x.Key == SqlServerSaPasswordKey)?.Value;

        /// <summary>
        /// Returns whether the given instance has SQL server password defined or not.
        /// </summary>
        /// <param name="instance">The the instance to check.</param>
        public static bool HasSqlServerPassword(this Instance instance) => instance.GetSqlServerPassword() != null;

        /// <summary>
        /// Returns the zone name where the instance is located.
        /// </summary>
        /// <param name="instance">The the instance to check.</param>
        public static string GetZoneName(this Instance instance) => new Uri(instance.Zone).Segments.Last();
    }
}
