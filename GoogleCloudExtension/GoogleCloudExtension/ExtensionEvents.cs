// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;

namespace GoogleCloudExtension
{
    /// <summary>
    /// Global events in the extension.
    /// </summary>
    public static class ExtensionEvents
    {
        /// <summary>
        /// Raised when there's a new succesful deployment.
        /// </summary>
        public static event EventHandler AppEngineDeployed;

        /// <summary>
        /// Raises the <c>AppEngineDeployed</c> event.
        /// </summary>
        public static void RaiseAppEngineDeployed()
        {
            AppEngineDeployed?.Invoke(null, EventArgs.Empty);
        }
    }
}
