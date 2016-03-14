// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// Defines the JSON schema of a component state, which contains the installaion
    /// state of the component.
    /// </summary>
    public class ComponentState
    {
        public const string InstalledState = "Installed";
        public const string NotInstalledState = "Not Installed";
        public const string UpdateAvailableState = "Update Available";

        [JsonProperty("name")]
        public string StateName { get; set; }

        public bool IsInstalled() => StateName == InstalledState || StateName == UpdateAvailableState;

        public bool IsNotInstalled() => StateName == NotInstalledState;
    }
}