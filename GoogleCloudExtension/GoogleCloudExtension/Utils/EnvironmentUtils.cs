// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Dnx;
using GoogleCloudExtension.GCloud.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Utils to use to determine installation state of the various dependencies for the extension.
    /// </summary>
    static public class EnvironmentUtils
    {
        /// <summary>
        /// List of components required by the extension. This list is to be kept in sync with the
        /// requirements for the extension.
        /// </summary>
        private static readonly IList<string> s_ComponentsNeeded = new List<string>
        {
            "alpha",  // For the preview app deploy command.
            "beta",   // For the beta compute reset-windows-password command.
        };

        /// <summary>
        /// Cache for the validation result, this ensures that this expensive operation is only
        /// performed once.
        /// </summary>
        private static readonly Lazy<Task<GCloudValidationResult>> s_CachedGCloudResult =
            new Lazy<Task<GCloudValidationResult>>(ValidateGCloudInstallationImplAsync);

        /// <summary>
        /// Provides a very fast check to see if gcloud is present in the machine or not, useful when
        /// all that is required is to know if gcloud is present, or if calling an async method is not
        /// feasible.
        /// </summary>
        /// <returns>True if gcloud is installed, false otherwise, including error.</returns>
        public static bool IsGCloudInstalled() => GCloudWrapper.Instance.IsGCloudCliInstalled();

        /// <summary>
        /// Validates the gcloud environment, it is the most costly of all the checks and therefore it is
        /// cached. It will not change during the life of Visual Studio.
        /// </summary>
        /// <returns>A task with the result of the validation.</returns>
        public static Task<GCloudValidationResult> ValidateGCloudInstallation() => s_CachedGCloudResult.Value;

        /// <summary>
        /// Validates that the gcloud and the dnx environments are correclty setup.
        /// </summary>
        /// <returns></returns>
        public static DnxValidationResult ValidateDnxInstallation()
        {
            return new DnxValidationResult(DnxEnvironment.ValidateDnxInstallation());
        }

        /// <summary>
        /// Actually calculates the gcloud validation result.
        /// </summary>
        /// <returns></returns>
        private static Task<GCloudValidationResult> ValidateGCloudInstallationImplAsync()
        {
            return Task.Run<GCloudValidationResult>(async () =>
            {
                var gcloudInstalled = GCloudWrapper.Instance.IsGCloudCliInstalled();

                if (!gcloudInstalled)
                {
                    return new GCloudValidationResult(
                        gcloudInstalled: gcloudInstalled,
                        missingComponents: Enumerable.Empty<Component>());
                }

                // Ensure that the list of components that we need are installed.
                var components = await GCloudWrapper.Instance.GetComponentsAsync();
                var missingComponents = components
                    .Where(x => s_ComponentsNeeded.Contains(x.Id) && x.State.IsNotInstalled());
                return new GCloudValidationResult(
                    gcloudInstalled: gcloudInstalled,
                    missingComponents: missingComponents);
            });
        }

        /// <summary>
        /// Prefetches the validation information for gcloud SDK.,
        /// </summary>
        public static async void PreFetchGCloudValidationState()
        {
            var validationResult = await ValidateGCloudInstallation();
            if (validationResult.IsValidGCloudInstallation())
            {
                Debug.WriteLine("GCloud is correctly installed.");
            }
            else
            {
                Debug.WriteLine($"Failed validation: {validationResult}");
            }
        }
    }
}
