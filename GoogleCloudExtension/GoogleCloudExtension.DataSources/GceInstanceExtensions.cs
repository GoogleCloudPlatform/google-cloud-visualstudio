// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GoogleCloudExtension.DataSources
{
    public enum InstanceStatus
    {
        None,
        Provisioning,
        Staging,
        Running,
        Stopping,
        Terminated,
    }

    public static class GceInstanceExtensions
    {
        private const string WindowsCredentialsKey = "windows-credentials";

        public const string ProvisioningStatus = "PROVISIONING";
        public const string StagingStatus = "STAGING";
        public const string RunningStatus = "RUNNING";
        public const string StoppingStatus = "STOPPING";
        public const string TerminatedStatus = "TERMINATED";
        
        public static bool IsAspnetInstance(this GceInstance instance)
        {
            return instance.Tags?.Items?.Contains("aspnet") ?? false;
        }

        public static GceCredentials GetServerCredentials(this GceInstance instance)
        {
            var credentials = instance.Metadata.Items?.FirstOrDefault(x => x.Key == WindowsCredentialsKey)?.Value;
            if (credentials != null)
            {
                return JsonConvert.DeserializeObject<GceCredentials>(credentials);
            }
            return null;
        }

        /// <summary>
        /// Stores the new credentials for the instance, returning a new instance that has been fully
        /// updated with the new metadata.
        /// </summary>
        /// <param name="instance">The instance where to store the credentials.</param>
        /// <param name="credentials">The credentials to store.</param>
        /// <param name="oauthToken">The oauth token to use.</param>
        /// <returns></returns>
        public static async Task<GceInstance> SetServerCredentials(this GceInstance instance, GceCredentials credentials, string oauthToken)
        {
            var serializedCredentials = JsonConvert.SerializeObject(
                credentials,
                new JsonSerializerSettings { Formatting = Formatting.None });
            Debug.WriteLine($"Writting credentials: {serializedCredentials}");
            var newMetadataItems = instance.Metadata.Items.SetValue(WindowsCredentialsKey, serializedCredentials);
            return await GceDataSource.StoreMetadata(instance, newMetadataItems, oauthToken);
        }

        public static bool IsGaeInstance(this GceInstance instance)
        {
            return !string.IsNullOrEmpty(instance.GetGaeModule());
        }

        public static string GetPublishUrl(this GceInstance instance)
        {
            return $"{instance.GetPublicIpAddress()}";
        }

        public static string GetDestinationAppUri(this GceInstance instance)
        {
            return $"http://{instance.GetPublicIpAddress()}";
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

        public static string GetTags(this GceInstance instance) => String.Join(", ", instance.Tags?.Items ?? Enumerable.Empty<string>());

        public static string GeneratePublishSettings(this GceInstance instance, GceCredentials credentials = null)
        {
            if (credentials == null)
            {
                credentials = instance.GetServerCredentials();
            }

            if (credentials == null)
            {
                return null;
            }

            var doc = new XDocument(
                new XElement("publishData",
                    new XElement("publishProfile",
                        new XAttribute("profileName", "Google Cloud Profile-WebDeploy"),
                        new XAttribute("publishMethod", "MSDeploy"),
                        new XAttribute("publishUrl", instance.GetPublishUrl()),
                        new XAttribute("msdeploySite", "Default Web Site"),
                        new XAttribute("userName", credentials.User),
                        new XAttribute("userPWD", credentials.Password),
                        new XAttribute("destinationAppUri", instance.GetDestinationAppUri()))));

            return doc.ToString();
        }

        public static bool IsRunning(this GceInstance instance) => instance.Status == RunningStatus;
    }
}
