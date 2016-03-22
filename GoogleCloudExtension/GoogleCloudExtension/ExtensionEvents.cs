// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;

namespace GoogleCloudExtension
{
    public static class ExtensionEvents
    {
        public static event EventHandler AppEngineDeployed;
        public static void RaiseAppEngineDeployed()
        {
            if (AppEngineDeployed != null)
            {
                AppEngineDeployed(null, EventArgs.Empty);
            }
        }
    }
}
