// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GoogleCloudExtension.DataSources
{
    public static class GceInstanceExtensions
    {
        private const string WindowsCredentialsKey = "windows-credentials";

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

        public static async Task SetServerCredentials(this GceInstance instance, GceCredentials credentials, string oauthToken)
        {
            var serializedCredentials = JsonConvert.SerializeObject(
                credentials,
                new JsonSerializerSettings { Formatting = Formatting.None });

            Debug.WriteLine($"Writting credentials: {serializedCredentials}");
            instance.StoreMetadata(WindowsCredentialsKey, serializedCredentials);            
            await GceDataSource.StoreMetadata(instance, WindowsCredentialsKey, serializedCredentials, oauthToken);
        }

        private static void StoreMetadata(this GceInstance instance, string key, string value)
        {
            // Ensure the instance has storage for metadata entries.
            if (instance.Metadata.Items == null)
            {
                instance.Metadata.Items = new List<MetadataEntry>();
            }
            var existingEntry = instance.Metadata.Items.FirstOrDefault(x => x.Key == key);
            if (existingEntry != null)
            {
                instance.Metadata.Items.Remove(existingEntry);
            }
            instance.Metadata.Items.Add(new MetadataEntry { Key = key, Value = value });
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

        public static string GetTags(this GceInstance instance)
        {
            return String.Join(", ", instance.Tags?.Items);
        }

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
    }
}
