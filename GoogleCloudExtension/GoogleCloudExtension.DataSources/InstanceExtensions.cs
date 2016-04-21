// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google.Apis.Compute.v1.Data;
using System;
using System.Linq;
using System.Xml.Linq;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class implement extension methods which implement behaviors on top of the instance models.
    /// </summary>
    public static class InstanceExtensions
    {
        private const string WindowsLicenseUrl = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2012-r2-dc";
        private const string SqlServerSaPassword = "c2d-property-saPassword";

        public const string ProvisioningStatus = "PROVISIONING";
        public const string StagingStatus = "STAGING";
        public const string RunningStatus = "RUNNING";
        public const string StoppingStatus = "STOPPING";
        public const string TerminatedStatus = "TERMINATED";

        /// <summary>
        /// Determins if the given instance is an ASP.NET instance.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>True if the instance is an ASP.NET instance.</returns>
        public static bool IsAspnetInstance(this Instance instance)
        {
            return instance.Tags?.Items?.Contains("aspnet") ?? false;
        }

        /// <summary>
        /// Determines if the given instance is a ManagedVM instance.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>True if the instance is a GAE instance, false otherwise.</returns>
        public static bool IsGaeInstance(this Instance instance)
        {
            return !string.IsNullOrEmpty(instance.GetGaeModule());
        }

        /// <summary>
        /// Determines if the given instance is a Windows instance.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>True if the instance is a Windows instance, false otherwise.</returns>
        public static bool IsWindowsInstance(this Instance instance)
        {
            return instance.Disks?.Where(x => x.Licenses?.Contains(WindowsLicenseUrl) ?? false).FirstOrDefault() != null;
        }

        /// <summary>
        /// Determines the URL to use to publish to this server. Typically used for ASP.NET 4.x apps.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>The URL.</returns>
        public static string GetPublishUrl(this Instance instance)
        {
            return instance.GetPublicIpAddress();
        }

        /// <summary>
        /// Determines the URL to use to see the ASP.NET app that runs on the given instance.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>The URL.</returns>
        public static string GetDestinationAppUri(this Instance instance)
        {
            return $"http://{instance.GetPublicIpAddress()}";
        }

        /// <summary>
        /// Returns the GAE module for a GCE instance if it part of an AppEngine app.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The module.</returns>
        public static string GetGaeModule(this Instance instance)
        {
            return instance.Metadata.Items?.FirstOrDefault(x => x.Key == "gae_backend_name")?.Value;
        }

        /// <summary>
        /// Returns the GAE version for a GCE instance that is part of an AppEngine app.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The version.</returns>
        public static string GetGaeVersion(this Instance instance)
        {
            return instance.Metadata.Items?.FirstOrDefault(x => x.Key == "gae_backend_version")?.Value;
        }

        /// <summary>
        /// Returns the internal IP address for the instance.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The IP address in string form.</returns>
        public static string GetInternalIpAddress(this Instance instance)
        {
            return instance.NetworkInterfaces?.FirstOrDefault()?.NetworkIP;
        }

        /// <summary>
        /// Returns the public IP address, if it exists, for the instance.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The IP address in string form.</returns>
        public static string GetPublicIpAddress(this Instance instance)
        {
            return instance.NetworkInterfaces?.FirstOrDefault()?.AccessConfigs?.FirstOrDefault()?.NatIP;
        }

        /// <summary>
        /// Returns a string with all of the tags for the instance.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>The tags.</returns>
        public static string GetTags(this Instance instance) => String.Join(", ", instance.Tags?.Items ?? Enumerable.Empty<string>());

        /// <summary>
        /// Generates the publishsettings information for a given GCE instance.
        /// </summary>
        /// <param name="instance">The instance to inspect.</param>
        /// <returns>A string with the publishsettings content.</returns>
        public static string GeneratePublishSettings(this Instance instance)
        {
            var doc = new XDocument(
                new XElement("publishData",
                    new XElement("publishProfile",
                        new XAttribute("profileName", "Google Cloud Profile-WebDeploy"),
                        new XAttribute("publishMethod", "MSDeploy"),
                        new XAttribute("publishUrl", instance.GetPublishUrl()),
                        new XAttribute("msdeploySite", "Default Web Site"),
                        new XAttribute("destinationAppUri", instance.GetDestinationAppUri()))));

            return doc.ToString();
        }

        public static bool IsRunning(this Instance instance) => instance.Status == RunningStatus;

        public static string GetSqlServerPassword(this Instance instance) => instance.Metadata.GetProperty(SqlServerSaPassword);

        public static bool IsSqlServer(this Instance instance) => instance.GetSqlServerPassword() != null;

        public static string ZoneName(this Instance instance)
        {
            var url = new Uri(instance.Zone);
            return url.Segments.Last();
        }
    }
}
