// Copyright 2016 Google Inc. All Rights Reserved.
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
        private static readonly IList<string> s_componentsNeeded = new List<string>
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
        public static Task<GCloudValidationResult> ValidateGCloudInstallationAsync() => s_CachedGCloudResult.Value;

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
        private static Task<GCloudValidationResult> ValidateGCloudInstallationImplAsync() => Task.Run<GCloudValidationResult>(
            async () =>
            {
                var gcloudInstalled = GCloudWrapper.Instance.IsGCloudCliInstalled();

                if (!gcloudInstalled)
                {
                    return new GCloudValidationResult(
                        gcloudInstalled: gcloudInstalled,
                        missingComponents: Enumerable.Empty<string>());
                }

                // Ensure that the list of components that we need are installed.
                var installedComponents = await GCloudWrapper.Instance.GetInstalledComponentsAsync();
                var missingComponents = s_componentsNeeded.Where(x => !installedComponents.Contains(x));
                return new GCloudValidationResult(
                    gcloudInstalled: gcloudInstalled,
                    missingComponents: missingComponents);
            });

        /// <summary>
        /// This method warms up the validation result cache by validating the gcloud installation. Since
        /// this validation actually happens in a background thread this method can be called as soon as
        /// the extension is loaded, this way the most probably outcome is that the validation results will
        /// be ready by the time they are needed when invoking a command.
        /// </summary>
        public static async void PreFetchGCloudValidationResult()
        {
            var validationResult = await ValidateGCloudInstallationAsync();
            if (validationResult.IsValidGCloudInstallation)
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
