// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Contains the result of running detection for the gcloud SDK.
    /// </summary>
    public class GCloudValidationResult
    {
        /// <summary>
        /// Whether gcloud SDK is installed.
        /// </summary>
        public bool IsGCloudInstalled { get; }

        /// <summary>
        /// The list required componets that are missing.
        /// </summary>
        public IReadOnlyCollection<CloudSdkComponent> MissingComponents { get; }

        public GCloudValidationResult(
            bool gcloudInstalled,
            IEnumerable<CloudSdkComponent> missingComponents)
        {
            IsGCloudInstalled = gcloudInstalled;
            MissingComponents = new ReadOnlyCollection<CloudSdkComponent>(missingComponents.ToList());
        }

        /// <summary>
        /// Returns if everything is ok with the gcloud SDK installation.
        /// </summary>
        /// <returns>True if the installation is fine, false otherwise.</returns>
        public bool IsValidGCloudInstallation => IsGCloudInstalled && MissingComponents.Count == 0;

        /// <summary>
        /// Returns a string representation of the result.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder resultBuilder = new StringBuilder();

            if (!IsGCloudInstalled)
            {
                resultBuilder.AppendLine("Need to install gcloud");
            }

            if (MissingComponents.Count != 0)
            {
                resultBuilder.AppendLine("Missing components:");
                foreach (var component in MissingComponents)
                {
                    resultBuilder.AppendLine($"  Component: {component.Id}");
                }
            }

            return resultBuilder.ToString();
        }
    }
}