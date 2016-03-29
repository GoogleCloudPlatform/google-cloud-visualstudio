// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class contains the functionality to manage AppEngine applications, listing of versions,
    /// managing the default verision, etc...
    /// </summary>
    public static class AppEngineClient
    {
        /// <summary>
        /// Sets the given version as the default version for the given module.
        /// </summary>
        /// <param name="module">The module to change.</param>
        /// <param name="version">The version to be made default.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task SetDefaultAppVersionAsync(string module, string version)
        {
            await GCloudWrapper.Instance.GetCommandOutputAsync($"preview app modules set-default {module} --version={version} --quiet");
        }

        /// <summary>
        /// Deletes the given app version from the module.
        /// </summary>
        /// <param name="module">The module that owns the version to remove.</param>
        /// <param name="version">The version to remove.</param>
        /// <returns>Taks that will be fullfilled when done.</returns>
        public static async Task DeleteAppVersion(string module, string version)
        {
            await GCloudWrapper.Instance.GetCommandOutputAsync($"preview app modules delete {module} --version={version} --quiet");
        }
    }
}
